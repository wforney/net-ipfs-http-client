namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[TestClass]
public class BlockApiTest
{
    private readonly string id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
    private readonly byte[] blob = Encoding.UTF8.GetBytes("blorb");

    [TestMethod]
    public void Put_Bytes()
    {
        var cid = TestFixture.IpfsContext.Block.PutAsync(this.blob).Result;
        Assert.AreEqual(this.id, (string)cid);

        var data = TestFixture.IpfsContext.Block.GetAsync(cid).Result;
        Assert.AreEqual(this.blob.Length, data.Size);
        CollectionAssert.AreEqual(this.blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_ContentType()
    {
        var cid = TestFixture.IpfsContext.Block.PutAsync(this.blob, contentType: "raw").Result;
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var data = TestFixture.IpfsContext.Block.GetAsync(cid).Result;
        Assert.AreEqual(this.blob.Length, data.Size);
        CollectionAssert.AreEqual(this.blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_Hash()
    {
        var cid = TestFixture.IpfsContext.Block.PutAsync(this.blob, "raw", "sha2-512").Result;
        Assert.AreEqual("bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e", (string)cid);

        var data = TestFixture.IpfsContext.Block.GetAsync(cid).Result;
        Assert.AreEqual(this.blob.Length, data.Size);
        CollectionAssert.AreEqual(this.blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Bytes_Pinned()
    {
        var data1 = new byte[] { 23, 24, 127 };
        var cid1 = TestFixture.IpfsContext.Block.PutAsync(data1, contentType: "raw", pin: true).Result;
        var pins = TestFixture.IpfsContext.Pin.ListAsync().Result;
        Assert.IsTrue(pins.Any(pin => pin == cid1));

        var data2 = new byte[] { 123, 124, 27 };
        var cid2 = TestFixture.IpfsContext.Block.PutAsync(data2, contentType: "raw", pin: false).Result;
        pins = TestFixture.IpfsContext.Pin.ListAsync().Result;
        Assert.IsFalse(pins.Any(pin => pin == cid2));
    }

    [TestMethod]
    public void Put_Stream()
    {
        var cid = TestFixture.IpfsContext.Block.PutAsync(new MemoryStream(this.blob)).Result;
        Assert.AreEqual(this.id, (string)cid);

        var data = TestFixture.IpfsContext.Block.GetAsync(cid).Result;
        Assert.AreEqual(this.blob.Length, data.Size);
        CollectionAssert.AreEqual(this.blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_ContentType()
    {
        var cid = TestFixture.IpfsContext.Block.PutAsync(new MemoryStream(this.blob), contentType: "raw").Result;
        Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

        var data = TestFixture.IpfsContext.Block.GetAsync(cid).Result;
        Assert.AreEqual(this.blob.Length, data.Size);
        CollectionAssert.AreEqual(this.blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_Hash()
    {
        var cid = TestFixture.IpfsContext.Block.PutAsync(new MemoryStream(this.blob), "raw", "sha2-512").Result;
        Assert.AreEqual("bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e", (string)cid);

        var data = TestFixture.IpfsContext.Block.GetAsync(cid).Result;
        Assert.AreEqual(this.blob.Length, data.Size);
        CollectionAssert.AreEqual(this.blob, data.DataBytes);
    }

    [TestMethod]
    public void Put_Stream_Pinned()
    {
        var data1 = new MemoryStream(new byte[] { 23, 24, 127 });
        var cid1 = TestFixture.IpfsContext.Block.PutAsync(data1, contentType: "raw", pin: true).Result;
        var pins = TestFixture.IpfsContext.Pin.ListAsync().Result;
        Assert.IsTrue(pins.Any(pin => pin == cid1));

        var data2 = new MemoryStream(new byte[] { 123, 124, 27 });
        var cid2 = TestFixture.IpfsContext.Block.PutAsync(data2, contentType: "raw", pin: false).Result;
        pins = TestFixture.IpfsContext.Pin.ListAsync().Result;
        Assert.IsFalse(pins.Any(pin => pin == cid2));
    }

    [TestMethod]
    public void Get()
    {
        var _ = TestFixture.IpfsContext.Block.PutAsync(this.blob).Result;
        var block = TestFixture.IpfsContext.Block.GetAsync(this.id).Result;
        Assert.AreEqual(this.id, (string)block.Id);
        CollectionAssert.AreEqual(this.blob, block.DataBytes);
        var blob1 = new byte[this.blob.Length];
        block.DataStream.Read(blob1, 0, blob1.Length);
        CollectionAssert.AreEqual(this.blob, blob1);
    }

    [TestMethod]
    public void Stat()
    {
        var _ = TestFixture.IpfsContext.Block.PutAsync(this.blob).Result;
        var info = TestFixture.IpfsContext.Block.StatAsync(this.id).Result;
        Assert.AreEqual(this.id, (string)info.Id);
        Assert.AreEqual(5, info.Size);
    }

    [TestMethod]
    public async Task Remove()
    {
        var _ = TestFixture.IpfsContext.Block.PutAsync(this.blob).Result;
        var cid = await TestFixture.IpfsContext.Block.RemoveAsync(this.id);
        Assert.AreEqual(this.id, (string)cid);
    }

    [TestMethod]
    public void Remove_Unknown() => ExceptionAssert.Throws<Exception>(() => { var _ = TestFixture.IpfsContext.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result; });

    [TestMethod]
    public async Task Remove_Unknown_OK()
    {
        var cid = await TestFixture.IpfsContext.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
    }

}
