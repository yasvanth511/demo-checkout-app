using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

var otel = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("checkout-api", serviceVersion: "1.0.0"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(CheckoutMetrics.MeterName)
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    })
    .WithLogging(configureBuilder: null, configureOptions: options => options.IncludeFormattedMessage = true);

// Endpoint, protocol and auth come from the standard OTEL_EXPORTER_OTLP_* variables. Without an
// endpoint the exporter would retry localhost:4317 forever, so it is only enabled when one is set.
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    otel.UseOtlpExporter();
}

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

// Initialize database on boot (EnsureCreated works for In-Memory and relational without requiring migration files)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CheckoutDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database EnsureCreated skipped or failed; running with in-memory state.");
    }
}


app.MapControllers();

app.Run();
