using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CheckoutApi.Data;
using CheckoutApi.Models;
using CheckoutApi.Telemetry;

namespace CheckoutApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly CheckoutDbContext _db;
    private readonly ILogger<CheckoutController> _logger;
    private readonly IConfiguration _config;

    public CheckoutController(CheckoutDbContext db, ILogger<CheckoutController> logger, IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessCheckout([FromBody] CheckoutRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        CheckoutMetrics.CheckoutRequests.Add(1);

        int timeoutSeconds = _config.GetValue<int>("Payment:TimeoutSeconds", 3);

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "Items list cannot be empty" });
        }

        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            Currency = request.Currency ?? "USD",
            DiscountAmount = request.DiscountAmount,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow
        };

        decimal subtotal = 0;
        foreach (var item in request.Items)
        {
            subtotal += item.UnitPrice * item.Quantity;
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                Sku = item.Sku,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });
        }

        order.TotalAmount = Math.Max(0, subtotal - request.DiscountAmount);

        // Payment processing simulation with timeout logic
        var payment = new PaymentRecord
        {
            OrderId = order.Id,
            PaymentMethod = request.PaymentMethod ?? "CreditCard",
            Amount = order.TotalAmount,
            TransactionId = $"txn_{Guid.NewGuid():N}",
            TimeoutSeconds = timeoutSeconds,
            ProcessedAt = DateTime.UtcNow
        };

        // If simulated timeout flag is enabled or latency exceeds timeout
        if (request.SimulateTimeout || timeoutSeconds < 2)
        {
            CheckoutMetrics.PaymentTimeoutErrors.Add(1);
            order.Status = "PaymentFailedTimeout";
            payment.Status = "TimedOut";
            _logger.LogError("Payment processing timed out after {TimeoutSeconds}s for order {OrderId}", timeoutSeconds, order.Id);

            order.Payment = payment;
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return StatusCode(504, new
            {
                error = "payment_gateway_timeout",
                message = $"Payment processor exceeded configured timeout threshold of {timeoutSeconds}s",
                orderId = order.Id
            });
        }

        order.Status = "Completed";
        payment.Status = "Success";
        order.Payment = payment;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        stopwatch.Stop();
        CheckoutMetrics.CheckoutDurationMs.Record(stopwatch.ElapsedMilliseconds);
        CheckoutMetrics.CheckoutSuccesses.Add(1);

        _logger.LogInformation("Order {OrderId} processed successfully for {Email} in {ElapsedMs}ms",
            order.Id, order.CustomerEmail, stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            orderId = order.Id,
            status = order.Status,
            total = order.TotalAmount,
            transactionId = payment.TransactionId
        });
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound(new { error = "order_not_found" });
        return Ok(order);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> ListOrders([FromQuery] int take = 20)
    {
        var orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(Math.Min(take, 100))
            .ToListAsync();
        return Ok(orders);
    }
}

public class CheckoutRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public bool SimulateTimeout { get; set; }
    public List<CheckoutItemRequest> Items { get; set; } = new();
}

public class CheckoutItemRequest
{
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
