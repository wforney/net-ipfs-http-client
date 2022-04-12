namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

[TestClass]
public class BootstapApiTest
{
    private readonly MultiAddress somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

    [TestMethod]
    public async Task Add_Remove()
    {
        var addr = await TestFixture.IpfsContext.Bootstrap.AddAsync(this.somewhere);
        Assert.IsNotNull(addr);
        Assert.AreEqual(this.somewhere, addr);
        var addrs = await TestFixture.IpfsContext.Bootstrap.ListAsync();
        Assert.IsTrue(addrs.Any(a => a == this.somewhere));

        addr = await TestFixture.IpfsContext.Bootstrap.RemoveAsync(this.somewhere);
        Assert.IsNotNull(addr);
        Assert.AreEqual(this.somewhere, addr);
        addrs = await TestFixture.IpfsContext.Bootstrap.ListAsync();
        Assert.IsFalse(addrs.Any(a => a == this.somewhere));
    }

    [TestMethod]
    public async Task List()
    {
        var addrs = await TestFixture.IpfsContext.Bootstrap.ListAsync();
        Assert.IsNotNull(addrs);
        Assert.AreNotEqual(0, addrs.Count());
    }

    [TestMethod]
    public async Task Remove_All()
    {
        var original = await TestFixture.IpfsContext.Bootstrap.ListAsync();
        await TestFixture.IpfsContext.Bootstrap.RemoveAllAsync();
        var addrs = await TestFixture.IpfsContext.Bootstrap.ListAsync();
        Assert.AreEqual(0, addrs.Count());
        foreach (var addr in original)
        {
            await TestFixture.IpfsContext.Bootstrap.AddAsync(addr);
        }
    }

    [TestMethod]
    public async Task Add_Defaults()
    {
        var original = await TestFixture.IpfsContext.Bootstrap.ListAsync();
        await TestFixture.IpfsContext.Bootstrap.RemoveAllAsync();
        try
        {
            await TestFixture.IpfsContext.Bootstrap.AddDefaultsAsync();
            var addrs = await TestFixture.IpfsContext.Bootstrap.ListAsync();
            Assert.AreNotEqual(0, addrs.Count());
        }
        finally
        {
            await TestFixture.IpfsContext.Bootstrap.RemoveAllAsync();
            foreach (var addr in original)
            {
                await TestFixture.IpfsContext.Bootstrap.AddAsync(addr);
            }
        }
    }
}
