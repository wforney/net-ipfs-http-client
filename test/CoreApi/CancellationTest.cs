namespace Ipfs.Http.Client.Tests.CoreApi;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class CancellationTest
{
    [TestMethod]
    public async Task Cancel_Operation()
    {
        var cs = new CancellationTokenSource(500);
        try
        {
            await Task.Delay(1000);
            var result = await TestFixture.IpfsContext.Generic.IdAsync(cancel: cs.Token);
            Assert.Fail("Did not throw TaskCanceledException");
        }
        catch (TaskCanceledException)
        {
            return;
        }
    }
}
