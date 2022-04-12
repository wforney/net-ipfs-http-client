// <copyright file="BlockApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;

/// <summary>
/// Class BlockApi.
/// Implements the <see cref="Ipfs.CoreApi.IBlockApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IBlockApi" />
public class BlockApi : IBlockApi
{
    private readonly IIpfsClient ipfs;
    private readonly IPinApi pinApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    /// <param name="pinApi">The pin API.</param>
    /// <exception cref="System.ArgumentNullException">ipfs</exception>
    /// <exception cref="System.ArgumentNullException">pinApi</exception>
    public BlockApi(IIpfsClient ipfs, IPinApi pinApi)
    {
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
        this.pinApi = pinApi ?? throw new ArgumentNullException(nameof(pinApi));
    }

    /// <inheritdoc />
    public async Task<IDataBlock?> GetAsync(Cid id, CancellationToken cancel = default)
    {
        var data = await this.ipfs.ExecuteCommand<byte[]>("block/get", id, cancel);
        return data is null
            ? null
            : (IDataBlock)new Block
            {
                DataBytes = data,
                Id = id
            };
    }

    /// <inheritdoc />
    public async Task<Cid?> PutAsync(
        byte[] data,
        string contentType = Cid.DefaultContentType,
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = false,
        CancellationToken cancel = default)
    {
        var options = new List<string>();
        if (multiHash != MultiHash.DefaultAlgorithmName ||
            contentType != Cid.DefaultContentType ||
            encoding != MultiBase.DefaultAlgorithmName)
        {
            options.Add($"mhtype={multiHash}");
            options.Add($"format={contentType}");
            options.Add($"cid-base={encoding}");
        }

        var json = await this.ipfs.ExecuteCommand<byte[], string?>("block/put", null, data, IpfsClient.IpfsHttpClientName, cancel, options.ToArray());
        if (json is null)
        {
            return null;
        }

        var info = JObject.Parse(json);
        Cid cid = (string?)info["Key"];

        if (pin)
        {
            _ = await this.pinApi.AddAsync(cid, recursive: false, cancel: cancel);
        }

        return cid;
    }

    /// <inheritdoc />
    public async Task<Cid?> PutAsync(
        Stream data,
        string contentType = Cid.DefaultContentType,
        string multiHash = MultiHash.DefaultAlgorithmName,
        string encoding = MultiBase.DefaultAlgorithmName,
        bool pin = false,
        CancellationToken cancel = default)
    {
        var options = new List<string>();
        if (multiHash != MultiHash.DefaultAlgorithmName ||
            contentType != Cid.DefaultContentType ||
            encoding != MultiBase.DefaultAlgorithmName)
        {
            options.Add($"mhtype={multiHash}");
            options.Add($"format={contentType}");
            options.Add($"cid-base={encoding}");
        }

        var json = await this.ipfs.ExecuteCommand<Stream?, string?>("block/put", data: data, cancellationToken: cancel, options: options.ToArray());
        if (json is null)
        {
            return null;
        }

        var info = JObject.Parse(json);
        Cid? cid = (string?)info?["Key"];

        if (pin)
        {
            _ = await this.pinApi.AddAsync(cid, recursive: false, cancel: cancel);
        }

        return cid;
    }

    /// <inheritdoc />
    public async Task<Cid?> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("block/rm", id, cancel, "force=" + ignoreNonexistent.ToString().ToLowerInvariant());
        if (json is null || json.Length == 0)
        {
            return null;
        }

        var result = JObject.Parse(json);
        var error = (string?)result["Error"];
        return error is null ? (Cid?)(string?)result["Hash"] : throw new HttpRequestException(error);
    }

    /// <inheritdoc />
    public async Task<IDataBlock?> StatAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("block/stat", id, cancel);
        if (json is null)
        {
            return null;
        }

        var info = JObject.Parse(json);
        return new Block
        {
            Size = (long?)info?["Size"] ?? 0,
            Id = (string?)info?["Key"]
        };
    }
}
