// <copyright file="NameApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class NameApi.
/// Implements the <see cref="Ipfs.CoreApi.INameApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.INameApi" />
public class NameApi : INameApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public NameApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<NamedContent?> PublishAsync(string path, bool resolve = true, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>(
            "name/publish",
            path,
            cancel,
            "lifetime=24h", // TODO
            $"resolve={resolve.ToString().ToLowerInvariant()}",
            $"key={key}");
        if (json is null)
        {
            return null;
        }

        // TODO: lifetime
        var info = JObject.Parse(json);
        return new NamedContent
        {
            NamePath = (string?)info?["Name"],
            ContentPath = (string?)info?["Value"]
        };
    }

    /// <inheritdoc />
    public Task<NamedContent?> PublishAsync(Cid id, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default) =>
        this.PublishAsync($"/ipfs/{id.Encode()}", false, key, lifetime, cancel);

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string name, bool recursive = false, bool nocache = false, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>(
            "name/resolve",
            name,
            cancel,
            $"recursive={recursive.ToString().ToLowerInvariant()}",
            $"nocache={nocache.ToString().ToLowerInvariant()}");
        if (json is null)
        {
            return null;
        }

        var path = (string?)JObject.Parse(json)?["Path"];
        return path;
    }
}
