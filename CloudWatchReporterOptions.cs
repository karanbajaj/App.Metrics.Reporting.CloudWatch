namespace App.Metrics.Reporting.CloudWatch
{
    using App.Metrics.Filters;
    using System;

    /// <summary>
    /// Provides programmatic configuration of AWS CloudWatch reporting in the App Metrics framework.
    /// </summary>
    public class CloudWatchReporterOptions
    {
        /// <summary>
        ///     Application Insights instrumentation key.
        /// </summary>
        public string? Profile { get; set; }

        public string? Region { get; set; }

        public string? AccessKey { get; set; }

        public string? SecretKey { get; set; }

       
        

        /// <summary>
        /// Gets or sets the <see cref="IFilterMetrics" /> to use for just this reporter.
        /// </summary>
        public IFilterMetrics? Filter { get; set; }

        /// <summary>
        /// Gets or sets the interval between flushing metrics.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the CloudWatch location that will store the metrics.
        /// </summary>
        public string CustomMetricNamespace { get; set; } = "App.Metrics";

        /// <summary>
        /// Metrics can exceed the max 1mb Size - so page through the metrics using this page size
        /// </summary>
        public int PageMetricsCount { get; set; } = 20;

    }
}
