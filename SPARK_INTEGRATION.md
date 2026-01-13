# Spark Integration for Customer Reviews

## Overview

This document describes the Apache Spark integration added to the Customer Reviews Azure Functions application. The integration provides batch analytics capabilities for processing and analyzing customer review data at scale.

## Architecture

The Spark integration is implemented through a new Azure Function called `SparkReviewAnalyzer` that runs on a scheduled timer (daily at midnight by default).

### Components

1. **SparkReviewAnalyzer.cs** - Timer-triggered Azure Function that:
   - Retrieves review data from CosmosDB
   - Performs Spark-style distributed analytics
   - Logs aggregated metrics to Application Insights

2. **Microsoft.Spark Package** - .NET for Apache Spark library (v2.1.1) enables:
   - Integration with Apache Spark clusters
   - DataFrame-based data processing
   - Distributed computing capabilities

## Features

The Spark analytics function calculates:
- Total number of reviews processed
- Count of approved vs rejected reviews
- Overall approval rate
- Average caption length

## Configuration

### Settings

Add the following configuration to your `local.settings.json`:

```json
{
  "Values": {
    "DaysAgo": "7"  // Number of days of historical data to analyze
  }
}
```

### Timer Schedule

The function runs on a CRON schedule: `0 0 0 * * *` (daily at midnight UTC)

To modify the schedule, update the `TimerTrigger` attribute in `SparkReviewAnalyzer.cs`.

## Deployment Considerations

### Local Development
In local development, the function uses in-memory LINQ operations that simulate Spark's distributed processing patterns.

### Production Deployment
For production use with large datasets, connect to:
- **Azure Databricks** - Managed Spark service on Azure
- **Azure HDInsight** - Open-source Spark clusters
- **Synapse Analytics** - Unified analytics platform

Update the `AnalyzeReviewData` method to create a `SparkSession` and connect to your Spark cluster:

```csharp
var spark = SparkSession.Builder()
    .AppName("ReviewAnalytics")
    .GetOrCreate();
```

## Testing

A unit test `TestSparkReviewAnalytics` has been added to verify the analytics calculations work correctly with sample data.

Run tests with:
```bash
dotnet test ContentModeratorFunction.Tests/ContentModeratorFunction.Tests.csproj
```

## Monitoring

Review analytics results are logged to Application Insights with:
- Total reviews processed
- Approval/rejection counts
- Approval rate percentage
- Average caption length

View these metrics in the Azure Portal under Application Insights > Logs.

## Future Enhancements

Potential improvements for the Spark integration:
1. Real-time streaming analytics with Spark Structured Streaming
2. Machine learning model training on review data
3. Sentiment analysis using Spark MLlib
4. Advanced text analytics and pattern detection
5. Integration with Power BI for visualization

## Dependencies

- Microsoft.Spark v2.1.1 - .NET for Apache Spark
- Microsoft.Azure.WebJobs.Extensions.CosmosDB v3.0.1 - CosmosDB bindings
- Microsoft.Azure.WebJobs.Logging.ApplicationInsights v3.0.0 - Telemetry

## References

- [.NET for Apache Spark Documentation](https://docs.microsoft.com/en-us/dotnet/spark/)
- [Azure Databricks](https://azure.microsoft.com/en-us/services/databricks/)
- [Apache Spark](https://spark.apache.org/)
