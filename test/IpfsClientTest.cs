namespace Ipfs.Http;

using Ipfs.Http.Client.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

/// <summary>
///This is a test class for IpfsClientTest and is intended
///to contain all IpfsClientTest Unit Tests
///</summary>
[TestClass]
public partial class IpfsClientTest
{
    /// <summary>
    ///   A test for IpfsClient Constructor
    ///</summary>
    [TestMethod]
    public void Can_Create()
    {
        var target = TestFixture.Ipfs;
        Assert.IsNotNull(target);
    }

    [TestMethod]
    public void Do_Command_Throws_Exception_On_Invalid_Command()
    {
        var target = TestFixture.Ipfs;
        object unknown;
        ExceptionAssert.Throws<HttpRequestException>(() => unknown = target.ExecuteCommand<string?>("foobar", default).Result);
    }

    [TestMethod]
    public void Do_Command_Throws_Exception_On_Missing_Argument()
    {
        var target = TestFixture.Ipfs;
        object unknown;
        ExceptionAssert.Throws<HttpRequestException>(() => unknown = target.ExecuteCommand<string?>("key/gen", default).Result);
    }
}
