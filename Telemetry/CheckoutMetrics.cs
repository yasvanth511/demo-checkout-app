using System.Diagnostics.Metrics;

namespace CheckoutApi.Telemetry;

public static class CheckoutMetrics
{
    public const string MeterName = "CheckoutApi.Metrics";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    // Units in braces are annotations, which the OTel-to-Prometheus name translation (used by both
    // /metrics and Grafana Cloud's OTLP ingest) does not append to the metric name.
    public static readonly Counter<long> CheckoutRequests = Meter.CreateCounter<long>(
        "checkout_requests",
        "{request}",
        "Total number of checkout requests initiated");

    public static readonly Counter<long> CheckoutSuccesses = Meter.CreateCounter<long>(
        "checkout_success",
        "{request}",
        "Total number of successfully completed checkout orders");

    public static readonly Counter<long> PaymentTimeoutErrors = Meter.CreateCounter<long>(
        "payment_timeout_errors",
        "{error}",
        "Total number of payment timeout errors encountered during checkout");

    public static readonly Histogram<double> CheckoutDurationMs = Meter.CreateHistogram<double>(
        "checkout_duration",
        "ms",
        "Time taken to process checkout orders");
}
