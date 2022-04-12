namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class DhtApiTest
{
    private const string helloWorldID = "QmT78zSuBmuS4z925WZfrqQ1qHaJ56DQaTfyMUF7F8ff5o";

    [TestMethod]
    public async Task FindPeer()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var mars = await TestFixture.IpfsContext.Dht.FindPeerAsync("QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3", cts.Token);
        Assert.IsNotNull(mars);
    }

    [TestMethod]
    public async Task FindProviders()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var providers = await TestFixture.IpfsContext.Dht.FindProvidersAsync(helloWorldID, 1, cancel: cts.Token);
        Assert.AreNotEqual(0, providers.Count());
    }
}
