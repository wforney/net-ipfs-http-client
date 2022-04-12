namespace Ipfs.Http.Client.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BlockTest
{
    private readonly byte[] someBytes = new byte[] { 1, 2, 3 };

    [TestMethod]
    public void DataBytes()
    {
        var block = new Block
        {
            DataBytes = someBytes
        };
        CollectionAssert.AreEqual(this.someBytes, block.DataBytes);
    }

    [TestMethod]
    public void DataStream()
    {
        var block = new Block
        {
            DataBytes = someBytes
        };
        var stream = block.DataStream;
        Assert.AreEqual(1, stream.ReadByte());
        Assert.AreEqual(2, stream.ReadByte());
        Assert.AreEqual(3, stream.ReadByte());
        Assert.AreEqual(-1, stream.ReadByte(), "at eof");
    }
}
