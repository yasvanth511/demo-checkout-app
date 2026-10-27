using System.Diagnostics.Metrics;

namespace CheckoutApi.Telemetry;

public static class CheckoutMetrics
{
    public const string MeterName = "CheckoutApi.Metrics";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> CheckoutRequests = Meter.CreateCounter<long>(
        "checkout_requests_total",
        "requests",
        "Total number of checkout requests initiated");

    public static readonly Counter<long> CheckoutSuccesses = Meter.CreateCounter<long>(
        "checkout_success_total",
        "requests",
        "Total number of successfully completed checkout orders");

    public static readonly Counter<long> PaymentTimeoutErrors = Meter.CreateCounter<long>(
        "payment_timeout_errors_total",
        "errors",
        "Total number of payment timeout errors encountered during checkout");

    public static readonly Histogram<double> CheckoutDurationMs = Meter.CreateHistogram<double>(
        "checkout_duration_ms",
        "ms",
        "Time taken to process checkout orders");
}
