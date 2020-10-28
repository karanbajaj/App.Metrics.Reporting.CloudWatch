namespace App.Metrics
{
    using App.Metrics.Builder;
    using App.Metrics.Reporting.CloudWatch;
    using System;

    /// <summary>
    ///     Builder for configuring AWS CloudWatch reporting using an <see cref="IMetricsReportingBuilder" />.
    /// </summary>
    public static class CloudWatchReporterBuilder
    {
        /// <summary>
        ///     Add the <see cref="CloudWatchReporter" /> allowing metrics to be reported to AWS CloudWatch.
        /// </summary>
        /// <param name="reportingBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="options">The reporting options to use.</param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToCloudWatch(
            this IMetricsReportingBuilder reportingBuilder,
            CloudWatchReporterOptions options)
        {
            if (reportingBuilder == null)
            {
                throw new ArgumentNullException(nameof(reportingBuilder));
            }

            var reporter = new CloudWatchReporter(options);

            return reportingBuilder.Using(reporter);
        }

        /// <summary>
        ///     Add the <see cref="CloudWatchReporter" /> allowing metrics to be reported to AWS CloudWatch.
        /// </summary>
        /// <param name="reportingBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="setupAction">The reporting options to use.</param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToCloudWatch(
            this IMetricsReportingBuilder reportingBuilder,
            Action<CloudWatchReporterOptions> setupAction)
        {
            if (reportingBuilder == null)
            {
                throw new ArgumentNullException(nameof(reportingBuilder));
            }

            var options = new CloudWatchReporterOptions();
            setupAction?.Invoke(options);
            var reporter = new CloudWatchReporter(options);

            return reportingBuilder.Using(reporter);
        }

        /// <summary>
        ///     Add the <see cref="CloudWatchReporter" /> allowing metrics to be reported to AWS CloudWatch.
        /// </summary>
        /// <param name="reportingBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <param name="profile">AWS credentials profiles.</param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToCloudWatch(this IMetricsReportingBuilder reportingBuilder, string profile)
        {
            if (reportingBuilder == null)
            {
                throw new ArgumentNullException(nameof(reportingBuilder));
            }

            var options = new CloudWatchReporterOptions
            {
                Profile = profile,
            };

            var reporter = new CloudWatchReporter(options);

            return reportingBuilder.Using(reporter);
        }

        /// <summary>
        ///     Add the <see cref="CloudWatchReporter" /> allowing metrics to be reported to AWS CloudWatch.
        /// </summary>
        /// <param name="reportingBuilder">
        ///     The <see cref="IMetricsReportingBuilder" /> used to configure metrics reporters.
        /// </param>
        /// <returns>
        ///     An <see cref="IMetricsBuilder" /> that can be used to further configure App Metrics.
        /// </returns>
        public static IMetricsBuilder ToCloudWatch(this IMetricsReportingBuilder reportingBuilder)
        {
            if (reportingBuilder == null)
            {
                throw new ArgumentNullException(nameof(reportingBuilder));
            }

            var options = new CloudWatchReporterOptions{};

            var reporter = new CloudWatchReporter(options);

            return reportingBuilder.Using(reporter);
        }
    }
}
