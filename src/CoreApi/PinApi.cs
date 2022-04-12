// <copyright file="PinApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class PinApi.
/// Implements the <see cref="Ipfs.CoreApi.IPinApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IPinApi" />
public class PinApi : IPinApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public PinApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<IEnumerable<Cid?>?> AddAsync(string path, bool recursive = true, CancellationToken cancel = default)
    {
        var opts = $"recursive={recursive.ToString().ToLowerInvariant()}";
        var json = await this.ipfs.ExecuteCommand<string?>("pin/add", path, cancel, opts);
        return json is null
            ? (IEnumerable<Cid?>?)null
            : (((JArray?)JObject.Parse(json)?["Pins"])?.Select(p => (Cid?)(string?)p));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Cid?>?> ListAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("pin/ls", null, cancel);
        if (json is null)
        {
            return null;
        }

        var keys = (JObject?)JObject.Parse(json)?["Keys"];
        return keys
            ?.Properties()
            ?.Select(p => (Cid)p.Name);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Cid?>?> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default)
    {
        var opts = $"recursive={recursive.ToString().ToLowerInvariant()}";
        var json = await this.ipfs.ExecuteCommand<string?>("pin/rm", id, cancel, opts);
        return json is null
            ? (IEnumerable<Cid?>?)null
            : (((JArray?)JObject.Parse(json)?["Pins"])?.Select(p => (Cid?)(string?)p));
    }
}
