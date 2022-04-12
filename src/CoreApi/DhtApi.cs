// <copyright file="DhtApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

/// <summary>
/// Class DhtApi.
/// Implements the <see cref="Ipfs.CoreApi.IDhtApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IDhtApi" />
public class DhtApi : IDhtApi
{
    private readonly IGenericApi genericApi;
    private readonly IIpfsClient ipfs;
    private readonly ILogger<IDhtApi> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DhtApi"/> class.
    /// </summary>
    /// <param name="genericApi">The generic API.</param>
    /// <param name="ipfs">The ipfs.</param>
    /// <param name="logger">The logger.</param>
    public DhtApi(IGenericApi genericApi, IIpfsClient ipfs, ILogger<IDhtApi> logger)
    {
        this.genericApi = genericApi;
        this.ipfs = ipfs;
        this.logger = logger;
    }

    /// <summary>
    /// Finds the peer asynchronous.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Task&lt;System.Nullable&lt;Peer&gt;&gt;.</returns>
    public Task<Peer?> FindPeerAsync(MultiHash id, CancellationToken cancel = default) => this.genericApi.IdAsync(id, cancel);

    /// <summary>
    /// Find providers as an asynchronous operation.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="limit">The limit.</param>
    /// <param name="providerFound">The provider found.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
    public async Task<IEnumerable<Peer>?> FindProvidersAsync(Cid id, int limit = 20, Action<Peer>? providerFound = null, CancellationToken cancel = default)
    {
        // TODO: providerFound action
        var stream = await this.ipfs.ExecuteCommand<Stream?>("dht/findprovs", id, cancel, $"num-providers={limit}");
        return stream is null ? (IEnumerable<Peer>?)null : this.ProviderFromStream(stream, limit).ToEnumerable();
    }

    /// <summary>
    /// Gets the asynchronous.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Task&lt;System.Byte[]&gt;.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default) => throw new NotImplementedException();

    /// <summary>
    /// Provides the asynchronous.
    /// </summary>
    /// <param name="cid">The cid.</param>
    /// <param name="advertise">if set to <c>true</c> [advertise].</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Task.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default) => throw new NotImplementedException();

    /// <summary>
    /// Puts the asynchronous.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Task.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default) => throw new NotImplementedException();

    /// <summary>
    /// Tries the get asynchronous.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Task&lt;System.Boolean&gt;.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public Task<bool> TryGetAsync(byte[] key, out byte[] value, CancellationToken cancel = default) => throw new NotImplementedException();

    private async IAsyncEnumerable<Peer> ProviderFromStream(Stream stream, int limit = int.MaxValue)
    {
        using var sr = new StreamReader(stream);
        var n = 0;
        while (!sr.EndOfStream && n < limit)
        {
            var json = await sr.ReadLineAsync();
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("Provider {json}", json);
            }

            var r = json is null ? null : JObject.Parse(json);
            var id = (string?)r?["ID"];
            if (id == string.Empty)
            {
                var responses = (JArray?)r?["Responses"];
                if (responses is null)
                {
                    continue;
                }

                foreach (var response in responses)
                {
                    var rid = (string?)response?["ID"];
                    if (rid != string.Empty)
                    {
                        ++n;
                        yield return new Peer { Id = new MultiHash(rid) };
                    }
                }
            }
            else
            {
                ++n;
                yield return new Peer { Id = new MultiHash(id) };
            }
        }
    }
}
