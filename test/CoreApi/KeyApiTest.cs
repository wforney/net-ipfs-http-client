namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

[TestClass]
public class KeyApiTest
{
    private readonly int KeySize = 2048;

    [TestMethod]
    public void Api_Exists() => Assert.IsNotNull(TestFixture.IpfsContext.Key);

    [TestMethod]
    public async Task Create_RSA_Key()
    {
        var name = "net-api-test-create";
        try
        {
            var key = await TestFixture.IpfsContext.Key.CreateAsync(name, "rsa", this.KeySize);
            Assert.IsNotNull(key);
            Assert.IsNotNull(key.Id);
            Assert.AreEqual(name, key.Name);

            var keys = await TestFixture.IpfsContext.Key.ListAsync();
            var clone = keys.Single(k => k.Name == name);
            Assert.AreEqual(key.Name, clone.Name);
            Assert.AreEqual(key.Id, clone.Id);
        }
        finally
        {
            _ = await TestFixture.IpfsContext.Key.RemoveAsync(name);
        }
    }

    [TestMethod]
    public async Task Remove_Key()
    {
        var name = "net-api-test-remove";
        var key = await TestFixture.IpfsContext.Key.CreateAsync(name, "rsa", this.KeySize);
        var keys = await TestFixture.IpfsContext.Key.ListAsync();
        var clone = keys.Single(k => k.Name == name);
        Assert.IsNotNull(clone);

        var removed = await TestFixture.IpfsContext.Key.RemoveAsync(name);
        Assert.IsNotNull(removed);
        Assert.AreEqual(key.Name, removed.Name);
        Assert.AreEqual(key.Id, removed.Id);

        keys = await TestFixture.IpfsContext.Key.ListAsync();
        Assert.IsFalse(keys.Any(k => k.Name == name));
    }

    [TestMethod]
    public async Task Rename_Key()
    {
        var oname = "net-api-test-rename1";
        var rname = "net-api-test-rename2";
        try
        {
            var okey = await TestFixture.IpfsContext.Key.CreateAsync(oname, "rsa", this.KeySize);
            Assert.AreEqual(oname, okey.Name);

            var rkey = await TestFixture.IpfsContext.Key.RenameAsync(oname, rname);
            Assert.AreEqual(okey.Id, rkey.Id);
            Assert.AreEqual(rname, rkey.Name);

            var keys = await TestFixture.IpfsContext.Key.ListAsync();
            Assert.IsTrue(keys.Any(k => k.Name == rname));
            Assert.IsFalse(keys.Any(k => k.Name == oname));
        }
        finally
        {
            try
            {
                _ = await TestFixture.IpfsContext.Key.RemoveAsync(oname);
            }
            catch (Exception) { }

            try
            {
                _ = await TestFixture.IpfsContext.Key.RemoveAsync(rname);
            }
            catch (Exception) { }
        }
    }

    [TestMethod]
    public async Task Self_Key_Exists()
    {
        var ipfs = TestFixture.IpfsContext;
        var keys = await ipfs.Key.ListAsync();
        var self = keys.Single(k => k.Name == "self");
        var me = await ipfs.Generic.IdAsync();
        Assert.AreEqual("self", self.Name);
        Assert.AreEqual(me.Id, self.Id);
    }
}
