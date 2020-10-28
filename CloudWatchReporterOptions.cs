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

        /// <summary>
        /// Gets or sets the <see cref="IFilterMetrics" /> to use for just this reporter.
        /// </summary>
        public IFilterMetrics? Filter { get; set; }

        /// <summary>
        /// Gets or sets the interval between flushing metrics.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(60);
    }
}
