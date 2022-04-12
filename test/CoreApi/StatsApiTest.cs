namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

[TestClass]
public class StatsApiTest
{
    [TestMethod]
    public async Task SmokeTest()
    {
        var ipfs = TestFixture.IpfsContext;
        var bandwidth = await ipfs.Stats.BandwidthAsync();
        var bitswap = await ipfs.Stats.BitswapAsync();
        var repository = await ipfs.Stats.RepositoryAsync();
    }
}
