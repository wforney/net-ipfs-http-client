namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

[TestClass]
public class PinApiTest
{
    [TestMethod]
    public async Task Add_Remove()
    {
        var ipfs = TestFixture.IpfsContext;
        var result = await ipfs.FileSystem.AddTextAsync("I am pinned");
        var id = result.Id;

        var pins = await ipfs.Pin.AddAsync(id);
        Assert.IsTrue(pins.Any(pin => pin == id));
        var all = await ipfs.Pin.ListAsync();
        Assert.IsTrue(all.Any(pin => pin == id));

        pins = await ipfs.Pin.RemoveAsync(id);
        Assert.IsTrue(pins.Any(pin => pin == id));
        all = await ipfs.Pin.ListAsync();
        Assert.IsFalse(all.Any(pin => pin == id));
    }

    [TestMethod]
    public void List()
    {
        var ipfs = TestFixture.IpfsContext;
        var pins = ipfs.Pin.ListAsync().Result;
        Assert.IsNotNull(pins);
        Assert.IsTrue(pins.Count() > 0);
    }
}
