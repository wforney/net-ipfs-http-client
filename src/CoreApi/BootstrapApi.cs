// <copyright file="BootstrapApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;

/// <summary>
/// Class BootstrapApi.
/// Implements the <see cref="Ipfs.CoreApi.IBootstrapApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IBootstrapApi" />
public class BootstrapApi : IBootstrapApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public BootstrapApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<MultiAddress?> AddAsync(MultiAddress address, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("bootstrap/add", cancellationToken: cancel, arg: address.ToString());
        if (json is null)
        {
            return null;
        }

        var addrs = (JArray?)JObject.Parse(json)?["Peers"];
        var a = addrs?.FirstOrDefault();
        return a is null ? null : new MultiAddress((string?)a);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MultiAddress>?> AddDefaultsAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("bootstrap/add/default", cancellationToken: cancel);
        if (json is null)
        {
            return null;
        }

        var addrs = (JArray?)JObject.Parse(json)?["Peers"];
        return addrs
            ?.Select(a => MultiAddress.TryCreate((string?)a))
            .Where(ma => ma is not null);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MultiAddress>?> ListAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("bootstrap/list", cancellationToken: cancel);
        if (json is null)
        {
            return null;
        }

        var addrs = (JArray?)JObject.Parse(json)?["Peers"];
        return addrs
            ?.Select(a => MultiAddress.TryCreate((string?)a))
            .Where(ma => ma is not null);
    }

    /// <inheritdoc />
    public Task RemoveAllAsync(CancellationToken cancel = default) => this.ipfs.ExecuteCommand("bootstrap/rm/all", cancellationToken: cancel);

    /// <inheritdoc />
    public async Task<MultiAddress?> RemoveAsync(MultiAddress address, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("bootstrap/rm", cancellationToken: cancel, arg: address.ToString());
        if (json is null)
        {
            return null;
        }

        var addrs = (JArray?)JObject.Parse(json)?["Peers"];
        var a = addrs?.FirstOrDefault();
        return a is null ? null : new MultiAddress((string?)a);
    }
}
