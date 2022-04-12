// <copyright file="StatsApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class StatsApi.
/// Implements the <see cref="Ipfs.CoreApi.IStatsApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IStatsApi" />
public class StatsApi : IStatsApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatsApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public StatsApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<BandwidthData?> BandwidthAsync(CancellationToken cancel = default) => await this.ipfs.ExecuteCommand<BandwidthData?>("stats/bw", cancellationToken: cancel);

    /// <inheritdoc />
    public async Task<BitswapData?> BitswapAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("stats/bitswap", cancellationToken: cancel);
        if (json is null)
        {
            return null;
        }

        var stat = JObject.Parse(json);
        return new BitswapData
        {
            BlocksReceived = (ulong)(stat?["BlocksReceived"] ?? 0),
            DataReceived = (ulong)(stat?["DataReceived"] ?? 0),
            BlocksSent = (ulong)(stat?["BlocksSent"] ?? 0),
            DataSent = (ulong)(stat?["DataSent"] ?? 0),
            DupBlksReceived = (ulong)(stat?["DupBlksReceived"] ?? 0),
            DupDataReceived = (ulong)(stat?["DupDataReceived"] ?? 0),
            ProvideBufLen = (int)(stat?["ProvideBufLen"] ?? 0),
            Peers = ((JArray?)stat?["Peers"])?.Select(s => new MultiHash((string?)s)),
            Wantlist = ((JArray?)stat?["Wantlist"])?.Select(o => Cid.Decode(o["/"]?.ToString()))
        };
    }

    /// <inheritdoc />
    public async Task<RepositoryData?> RepositoryAsync(CancellationToken cancel = default) => await this.ipfs.ExecuteCommand<RepositoryData?>("stats/repo", cancellationToken: cancel);
}
