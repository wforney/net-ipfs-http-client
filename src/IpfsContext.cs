// <copyright file="IpfsContext.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>

namespace Ipfs.Http.Client;

using Ipfs.CoreApi;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Class IpfsContext.
/// </summary>
public class IpfsContext : ICoreApi
{
    /// <summary>
    /// The service provider
    /// </summary>
    private static IServiceProvider? ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpfsContext"/> class.
    /// </summary>
    /// <param name="bitswapApi">The bitswap API.</param>
    /// <param name="blockApi">The block API.</param>
    /// <param name="blockRepositoryApi">The block repository API.</param>
    /// <param name="bootstrapApi">The bootstrap API.</param>
    /// <param name="configApi">The configuration API.</param>
    /// <param name="dagApi">The dag API.</param>
    /// <param name="dhtApi">The DHT API.</param>
    /// <param name="dnsApi">The DNS API.</param>
    /// <param name="fileSystemApi">The file system API.</param>
    /// <param name="genericApi"></param>
    /// <param name="keyApi">The key API.</param>
    /// <param name="nameApi">The name API.</param>
    /// <param name="objectApi">The object API.</param>
    /// <param name="pinApi">The pin API.</param>
    /// <param name="pubSubApi">The pub sub API.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="statApi">The stat API.</param>
    /// <param name="swarmApi">The swarm API.</param>
    public IpfsContext(
        IBitswapApi bitswapApi,
        IBlockApi blockApi,
        IBlockRepositoryApi blockRepositoryApi,
        IBootstrapApi bootstrapApi,
        IConfigApi configApi,
        IDagApi dagApi,
        IDhtApi dhtApi,
        IDnsApi dnsApi,
        IFileSystemApi fileSystemApi,
        IGenericApi genericApi,
        IKeyApi keyApi,
        INameApi nameApi,
        IObjectApi objectApi,
        IPinApi pinApi,
        IPubSubApi pubSubApi,
        IServiceProvider serviceProvider,
        IStatsApi statApi,
        ISwarmApi swarmApi)
    {
        this.Bitswap = bitswapApi ?? throw new ArgumentNullException(nameof(bitswapApi));
        this.Block = blockApi ?? throw new ArgumentNullException(nameof(blockApi));
        this.BlockRepository = blockRepositoryApi ?? throw new ArgumentNullException(nameof(blockRepositoryApi));
        this.Bootstrap = bootstrapApi ?? throw new ArgumentNullException(nameof(bootstrapApi));
        this.Config = configApi ?? throw new ArgumentNullException(nameof(configApi));
        this.Dag = dagApi ?? throw new ArgumentNullException(nameof(dagApi));
        this.Dht = dhtApi ?? throw new ArgumentNullException(nameof(dhtApi));
        this.Dns = dnsApi ?? throw new ArgumentNullException(nameof(dnsApi));
        this.FileSystem = fileSystemApi ?? throw new ArgumentNullException(nameof(fileSystemApi));
        this.Generic = genericApi ?? throw new ArgumentNullException(nameof(genericApi));
        this.Key = keyApi ?? throw new ArgumentNullException(nameof(keyApi));
        this.Name = nameApi ?? throw new ArgumentNullException(nameof(nameApi));
        this.Object = objectApi ?? throw new ArgumentNullException(nameof(objectApi));
        this.Pin = pinApi ?? throw new ArgumentNullException(nameof(pinApi));
        this.PubSub = pubSubApi ?? throw new ArgumentNullException(nameof(pubSubApi));
        ServiceProvider = serviceProvider;
        this.Stats = statApi ?? throw new ArgumentNullException(nameof(statApi));
        this.Swarm = swarmApi ?? throw new ArgumentNullException(nameof(swarmApi));
    }

    /// <inheritdoc />
    public IBitswapApi Bitswap { get; }

    /// <inheritdoc />
    public IBlockApi Block { get; }

    /// <inheritdoc />
    public IBlockRepositoryApi BlockRepository { get; }

    /// <inheritdoc />
    public IBootstrapApi Bootstrap { get; }

    /// <inheritdoc />
    public IConfigApi Config { get; }

    /// <inheritdoc />
    public IDagApi Dag { get; }

    /// <inheritdoc />
    public IDhtApi Dht { get; }

    /// <inheritdoc />
    public IDnsApi Dns { get; }

    /// <inheritdoc />
    public IFileSystemApi FileSystem { get; }

    /// <inheritdoc />
    public IGenericApi Generic { get; }

    /// <inheritdoc />
    public IKeyApi Key { get; }

    /// <inheritdoc />
    public INameApi Name { get; }

    /// <inheritdoc />
    public IObjectApi Object { get; }

    /// <inheritdoc />
    public IPinApi Pin { get; }

    /// <inheritdoc />
    public IPubSubApi PubSub { get; }

    /// <inheritdoc />
    public IStatsApi Stats { get; }

    /// <inheritdoc />
    public ISwarmApi Swarm { get; }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    /// <returns>IServiceProvider.</returns>
    public static IServiceProvider GetServiceProvider() => ServiceProvider ??= new ServiceCollection().AddIpfsClient().BuildServiceProvider();
}
