using NUnit.Framework;

namespace Unido.Tests
{
    public class DownloadServiceCreationTests
    {
        [Test]
        public void DownloadServiceCreationWithLessThanZeroTimeoutTest()
        {
            DownloadServiceConfig mockConfig = new DownloadServiceConfig();
            mockConfig.Timeout = -5;
            using var ds = new DownloadService(mockConfig);
            Assert.Greater(ds.Timeout, 0);
        }

        [Test]
        public void DownloadServiceCreationDefaultDownloadOptionsIsNotNullTest()
        {
            using var ds = new DownloadService();
            Assert.IsNotNull(ds.DefaultDownloadOptions);
        }
    }
}
