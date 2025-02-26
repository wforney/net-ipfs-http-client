﻿namespace Ipfs.Http.Client.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

[TestClass]
public partial class MerkleNodeTest
{
    private const string IpfsInfo = "QmVtU7ths96fMgZ8YSZAbKghyieq7AjxNdcqyVzxTt3qVe";

    [TestMethod]
    public void HashWithNamespace()
    {
        var node = new MerkleNode(TestFixture.IpfsContext, "/ipfs/" + IpfsInfo);
        Assert.AreEqual(IpfsInfo, (string)node.Id);
    }

    [TestMethod]
    public void Stringify()
    {
        var node = new MerkleNode(TestFixture.IpfsContext, IpfsInfo);
        Assert.AreEqual("/ipfs/" + IpfsInfo, node.ToString());
    }

    [TestMethod]
    public void FromString()
    {
        var a = new MerkleNode(TestFixture.IpfsContext, IpfsInfo);
        var b = (MerkleNode)IpfsInfo;
        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void NullHash()
    {
        ExceptionAssert.Throws<ArgumentNullException>(() => new MerkleNode(TestFixture.IpfsContext, (string)null));
        ExceptionAssert.Throws<ArgumentNullException>(() => new MerkleNode(TestFixture.IpfsContext, ""));
        ExceptionAssert.Throws<ArgumentNullException>(() => new MerkleNode(TestFixture.IpfsContext, (Cid)null));
    }

    [TestMethod]
    public void FromALink()
    {
        var node = new MerkleNode(TestFixture.IpfsContext, IpfsInfo);
        var link = new MerkleNode(TestFixture.IpfsContext, node.Links.First());
        Assert.AreEqual(link.Id, node.Links.First().Id);
        Assert.AreEqual(link.Name, node.Links.First().Name);
        Assert.AreEqual(link.BlockSize, node.Links.First().Size);
    }

    [TestMethod]
    public void ToALink()
    {
        var node = new MerkleNode(TestFixture.IpfsContext, IpfsInfo);
        var link = node.ToLink();
        Assert.AreEqual(link.Id, node.Id);
        Assert.AreEqual(link.Name, node.Name);
        Assert.AreEqual(link.Size, node.BlockSize);
    }

    [TestMethod]
    public void Value_Equality()
    {
        var a0 = new MerkleNode(TestFixture.IpfsContext, "QmStfpa7ppKPSsdnazBy3Q5QH4zNzGLcpWV88otjVSV7SY");
        var a1 = new MerkleNode(TestFixture.IpfsContext, "QmStfpa7ppKPSsdnazBy3Q5QH4zNzGLcpWV88otjVSV7SY");
        var b = new MerkleNode(TestFixture.IpfsContext, "QmagNHT6twJRBZcGeviiGzHVTMbNnJZameLyL6T14GUHCS");
        MerkleNode nullNode = null;

#pragma warning disable 1718
        Assert.IsTrue(a0 == a0);
        Assert.IsTrue(a0 == a1);
        Assert.IsFalse(a0 == b);
        Assert.IsFalse(a0 is null);

#pragma warning disable 1718
        Assert.IsFalse(a0 != a0);
        Assert.IsFalse(a0 != a1);
        Assert.IsTrue(a0 != b);
        Assert.IsTrue(a0 is not null);

        Assert.IsTrue(a0.Equals(a0));
        Assert.IsTrue(a0.Equals(a1));
        Assert.IsFalse(a0.Equals(b));
        Assert.IsFalse(a0.Equals(null));

        Assert.AreEqual(a0, a0);
        Assert.AreEqual(a0, a1);
        Assert.AreNotEqual(a0, b);
        Assert.AreNotEqual(a0, null);

        Assert.AreEqual(a0, a0);
        Assert.AreEqual(a0, a1);
        Assert.AreNotEqual(a0, b);
        Assert.AreNotEqual(a0, null);

        Assert.AreEqual(a0.GetHashCode(), a0.GetHashCode());
        Assert.AreEqual(a0.GetHashCode(), a1.GetHashCode());
        Assert.AreNotEqual(a0.GetHashCode(), b.GetHashCode());

        Assert.IsTrue(nullNode is null);
        Assert.IsFalse(null == a0);
        Assert.IsFalse(nullNode is not null);
        Assert.IsTrue(null != a0);
    }

    [TestMethod]
    public void DataBytes()
    {
        var node = new MerkleNode(TestFixture.IpfsContext, IpfsInfo);
        var data = node.DataBytes;
        Assert.AreEqual(node.BlockSize, data.Length);
    }

    [TestMethod]
    public void DataStream()
    {
        var node = new MerkleNode(TestFixture.IpfsContext, IpfsInfo);
        var data = node.DataBytes;
        var streamData = new MemoryStream();
        node.DataStream.CopyTo(streamData);
        CollectionAssert.AreEqual(data, streamData.ToArray());
    }
}
