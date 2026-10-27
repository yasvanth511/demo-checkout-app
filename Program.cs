using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using CheckoutApi.Data;
using CheckoutApi.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database (PostgreSQL if connection string provided, InMemory fallback)
var postgresConn = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(postgresConn))
{
    builder.Services.AddDbContext<CheckoutDbContext>(options =>
        options.UseNpgsql(postgresConn));
}
else
{
    builder.Services.AddDbContext<CheckoutDbContext>(options =>
        options.UseInMemoryDatabase("CheckoutDb"));
}

// Configure OpenTelemetry Metrics with Prometheus Exporter
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(CheckoutMetrics.MeterName)
            .AddPrometheusExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAuthorization();

// Expose Prometheus scraping endpoint for Grafana
app.UseOpenTelemetryPrometheusScrapingEndpoint("/metrics");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "checkout-api",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

// Apply migrations or create in-memory db on boot
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CheckoutDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

app.MapControllers();

app.Run();
