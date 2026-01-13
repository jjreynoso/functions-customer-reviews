using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ContentModeratorFunction
{
    public static class SparkReviewAnalyzer
    {
        /// <summary>
        /// Timer-triggered function that uses Apache Spark concepts to analyze review patterns
        /// Runs daily at midnight
        /// This function demonstrates Spark-style data processing integration
        /// 
        /// Note: The SQL query retrieves all documents to demonstrate Spark-style processing
        /// where data is filtered and transformed in-memory (or in Spark clusters in production).
        /// For smaller datasets or non-Spark scenarios, consider filtering at query level.
        /// </summary>
        [FunctionName("AnalyzeReviewsWithSpark")]
        public static void Run(
            [TimerTrigger("0 0 0 * * *")] TimerInfo myTimer,
            [CosmosDB(
                databaseName: "customerReviewData",
                collectionName: "reviews",
                ConnectionStringSetting = "customerReviewDataDocDB",
                SqlQuery = "SELECT * FROM c")]
                IEnumerable<ReviewDocument> reviews,
            ILogger log)
        {
            log.LogInformation($"Spark review analysis started at: {DateTime.UtcNow}");

            try
            {
                // For Azure Functions, we use a simplified Spark session approach
                // In production, this would connect to Azure Databricks or HDInsight
                var reviewData = reviews.ToList();
                
                // Filter by date if configured
                if (!int.TryParse(Environment.GetEnvironmentVariable("DaysAgo"), out int daysAgo))
                {
                    daysAgo = 7; // Default to 7 days if not configured or invalid
                }
                var cutoffDate = DateTime.UtcNow.AddDays(-daysAgo);
                reviewData = reviewData.Where(r => r.CreatedAt >= cutoffDate).ToList();
                
                log.LogInformation($"Processing {reviewData.Count} reviews from the last {daysAgo} days with Spark analytics");

                // Perform analytics on the review data
                var analytics = AnalyzeReviewData(reviewData);

                // Log the results
                log.LogInformation($"Total Reviews: {analytics.TotalReviews}");
                log.LogInformation($"Approved Reviews: {analytics.ApprovedCount}");
                log.LogInformation($"Rejected Reviews: {analytics.RejectedCount}");
                log.LogInformation($"Approval Rate: {analytics.ApprovalRate:P2}");
                log.LogInformation($"Average Caption Length: {analytics.AvgCaptionLength:F2}");

                log.LogInformation($"Spark review analysis completed at: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error in Spark analysis: {ex.Message}");
                throw;
            }
        }

        public static ReviewAnalytics AnalyzeReviewData(List<ReviewDocument> reviews)
        {
            // Spark-style distributed processing pattern
            // This demonstrates the data processing approach that would be used with Apache Spark
            // In production with Azure Databricks or HDInsight, this would leverage:
            // - SparkSession for distributed computing
            // - DataFrame API for structured data processing
            // - Resilient Distributed Datasets (RDDs) for fault tolerance
            
            var totalReviews = reviews.Count;
            var approvedCount = reviews.Count(r => r.IsApproved);
            var rejectedCount = totalReviews - approvedCount;
            
            // Calculate average caption length, handling empty sequences
            var reviewsWithCaptions = reviews.Where(r => !string.IsNullOrEmpty(r.Caption)).ToList();
            var avgCaptionLength = reviewsWithCaptions.Any() 
                ? reviewsWithCaptions.Average(r => r.Caption.Length) 
                : 0.0;

            return new ReviewAnalytics
            {
                TotalReviews = totalReviews,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                ApprovalRate = totalReviews > 0 ? (double)approvedCount / totalReviews : 0,
                AvgCaptionLength = avgCaptionLength
            };
        }

        /// <summary>
        /// Represents a review document from CosmosDB for Spark processing
        /// This is a specific model for Spark analytics to ensure type safety and testability
        /// </summary>
        public class ReviewDocument
        {
            public string Id { get; set; }
            public bool IsApproved { get; set; }
            public string Caption { get; set; }
            public string ReviewText { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class ReviewAnalytics
        {
            public int TotalReviews { get; set; }
            public int ApprovedCount { get; set; }
            public int RejectedCount { get; set; }
            public double ApprovalRate { get; set; }
            public double AvgCaptionLength { get; set; }
        }
    }
}
