namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class DnsApiTest
{
    [TestMethod]
    public void Api_Exists() => Assert.IsNotNull(TestFixture.IpfsContext.Dns);

    [TestMethod]
    [Ignore("takes forever")]
    public async Task Publish()
    {
        var cs = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var content = await TestFixture.IpfsContext.FileSystem.AddTextAsync("hello world");
        var key = await TestFixture.IpfsContext.Key.CreateAsync("name-publish-test", "rsa", 1024);
        try
        {
            var result = await TestFixture.IpfsContext.Name.PublishAsync(content.Id, key.Name, cancel: cs.Token);
            Assert.IsNotNull(result);
            StringAssert.EndsWith(result.NamePath, key.Id.ToString());
            StringAssert.EndsWith(result.ContentPath, content.Id.Encode());
        }
        finally
        {
            await TestFixture.IpfsContext.Key.RemoveAsync(key.Name);
        }
    }

    [TestMethod]
    public async Task Resolve()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var path = await TestFixture.IpfsContext.Dns.ResolveAsync("ipfs.io", recursive: true, cancel: cts.Token);
        StringAssert.StartsWith(path, "/ipfs/");
    }
}
