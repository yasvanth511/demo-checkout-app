namespace CheckoutApi.Models;

public class PaymentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = "Initiated";
    public int TimeoutSeconds { get; set; } = 3;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
