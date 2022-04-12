namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

[TestClass]
public class BlockRepositoryTest
{
    [TestMethod]
    public async Task Stats()
    {
        var stats = await TestFixture.IpfsContext.BlockRepository.StatisticsAsync();
        Assert.IsNotNull(stats);
    }

    [TestMethod]
    public async Task Version()
    {
        var version = await TestFixture.IpfsContext.BlockRepository.VersionAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(version));
    }
}
