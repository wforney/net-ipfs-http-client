// <copyright file="SwarmApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class SwarmApi.
/// Implements the <see cref="Ipfs.CoreApi.ISwarmApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.ISwarmApi" />
public class SwarmApi : ISwarmApi
{
    private readonly IConfigApi configApi;
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwarmApi"/> class.
    /// </summary>
    /// <param name="configApi">The configuration API.</param>
    /// <param name="ipfs">The ipfs.</param>
    /// <exception cref="System.ArgumentNullException">configApi</exception>
    /// <exception cref="System.ArgumentNullException">ipfs</exception>
    public SwarmApi(IConfigApi configApi, IIpfsClient ipfs)
    {
        this.configApi = configApi ?? throw new ArgumentNullException(nameof(configApi));
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
    }

    /// <inheritdoc />
    public async Task<MultiAddress?> AddAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default)
    {
        // go-ipfs always does persist, https://github.com/ipfs/go-ipfs/issues/4605
        var json = await this.ipfs.ExecuteCommand<string?>("swarm/filters/add", address.ToString(), cancel);
        if (json is null)
        {
            return null;
        }

        var addrs = (JArray?)JObject.Parse(json)?["Strings"];
        var a = addrs?.FirstOrDefault();
        return a is null ? null : new MultiAddress((string?)a);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Peer?>?> AddressesAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("swarm/addrs", cancellationToken: cancel);
        return json is null
            ? Enumerable.Empty<Peer>()
            : (((JObject?)JObject.Parse(json)?["Addrs"])
                ?.Properties()
                ?.Select(
                    p =>
                        new Peer
                        {
                            Id = p.Name,
                            Addresses = ((JArray)p.Value)
                                .Select(a => MultiAddress.TryCreate((string?)a))
                                .Where(ma => ma is not null)
                        }));
    }

    /// <inheritdoc />
    public async Task ConnectAsync(MultiAddress address, CancellationToken cancel = default) => await this.ipfs.ExecuteCommand("swarm/connect", address.ToString(), cancel);

    /// <inheritdoc />
    public async Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default) => await this.ipfs.ExecuteCommand("swarm/disconnect", address.ToString(), cancel);

    /// <inheritdoc />
    public async Task<IEnumerable<MultiAddress>> ListAddressFiltersAsync(bool persist = false, CancellationToken cancel = default)
    {
        JArray? addrs;
        if (persist)
        {
            addrs = await this.configApi.GetAsync("Swarm.AddrFilters", cancel) as JArray;
        }
        else
        {
            var json = await this.ipfs.ExecuteCommand<string?>("swarm/filters", cancellationToken: cancel);
            addrs = json is null ? null : JObject.Parse(json)["Strings"] as JArray;
        }

        return addrs is null
            ? (IEnumerable<MultiAddress>)Array.Empty<MultiAddress>()
            : addrs
                .Select(a => MultiAddress.TryCreate((string?)a))
                .Where(ma => ma is not null);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Peer>> PeersAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("swarm/peers", null, cancel, "verbose=true");
        if (json is null)
        {
            return Enumerable.Empty<Peer>();
        }

        var result = JObject.Parse(json);

        // Older servers return an array of strings
        var strings = (JArray?)result?["Strings"];
        if (strings is not null)
        {
            return strings
               .Select(s =>
               {
                   var parts = ((string?)s)?.Split(' ');
                   var address = new MultiAddress(parts?[0]);
                   return new Peer
                   {
                       Id = address.PeerId,
                       ConnectedAddress = parts?[0],
                       Latency = Duration.Parse(parts?[1])
                   };
               });
        }

        // Current servers return JSON
        var peers = (JArray?)result?["Peers"];
        if (peers is not null)
        {
            return peers.Select(p => new Peer
            {
                Id = (string?)p?["Peer"],
                ConnectedAddress = new MultiAddress((string?)p?["Addr"] + "/ipfs/" + (string?)p?["Peer"]),
                Latency = Duration.Parse((string?)p?["Latency"])
            });
        }

        // Hmmm. Another change we can handle
        throw new FormatException("Unknown response from 'swarm/peers");
    }

    /// <inheritdoc />
    public async Task<MultiAddress?> RemoveAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default)
    {
        // go-ipfs always does persist, https://github.com/ipfs/go-ipfs/issues/4605
        var json = await this.ipfs.ExecuteCommand<string?>("swarm/filters/rm", address.ToString(), cancel);
        if (json is null)
        {
            return null;
        }

        var addrs = (JArray?)JObject.Parse(json)?["Strings"];
        var a = addrs?.FirstOrDefault();
        return a is null ? null : new MultiAddress((string?)a);
    }
}
