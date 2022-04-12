namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

[TestClass]
public class DagApiTest
{
    [TestMethod]
    public async Task PutAndGet_JSON()
    {
        var expected = new JObject
        {
            ["a"] = "alpha"
        };
        var expectedId = "bafyreigdhej736dobd6z3jt2vxsxvbwrwgyts7e7wms6yrr46rp72uh5bu";
        var id = await TestFixture.IpfsContext.Dag.PutAsync(expected);
        Assert.IsNotNull(id);
        Assert.AreEqual(expectedId, (string)id);

        var actual = await TestFixture.IpfsContext.Dag.GetAsync(id);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected["a"], actual["a"]);

        var value = (string)await TestFixture.IpfsContext.Dag.GetAsync(expectedId + "/a");
        Assert.AreEqual(expected["a"], value);
    }

    [TestMethod]
    public async Task PutAndGet_POCO()
    {
        var expected = new Name { First = "John", Last = "Smith" };
        var id = await TestFixture.IpfsContext.Dag.PutAsync(expected);
        Assert.IsNotNull(id);

        var actual = await TestFixture.IpfsContext.Dag.GetAsync<Name>(id);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.First, actual.First);
        Assert.AreEqual(expected.Last, actual.Last);

        var value = (string)await TestFixture.IpfsContext.Dag.GetAsync(id.Encode() + "/Last");
        Assert.AreEqual(expected.Last, value);
    }

    private class Name
    {
        public string First { get; set; }
        public string Last { get; set; }
    }
}
