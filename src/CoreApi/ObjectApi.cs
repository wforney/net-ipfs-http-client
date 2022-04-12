// <copyright file="ObjectApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class ObjectApi.
/// Implements the <see cref="Ipfs.CoreApi.IObjectApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IObjectApi" />
public class ObjectApi : IObjectApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    /// <exception cref="System.ArgumentNullException">ipfs</exception>
    public ObjectApi(IIpfsClient ipfs) => this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));

    /// <inheritdoc />
    public Task<Stream?> DataAsync(Cid id, CancellationToken cancel = default) =>
        this.ipfs.ExecuteCommand<Stream?>("object/data", id, cancel);

    /// <inheritdoc />
    public async Task<DagNode?> GetAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("object/get", id, cancel);
        return json is null ? null : GetDagFromJson(json);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IMerkleLink>?> LinksAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("object/links", id, cancel);
        return json is null ? (IEnumerable<IMerkleLink>?)null : GetDagFromJson(json)?.Links;
    }

    /// <inheritdoc />
    public async Task<DagNode?> NewAsync(string? template = null, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("object/new", template, cancel);
        var hash = json is null ? null : (string?)JObject.Parse(json)?["Hash"];
        return await this.GetAsync(hash, cancel);
    }

    /// <inheritdoc />
    public Task<DagNode?> NewDirectoryAsync(CancellationToken cancel = default) => this.NewAsync("unixfs-dir", cancel);

    /// <inheritdoc />
    public Task<DagNode?> PutAsync(byte[] data, IEnumerable<IMerkleLink>? links = null, CancellationToken cancel = default) => this.PutAsync(new DagNode(data, links), cancel);

    /// <inheritdoc />
    public async Task<DagNode?> PutAsync(DagNode node, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<byte[], string?>("object/put", data: node.ToArray(), cancellationToken: cancel, options: "inputenc=protobuf");
        return json is null ? null : node;
    }

    // TOOD: patch sub API

    /// <inheritdoc />
    public async Task<ObjectStat?> StatAsync(Cid id, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("object/stat", id, cancel);
        if (json is null)
        {
            return null;
        }

        var r = JObject.Parse(json);

        return new ObjectStat
        {
            LinkCount = (int)(r?["NumLinks"] ?? 0),
            LinkSize = (long)(r?["LinksSize"] ?? 0),
            BlockSize = (long)(r?["BlockSize"] ?? 0),
            DataSize = (long)(r?["DataSize"] ?? 0),
            CumulativeSize = (long)(r?["CumulativeSize"] ?? 0)
        };
    }

    /// <summary>
    /// Gets the dag from json.
    /// </summary>
    /// <param name="json">The json.</param>
    /// <returns>System.Nullable&lt;DagNode&gt;.</returns>
    private static DagNode? GetDagFromJson(string json)
    {
        if (json is null)
        {
            return null;
        }

        var result = JObject.Parse(json);
        byte[]? data = null;
        var stringData = (string?)result?["Data"];
        if (stringData is not null)
        {
            data = Encoding.UTF8.GetBytes(stringData);
        }

        var links = result is null ? null : ((JArray?)result?["Links"])
            ?.Select(link => new DagLink(
                (string?)link?["Name"],
                (string?)link?["Hash"],
                (long)(link?["Size"] ?? 0)));
        return new DagNode(data, links);
    }
}
