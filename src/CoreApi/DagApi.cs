// <copyright file="DagApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class DagApi.
/// Implements the <see cref="Ipfs.CoreApi.IDagApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IDagApi" />
public class DagApi : IDagApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="DagApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public DagApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<JObject?> GetAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("dag/get", id, cancel);
        return json is null ? null : JObject.Parse(json);
    }

    /// <inheritdoc />
    public async Task<JToken?> GetAsync(string path, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("dag/get", path, cancel);
        return json is null ? null : JToken.Parse(json);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(Cid id, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("dag/get", id, cancel);
        return json is null ? default : JsonConvert.DeserializeObject<T>(json);
    }

    /// <inheritdoc />
    public async Task<Cid?> PutAsync(
        JObject data,
        string contentType = "dag-cbor",
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = true,
        CancellationToken cancel = default)
    {
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
        using (var jw = new JsonTextWriter(sw))
        {
            var serializer = new JsonSerializer
            {
                Culture = CultureInfo.InvariantCulture
            };
            serializer.Serialize(jw, data);
        }

        ms.Position = 0;
        return await this.PutAsync(ms, contentType, multiHash, encoding, pin, cancel);
    }

    /// <inheritdoc />
    public async Task<Cid?> PutAsync(
        object data,
        string contentType = "dag-cbor",
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = true,
        CancellationToken cancel = default)
    {
        using var ms = new MemoryStream(
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)),
            false);
        return await this.PutAsync(ms, contentType, multiHash, encoding, pin, cancel);
    }

    /// <inheritdoc />
    public async Task<Cid?> PutAsync(
        Stream data,
        string contentType = "dag-cbor",
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = true,
        CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<Stream?, string?>(
            "dag/put",
            null,
            data,
            "unknown",
            cancel,
            $"format={contentType}",
            $"pin={pin.ToString().ToLowerInvariant()}",
            $"hash={multiHash}",
            $"cid-base={encoding}");
        if (json is null)
        {
            return null;
        }

        var result = JObject.Parse(json);
        return (Cid?)(string?)result?["Cid"]?["/"];
    }
}
