namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A client that allows access to the InterPlanetary File System (IPFS).
/// </summary>
/// <remarks>
/// The API is based on the <see href="https://ipfs.io/docs/commands/">IPFS commands</see>.
/// </remarks>
/// <seealso href="https://ipfs.io/docs/api/">IPFS API</seealso>
/// <seealso href="https://ipfs.io/docs/commands/">IPFS commands</seealso>
/// <remarks>
/// <b>IpfsClient</b> is thread safe, only one instance is required by the application.
/// </remarks>
public class GenericApi : IGenericApi
{
    private const double TicksPerNanosecond = TimeSpan.TicksPerMillisecond * 0.000001;

    private readonly IIpfsClient ipfsClient;
    private readonly ILogger<GenericApi> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericApi"/> class.
    /// </summary>
    /// <param name="ipfsClient">The IPFS client.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">logger</exception>
    public GenericApi(IIpfsClient ipfsClient, ILogger<GenericApi> logger)
    {
        this.ipfsClient = ipfsClient ?? throw new ArgumentNullException(nameof(ipfsClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Peer?> IdAsync(MultiHash? peer = null, CancellationToken cancel = default) =>
        await this.ipfsClient.ExecuteCommand<Peer>("id", null, cancel, peer?.ToString() ?? string.Empty);

    /// <inheritdoc />
    public async Task<IEnumerable<PingResult>> PingAsync(MultiHash peer, int count = 10, CancellationToken cancel = default) =>
        this.StreamPingResult(
            await this.ipfsClient.ExecuteCommand<Stream>(
                "ping",
                peer.ToString(),
                cancel,
                $"count={count.ToString(CultureInfo.InvariantCulture)}"))
            .ToEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<PingResult>> PingAsync(MultiAddress address, int count = 10, CancellationToken cancel = default) =>
        this.StreamPingResult(
            await this.ipfsClient.ExecuteCommand<Stream>(
                "ping",
                address.ToString(),
                cancel,
                $"count={count.ToString(CultureInfo.InvariantCulture)}"))
            .ToEnumerable();

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string name, bool recursive = true, CancellationToken cancel = default)
    {
        var response = await this.ipfsClient.ExecuteCommand<string>(
            "resolve",
            name,
            cancel,
            $"recursive={recursive.ToString().ToLowerInvariant()}");
        return response is null ? null : (string?)(JObject.Parse(response)?["Path"]);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync() =>
        await this.ipfsClient.ExecuteCommand("shutdown");

    /// <inheritdoc />
    public async Task<Dictionary<string, string?>?> VersionAsync(CancellationToken cancel = default) =>
        await this.ipfsClient.ExecuteCommand<Dictionary<string, string?>?>("version", null, cancel);

    /// <summary>
    /// Streams the ping result.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>The ping result.</returns>
    private async IAsyncEnumerable<PingResult> StreamPingResult(Stream? stream)
    {
        if (stream is null)
        {
            yield break;
        }

        using var sr = new StreamReader(stream);
        while (!sr.EndOfStream)
        {
            var json = await sr.ReadLineAsync();
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("RSP {json}", json);
            }

            var r = string.IsNullOrWhiteSpace(json) ? null : JObject.Parse(json);
            if (r is null)
            {
                continue;
            }

            yield return new PingResult
            {
                Success = (bool?)r["Success"] ?? false,
                Text = (string?)r["Text"] ?? string.Empty,
                Time = TimeSpan.FromTicks((long)(((long?)r["Time"] ?? 0L) * TicksPerNanosecond))
            };
        }
    }
}
