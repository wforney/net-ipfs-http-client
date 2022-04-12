// <copyright file="DnsApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class DnsApi.
/// Implements the <see cref="Ipfs.CoreApi.IDnsApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IDnsApi" />
public class DnsApi : IDnsApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public DnsApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>(
            "dns",
            name,
            cancel,
            $"recursive={recursive.ToString().ToLowerInvariant()}");
        var path = json is null ? null : (string?)JObject.Parse(json)?["Path"];
        return path;
    }
}
