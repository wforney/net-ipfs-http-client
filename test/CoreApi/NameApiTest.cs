namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class NameApiTest
{
    [TestMethod]
    public void Api_Exists()
    {
        var ipfs = TestFixture.IpfsContext;
        Assert.IsNotNull(ipfs.Name);
    }

    [TestMethod]
    [Ignore("takes forever")]
    public async Task Publish()
    {
        var ipfs = TestFixture.IpfsContext;
        var cs = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var content = await ipfs.FileSystem.AddTextAsync("hello world");
        var key = await ipfs.Key.CreateAsync("name-publish-test", "rsa", 1024);
        try
        {
            var result = await ipfs.Name.PublishAsync(content.Id, key.Name, cancel: cs.Token);
            Assert.IsNotNull(result);
            StringAssert.EndsWith(result.NamePath, key.Id.ToString());
            StringAssert.EndsWith(result.ContentPath, content.Id.Encode());
        }
        finally
        {
            _ = await ipfs.Key.RemoveAsync(key.Name);
        }
    }

    [TestMethod]
    public async Task Resolve()
    {
        var ipfs = TestFixture.IpfsContext;
        var id = await ipfs.Name.ResolveAsync("ipfs.io", recursive: true);
        StringAssert.StartsWith(id, "/ipfs/");
    }
}
