using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime.CredentialManagement;
using App.Metrics.Apdex;
using App.Metrics.Counter;
using App.Metrics.Filters;
using App.Metrics.Formatters;
using App.Metrics.Histogram;
using App.Metrics.Logging;
using App.Metrics.Meter;
using App.Metrics.Timer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.Metrics.Reporting.CloudWatch
{
    public sealed class CloudWatchReporter : IReportMetrics, IDisposable
    {
        private const string UnitKey = "unit";
        private static readonly ILog Logger = LogProvider.For<CloudWatchReporter>();

        private readonly AmazonCloudWatchClient _client;
        private bool disposed;

        /// <inheritdoc />
        public IFilterMetrics? Filter { get; set; }

        /// <inheritdoc />
        public TimeSpan FlushInterval { get; set; }

        /// <inheritdoc />
        public IMetricsOutputFormatter? Formatter { get; set; }

		public string CustomNamespace { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CloudWatchReporter"/> class.
        /// </summary>
        /// <param name="options">
        ///     Configuration for <see cref="CloudWatchReporter"/>.
        /// </param>
        public CloudWatchReporter(CloudWatchReporterOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _client = GetClient(options);

            FlushInterval = options.FlushInterval > TimeSpan.Zero
                ? options.FlushInterval
                : AppMetricsConstants.Reporting.DefaultFlushInterval;
            Filter = options.Filter;

			CustomNamespace = options.CustomMetricNamespace;

            Logger.Info($"Using metrics reporter {nameof(CloudWatchReporter)}. FlushInterval: {FlushInterval}");
        }

        private AmazonCloudWatchClient GetClient(CloudWatchReporterOptions options)
        {

            //options.AccessKey is null && options.SecretKey is null && options.Region is null
            //? options.Profile is null?new AmazonCloudWatchClient():
            //: new AmazonCloudWatchClient(options.AwsAccessKeyId, options.AwsSecretAccessKey, options.Region);
            
            if (options.Profile != null)
            {
                var chain = new CredentialProfileStoreChain();
                if (chain.TryGetAWSCredentials(options.Profile, out var awsCredentials))
                {
                    return new AmazonCloudWatchClient(awsCredentials);
                }
            }
            else if(options.AccessKey != null && options.SecretKey != null && options.Region != null)
            {
                
                return new AmazonCloudWatchClient(options.AccessKey, options.SecretKey,Amazon.RegionEndpoint.GetBySystemName(options.Region));
            }
            else
            {
                return new AmazonCloudWatchClient();
            }
            throw new Exception("Unable to create AmazonCloudWatchClient");
        }
        
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            _client.Dispose();

            disposed = true;
        }

        /// <inheritdoc />
        public async Task<bool> FlushAsync(MetricsDataValueSource metricsData, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested || metricsData == null)
            {
                return false;
            }

            var sw = Stopwatch.StartNew();
            var now = DateTimeOffset.Now;
            var count = 0;
            var data = new List<MetricDatum>();
            foreach (var ctx in metricsData.Contexts)
            {
                foreach (var mt in TranslateContext(ctx, now))
                {
                    data.Add(mt);                   
                    ++count;
                }
            }

            if (count > 0)
            {
                await _client.PutMetricDataAsync(new PutMetricDataRequest
                {
                    MetricData = data,
                    Namespace = CustomNamespace
                });
                Logger.Trace($"Flushed TelemetryClient; {count} records; elapsed: {sw.Elapsed}.");
            }

            return true;
        }

        private IEnumerable<MetricDatum> TranslateContext(MetricsContextValueSource ctx, DateTimeOffset now)
        {
            var context = Filter != null ? ctx.Filter(Filter) : ctx;
            var contextName = context.Context;

            foreach (var source in context.ApdexScores)
            {
                foreach(var mt in Translate(source, contextName, now))
                {
                    yield return mt;
                }
            }

            foreach (var source in context.Counters)
            {
                yield return Translate(source, contextName, now);
            }

            foreach (var source in context.Gauges)
            {
                string metricName = source.IsMultidimensional ? source.MultidimensionalName : source.Name;
                var mt = new MetricDatum
                {
                    Value = source.Value,
                    MetricName = metricName,
                    TimestampUtc = now.UtcDateTime
                };
                yield return mt;
            }

            foreach (var source in context.Histograms)
            {
                yield return Translate(source, contextName, now);
            }

            foreach (var source in context.Meters)
            {
                yield return Translate(source, contextName, now);
            }

            foreach (var source in context.Timers)
            {
                yield return Translate(source, contextName, now);
            }
        }

        private static IEnumerable<MetricDatum> Translate(ApdexValueSource source, string contextName, DateTimeOffset now)
        {
            string metricName = source.IsMultidimensional ? source.MultidimensionalName : source.Name;
            yield return new MetricDatum
            {
                MetricName = $"{metricName}-{nameof(ApdexValue.Satisfied)}",
                TimestampUtc = now.UtcDateTime,
                Value = source.Value.Satisfied
            };
            yield return new MetricDatum
            {
                MetricName = $"{metricName}-{nameof(ApdexValue.Tolerating)}",
                TimestampUtc = now.UtcDateTime,
                Value = source.Value.Tolerating
            };
            yield return new MetricDatum
            {
                MetricName = $"{contextName}-{nameof(ApdexValue.Frustrating)}",
                TimestampUtc = now.UtcDateTime,
                Value = source.Value.Frustrating
            };
        }

        private static MetricDatum Translate(CounterValueSource source, string contextName, DateTimeOffset now)
        {
            string metricName = source.IsMultidimensional ? source.MultidimensionalName : source.Name;
            var mt = new MetricDatum
            {
                MetricName = metricName,
                TimestampUtc = now.UtcDateTime,
                Value = source.ValueProvider.GetValue(source.ResetOnReporting).Count,
            };

            if (source.ReportSetItems)
            {
                AddDimensionsFromTags(mt, source);
            }

            return mt;
        }

        private static MetricDatum Translate(HistogramValueSource source, string contextName, DateTimeOffset now)
        {
            string metricName = source.IsMultidimensional ? source.MultidimensionalName : source.Name;
            var mt = new MetricDatum
            {
                MetricName = metricName,
                TimestampUtc = now.UtcDateTime,
                StatisticValues = new StatisticSet
                {
                    Maximum = source.Value.Max,
                    Minimum = source.Value.Min,
                    SampleCount = source.Value.Count,
                    Sum = source.Value.Sum
                }
            };

            return mt;
        }

        private static MetricDatum Translate(MeterValueSource source, string contextName, DateTimeOffset now)
        {
            string metricName = source.IsMultidimensional ? source.MultidimensionalName : source.Name;
            var mt = new MetricDatum
            {
                MetricName = metricName,
                TimestampUtc = now.UtcDateTime,
                Value = source.ValueProvider.GetValue(source.ResetOnReporting).MeanRate,
                Dimensions = new List<Dimension>
                {
                    new Dimension { Name = nameof(source.Value.MeanRate), Value = source.ValueProvider.GetValue(source.ResetOnReporting).MeanRate.ToString() },
                    new Dimension { Name = nameof(source.Value.OneMinuteRate), Value = source.ValueProvider.GetValue(source.ResetOnReporting).OneMinuteRate.ToString() },
                    new Dimension { Name = nameof(source.Value.FiveMinuteRate), Value = source.ValueProvider.GetValue(source.ResetOnReporting).FiveMinuteRate.ToString() },
                    new Dimension { Name = nameof(source.Value.FifteenMinuteRate), Value = source.ValueProvider.GetValue(source.ResetOnReporting).FifteenMinuteRate.ToString() },
                }
            };

            return mt;
        }

        private static MetricDatum Translate(TimerValueSource source, string contextName, DateTimeOffset now)
        {
            string metricName = source.IsMultidimensional ? source.MultidimensionalName : source.Name;
            var mt = new MetricDatum
            {
                MetricName = metricName,
                TimestampUtc = now.UtcDateTime,
                Unit = source.DurationUnit.ToString(),
                StatisticValues = new StatisticSet
                {
                    Maximum = source.Value.Histogram.Max,
                    Minimum = source.Value.Histogram.Min,
                    SampleCount = source.Value.Histogram.Count,
                    Sum = source.Value.Histogram.Sum,
                    

                },
                Dimensions = new List<Dimension>
                {
                    new Dimension { Name = nameof(source.Value.Rate.MeanRate), Value = source.Value.Rate.MeanRate.ToString() },
                    new Dimension { Name = nameof(source.Value.Rate.OneMinuteRate), Value = source.Value.Rate.OneMinuteRate.ToString() },
                    new Dimension { Name = nameof(source.Value.Rate.FiveMinuteRate), Value = source.Value.Rate.FiveMinuteRate.ToString() },
                    new Dimension { Name = nameof(source.Value.Rate.FifteenMinuteRate), Value = source.Value.Rate.FifteenMinuteRate.ToString() },
                }
            };

            return mt;
        }

        private static void AddDimensionsFromTags(MetricDatum mt, CounterValueSource source)
        {
            int a = 0;
            foreach (string tagName in source.Tags.Keys)
            {
                mt.Dimensions.Add(new Dimension { Name = tagName, Value = source.Tags.Values[a] });
                a++;
            }
        }
    }
}
