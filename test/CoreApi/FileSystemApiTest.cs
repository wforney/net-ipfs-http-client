namespace Ipfs.Http.Client.Tests.CoreApi;

using Ipfs.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class FileSystemApiTest
{
    [TestMethod]
    public void Add_NoPin()
    {
        var data = new MemoryStream(new byte[] { 11, 22, 33 });
        var options = new AddFileOptions { Pin = false };
        var node = TestFixture.IpfsContext.FileSystem.AddAsync(data, "", options).Result;
        var pins = TestFixture.IpfsContext.Pin.ListAsync().Result;
        Assert.IsFalse(pins.Any(pin => pin == node.Id));
    }

    [TestMethod]
    public async Task Add_Raw()
    {
        var options = new AddFileOptions
        {
            RawLeaves = true
        };
        var node = await TestFixture.IpfsContext.FileSystem.AddTextAsync("hello world", options);
        Assert.AreEqual("bafkreifzjut3te2nhyekklss27nh3k72ysco7y32koao5eei66wof36n5e", (string)node.Id);
        Assert.AreEqual(11, node.Size);

        var text = await TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(node.Id);
        Assert.AreEqual("hello world", text);
    }

    [TestMethod]
    public async Task Add_RawAndChunked()
    {
        var options = new AddFileOptions
        {
            RawLeaves = true,
            ChunkSize = 3
        };
        var node = await TestFixture.IpfsContext.FileSystem.AddTextAsync("hello world", options);
        Assert.AreEqual("QmUuooB6zEhMmMaBvMhsMaUzar5gs5KwtVSFqG4C1Qhyhs", (string)node.Id);
        Assert.AreEqual(false, node.IsDirectory);

        var links = (await TestFixture.IpfsContext.Object.LinksAsync(node.Id)).ToArray();
        Assert.AreEqual(4, links.Length);
        Assert.AreEqual("bafkreigwvapses57f56cfow5xvoua4yowigpwcz5otqqzk3bpcbbjswowe", (string)links[0].Id);
        Assert.AreEqual("bafkreiew3cvfrp2ijn4qokcp5fqtoknnmr6azhzxovn6b3ruguhoubkm54", (string)links[1].Id);
        Assert.AreEqual("bafkreibsybcn72tquh2l5zpim2bba4d2kfwcbpzuspdyv2breaq5efo7tq", (string)links[2].Id);
        Assert.AreEqual("bafkreihfuch72plvbhdg46lef3n5zwhnrcjgtjywjryyv7ffieyedccchu", (string)links[3].Id);

        var text = await TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(node.Id);
        Assert.AreEqual("hello world", text);
    }

    [TestMethod]
    public async Task Add_SizeChunking()
    {
        var options = new AddFileOptions
        {
            ChunkSize = 3
        };
        options.Pin = true;
        var node = await TestFixture.IpfsContext.FileSystem.AddTextAsync("hello world", options);
        Assert.AreEqual("QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn", (string)node.Id);
        Assert.AreEqual(false, node.IsDirectory);

        var links = (await TestFixture.IpfsContext.Object.LinksAsync(node.Id)).ToArray();
        Assert.AreEqual(4, links.Length);
        Assert.AreEqual("QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6", (string)links[0].Id);
        Assert.AreEqual("QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN", (string)links[1].Id);
        Assert.AreEqual("QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6", (string)links[2].Id);
        Assert.AreEqual("QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE", (string)links[3].Id);

        var text = await TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(node.Id);
        Assert.AreEqual("hello world", text);
    }

    [TestMethod]
    public async Task Add_Wrap()
    {
        var path = "hello.txt";
        File.WriteAllText(path, "hello world");
        try
        {
            var options = new AddFileOptions
            {
                Wrap = true
            };
            var node = await TestFixture.IpfsContext.FileSystem.AddFileAsync(path, options);
            Assert.AreEqual("QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ", (string)node.Id);
            Assert.AreEqual(true, node.IsDirectory);
            Assert.AreEqual(1, node.Links.Count());
            Assert.AreEqual("hello.txt", node.Links.First().Name);
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Links.First().Id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void AddDirectory()
    {
        var temp = MakeTemp();
        try
        {
            var dir = TestFixture.IpfsContext.FileSystem.AddDirectoryAsync(temp, false).Result;
            Assert.IsTrue(dir.IsDirectory);

            var files = dir.Links.ToArray();
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("alpha.txt", files[0].Name);
            Assert.AreEqual("beta.txt", files[1].Name);

            Assert.AreEqual("alpha", TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(files[0].Id).Result);
            Assert.AreEqual("beta", TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(files[1].Id).Result);

            Assert.AreEqual("alpha", TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(dir.Id + "/alpha.txt").Result);
            Assert.AreEqual("beta", TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(dir.Id + "/beta.txt").Result);
        }
        finally
        {
            DeleteTemp(temp);
        }
    }

    [TestMethod]
    public void AddDirectoryRecursive()
    {
        var temp = MakeTemp();
        try
        {
            var dir = TestFixture.IpfsContext.FileSystem.AddDirectoryAsync(temp, true).Result;
            Assert.IsTrue(dir.IsDirectory);

            var files = dir.Links.ToArray();
            Assert.AreEqual(3, files.Length);
            Assert.AreEqual("alpha.txt", files[0].Name);
            Assert.AreEqual("beta.txt", files[1].Name);
            Assert.AreEqual("x", files[2].Name);
            Assert.AreNotEqual(0, files[0].Size);
            Assert.AreNotEqual(0, files[1].Size);

            var xfiles = new FileSystemNode(TestFixture.IpfsContext.FileSystem)
            {
                Id = files[2].Id,
            }.Links.ToArray();
            Assert.AreEqual(2, xfiles.Length);
            Assert.AreEqual("x.txt", xfiles[0].Name);
            Assert.AreEqual("y", xfiles[1].Name);

            var yfiles = new FileSystemNode(TestFixture.IpfsContext.FileSystem)
            {
                Id = xfiles[1].Id,
            }.Links.ToArray();
            Assert.AreEqual(1, yfiles.Length);
            Assert.AreEqual("y.txt", yfiles[0].Name);

            var y = new FileSystemNode(TestFixture.IpfsContext.FileSystem)
            {
                Id = yfiles[0].Id,
            };
            Assert.AreEqual("y", Encoding.UTF8.GetString(y.DataBytes));
            Assert.AreEqual("y", TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(dir.Id + "/x/y/y.txt").Result);
        }
        finally
        {
            DeleteTemp(temp);
        }
    }

    [TestMethod]
    public void AddFile()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "hello world");
        try
        {
            var result = TestFixture.IpfsContext.FileSystem.AddFileAsync(path).Result;
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);
            Assert.AreEqual(0, result.Links.Count());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task AddFile_WithProgress()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "hello world");
        try
        {
            var bytesTransferred = 0UL;
            var options = new AddFileOptions
            {
                Progress = new Progress<TransferProgress>(t => bytesTransferred += t.Bytes)
            };
            var result = await TestFixture.IpfsContext.FileSystem.AddFileAsync(path, options);
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);

            // Progress reports get posted on another synchronisation context.
            var stop = DateTime.Now.AddSeconds(3);
            while (DateTime.Now < stop)
            {
                if (bytesTransferred == 11UL)
                {
                    break;
                }

                await Task.Delay(10);
            }

            Assert.AreEqual(11UL, bytesTransferred);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void AddText()
    {
        var result = TestFixture.IpfsContext.FileSystem.AddTextAsync("hello world").Result;
        Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);
    }

    [TestMethod]
    public async Task GetTar_EmptyDirectory()
    {
        var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _ = Directory.CreateDirectory(temp);
        try
        {
            var dir = TestFixture.IpfsContext.FileSystem.AddDirectoryAsync(temp, true).Result;
            var dirid = dir.Id.Encode();

            using var tar = await TestFixture.IpfsContext.FileSystem.GetAsync(dir.Id);
            var buffer = new byte[3 * 512];
            var offset = 0;
            while (offset < buffer.Length)
            {
                var n = await tar.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset));
                Assert.IsTrue(n > 0);
                offset += n;
            }

            Assert.AreEqual(-1, tar.ReadByte());
        }
        finally
        {
            DeleteTemp(temp);
        }
    }

    [TestMethod]
    public void GetText()
    {
        var result = TestFixture.IpfsContext.FileSystem.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD").Result;
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void Read_With_Offset()
    {
        var indata = new MemoryStream(new byte[] { 10, 20, 30 });
        var node = TestFixture.IpfsContext.FileSystem.AddAsync(indata).Result;
        using var outdata = TestFixture.IpfsContext.FileSystem.ReadFileAsync(node.Id, offset: 1).Result;
        Assert.AreEqual(20, outdata.ReadByte());
        Assert.AreEqual(30, outdata.ReadByte());
        Assert.AreEqual(-1, outdata.ReadByte());
    }

    [TestMethod]
    public void Read_With_Offset_Length_1()
    {
        var indata = new MemoryStream(new byte[] { 10, 20, 30 });
        var node = TestFixture.IpfsContext.FileSystem.AddAsync(indata).Result;
        using var outdata = TestFixture.IpfsContext.FileSystem.ReadFileAsync(node.Id, offset: 1, count: 1).Result;
        Assert.AreEqual(20, outdata.ReadByte());
        Assert.AreEqual(-1, outdata.ReadByte());
    }

    [TestMethod]
    public void Read_With_Offset_Length_2()
    {
        var indata = new MemoryStream(new byte[] { 10, 20, 30 });
        var node = TestFixture.IpfsContext.FileSystem.AddAsync(indata).Result;
        using var outdata = TestFixture.IpfsContext.FileSystem.ReadFileAsync(node.Id, offset: 1, count: 2).Result;
        Assert.AreEqual(20, outdata.ReadByte());
        Assert.AreEqual(30, outdata.ReadByte());
        Assert.AreEqual(-1, outdata.ReadByte());
    }

    [TestMethod]
    public void ReadText()
    {
        var node = TestFixture.IpfsContext.FileSystem.AddTextAsync("hello world").Result;
        var text = TestFixture.IpfsContext.FileSystem.ReadAllTextAsync(node.Id).Result;
        Assert.AreEqual("hello world", text);
    }

    private static void DeleteTemp(string temp)
    {
        while (true)
        {
            try
            {
                Directory.Delete(temp, true);
                break;
            }
            catch (Exception)
            {
                Thread.Sleep(1);
                continue;  // most likely anti-virus is reading a file
            }
        }
    }

    private static string MakeTemp()
    {
        var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var x = Path.Combine(temp, "x");
        var xy = Path.Combine(x, "y");
        _ = Directory.CreateDirectory(temp);
        _ = Directory.CreateDirectory(x);
        _ = Directory.CreateDirectory(xy);

        File.WriteAllText(Path.Combine(temp, "alpha.txt"), "alpha");
        File.WriteAllText(Path.Combine(temp, "beta.txt"), "beta");
        File.WriteAllText(Path.Combine(x, "x.txt"), "x");
        File.WriteAllText(Path.Combine(xy, "y.txt"), "y");
        return temp;
    }
}
