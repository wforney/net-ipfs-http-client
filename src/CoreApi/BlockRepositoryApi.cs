// <copyright file="BlockRepositoryApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class BlockRepositoryApi.
/// Implements the <see cref="Ipfs.CoreApi.IBlockRepositoryApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IBlockRepositoryApi" />
public class BlockRepositoryApi : IBlockRepositoryApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockRepositoryApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public BlockRepositoryApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task RemoveGarbageAsync(CancellationToken cancel = default) => await this.ipfs.ExecuteCommand("repo/gc", cancellationToken: cancel);

    /// <inheritdoc />
    public async Task<RepositoryData?> StatisticsAsync(CancellationToken cancel = default) => await this.ipfs.ExecuteCommand<RepositoryData?>("repo/stat", cancellationToken: cancel);

    /// <inheritdoc />
    public async Task VerifyAsync(CancellationToken cancel = default) => await this.ipfs.ExecuteCommand("repo/verify", cancellationToken: cancel);

    /// <inheritdoc />
    public async Task<string?> VersionAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("repo/version", cancellationToken: cancel);
        if (json is null)
        {
            return null;
        }

        var info = JObject.Parse(json);
        return (string?)info?["Version"];
    }
}
