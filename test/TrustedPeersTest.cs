namespace Ipfs.Http;

using Ipfs.Http.Client.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

public partial class IpfsClientTest
{
    private readonly MultiAddress newTrustedPeer = new("/ip4/25.196.147.100/tcp/4001/ipfs/QmaMqSwWShsPg2RbredZtoneFjXhim7AQkqbLxib45Lx4S");

    [TestMethod]
    public void Trusted_Peers_List()
    {
        Assert.IsNotNull(TestFixture.Ipfs.TrustedPeers);
        Assert.IsTrue(TestFixture.Ipfs.TrustedPeers.Count > 0);
    }

    [TestMethod]
    public void Trusted_Peers_Add_Remove()
    {
        if (TestFixture.Ipfs.TrustedPeers.Contains(this.newTrustedPeer))
        {
            _ = TestFixture.Ipfs.TrustedPeers.Remove(this.newTrustedPeer);
        }

        Assert.IsFalse(TestFixture.Ipfs.TrustedPeers.Contains(this.newTrustedPeer));
        TestFixture.Ipfs.TrustedPeers.Add(this.newTrustedPeer);
        Assert.IsTrue(TestFixture.Ipfs.TrustedPeers.Contains(this.newTrustedPeer));
        Assert.IsTrue(TestFixture.Ipfs.TrustedPeers.Remove(this.newTrustedPeer));
        Assert.IsFalse(TestFixture.Ipfs.TrustedPeers.Contains(this.newTrustedPeer));
    }

    // js-ipfs does NOT check IPFS addresses.
    // And this bad addr eventually breaks the server.
    // https://github.com/ipfs/js-ipfs/issues/1066
#if false
    [TestMethod]
    public void Trusted_Peers_Add_Missing_Peer_ID()
    {
        var missingPeerId = new MultiAddress("/ip4/25.196.147.100/tcp/4001");
        var ipfs = TestFixture.Ipfs;
        ExceptionAssert.Throws<Exception>(() => ipfs.TrustedPeers.Add(missingPeerId), "invalid IPFS address");
    }
#endif

    [TestMethod]
    public void Trusted_Peers_Clear()
    {
        var original = TestFixture.Ipfs.TrustedPeers.ToArray();
        TestFixture.Ipfs.TrustedPeers.Clear();
        Assert.AreEqual(0, TestFixture.Ipfs.TrustedPeers.Count);

        foreach (var a in original)
        {
            TestFixture.Ipfs.TrustedPeers.Add(a);
        }
    }

    [TestMethod]
    public void Trusted_Peers_Add_Default_Nodes()
    {
        var ipfs = TestFixture.Ipfs;
        var original = ipfs.TrustedPeers.ToArray();

        ipfs.TrustedPeers.Clear();
        Assert.AreEqual(0, ipfs.TrustedPeers.Count);
        ipfs.TrustedPeers.AddDefaultNodes();
        Assert.AreNotEqual(0, ipfs.TrustedPeers.Count);

        ipfs.TrustedPeers.Clear();
        foreach (var a in original)
        {
            ipfs.TrustedPeers.Add(a);
        }
    }
}
