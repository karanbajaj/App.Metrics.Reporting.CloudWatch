# CloudWatch App.Metrics reporter

## Usage
1. Install nuget package: [App.Metrics.Reporting.CloudWatch](https://www.nuget.org/packages/App.Metrics.Reporting.CloudWatch/)
2. Configure your app:
```csharp
            services.AddMetrics(builder =>
            {
                builder
                       .Configuration
                       .ReadFrom(Configuration)
                       .Report
                       .ToCloudWatch();

            });
```

## Links
* [App.Metrics documentation](https://www.app-metrics.io/)
* [CloudWatch Dotnet SDK](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/cloudwatch.html)
