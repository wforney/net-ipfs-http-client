namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

[TestClass]
public class ConfigApiTest
{
    private const string apiAddress = "/ip4/127.0.0.1/tcp/";
    private const string gatewayAddress = "/ip4/127.0.0.1/tcp/";

    [TestMethod]
    public void Get_Entire_Config()
    {
        var config = TestFixture.IpfsContext.Config.GetAsync().Result;
        StringAssert.StartsWith(config["Addresses"]["API"].Value<string>(), apiAddress);
    }

    [TestMethod]
    public void Get_Object_Key_Value()
    {
        var addresses = TestFixture.IpfsContext.Config.GetAsync("Addresses").Result;
        StringAssert.StartsWith(addresses["API"].Value<string>(), apiAddress);
        StringAssert.StartsWith(addresses["Gateway"].Value<string>(), gatewayAddress);
    }

    [TestMethod]
    public void Get_Scalar_Key_Value()
    {
        var api = TestFixture.IpfsContext.Config.GetAsync("Addresses.API").Result;
        StringAssert.StartsWith(api.Value<string>(), apiAddress);
    }

    [TestMethod]
    public void Keys_are_Case_Sensitive()
    {
        var api = TestFixture.IpfsContext.Config.GetAsync("Addresses.API").Result;
        StringAssert.StartsWith(api.Value<string>(), apiAddress);

        ExceptionAssert.Throws<Exception>(() => { var x = TestFixture.IpfsContext.Config.GetAsync("Addresses.api").Result; });
    }

    [TestMethod]
    public async Task Replace_Entire_Config()
    {
        var original = await TestFixture.IpfsContext.Config.GetAsync();
        try
        {
            var a = JObject.Parse("{ \"foo-x-bar\": 1 }");
            await TestFixture.IpfsContext.Config.ReplaceAsync(a);
        }
        finally
        {
            await TestFixture.IpfsContext.Config.ReplaceAsync(original);
        }
    }

    [TestMethod]
    public void Set_JSON_Value()
    {
        const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
        var value = JToken.Parse("['http://example.io']");
        TestFixture.IpfsContext.Config.SetAsync(key, value).Wait();
        Assert.AreEqual("http://example.io", TestFixture.IpfsContext.Config.GetAsync(key).Result[0]);
    }

    [TestMethod]
    public void Set_String_Value()
    {
        const string key = "foo";
        const string value = "foobar";
        TestFixture.IpfsContext.Config.SetAsync(key, value).Wait();
        Assert.AreEqual(value, TestFixture.IpfsContext.Config.GetAsync(key).Result);
    }
}
