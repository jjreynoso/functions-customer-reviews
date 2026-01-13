using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace ContentModeratorFunction.Tests
{
    public class ReviewPoco
    {
        public string ReviewText { get; set; }
    }

    public class AppSettingsFile
    {
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    [TestClass]
    public class TestContent
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            using (StreamReader r = new StreamReader("local.settings.json"))
            {
                string json = r.ReadToEnd();
                var appsettings = JsonConvert.DeserializeObject<AppSettingsFile>(json);

                foreach (var keyValue in appsettings.Values)
                {
                    Environment.SetEnvironmentVariable(keyValue.Key, keyValue.Value);
                }
            }
        }

        [TestMethod]
        public async Task TestTextModeration()
        {
            bool passes = await AnalyzeImage.PassesTextModeratorAsync(new ReviewPoco { ReviewText = "Donna" });

            Assert.IsTrue(passes);
        }

        [TestMethod]
        public async Task TestImageModeration()
        {
            using (var stream = new FileStream(@"TestImages\moxie.jpg", FileMode.Open))
            {
                var response = await AnalyzeImage.PassesImageModerationAsync(stream);

                Assert.IsTrue(response.Item1);
            }
        }

        [TestMethod]
        public void TestSparkReviewAnalytics()
        {
            // Test the Spark analytics with sample data
            var reviews = new List<SparkReviewAnalyzer.ReviewDocument>
            {
                new SparkReviewAnalyzer.ReviewDocument
                {
                    Id = "1",
                    IsApproved = true,
                    Caption = "a small cat sitting on a couch",
                    ReviewText = "Great product!",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new SparkReviewAnalyzer.ReviewDocument
                {
                    Id = "2",
                    IsApproved = false,
                    Caption = "a small dog sitting on a couch",
                    ReviewText = "Not what I expected",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new SparkReviewAnalyzer.ReviewDocument
                {
                    Id = "3",
                    IsApproved = true,
                    Caption = "a large cat sleeping",
                    ReviewText = "Perfect!",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            // Call the private method via reflection to test analytics logic
            var method = typeof(SparkReviewAnalyzer).GetMethod("AnalyzeReviewData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = method.Invoke(null, new object[] { reviews }) as SparkReviewAnalyzer.ReviewAnalytics;

            // Verify analytics results
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.TotalReviews);
            Assert.AreEqual(2, result.ApprovedCount);
            Assert.AreEqual(1, result.RejectedCount);
            Assert.AreEqual(0.6666, result.ApprovalRate, 0.001);
            Assert.IsTrue(result.AvgCaptionLength > 0);
        }
    }
}
