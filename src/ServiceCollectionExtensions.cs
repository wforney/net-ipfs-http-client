namespace Ipfs.Http.Client;

using Ipfs.CoreApi;
using Ipfs.Http.Client.CoreApi;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System.Net;
using System.Reflection;

/// <summary>
/// The service collection extensions class.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a custom configured HTTP client for IPFS.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="ipfsApiUrl">The IPFS API URL.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// The default URL to the IPFS HTTP API server is <c>http://localhost:5001</c>. The environment variable "IpfsApiUrl" can be used to override this default.
    /// </remarks>
    public static IServiceCollection AddIpfsClient(this IServiceCollection services, Uri? ipfsApiUrl = null)
    {
        ipfsApiUrl ??= new Uri(Environment.GetEnvironmentVariable("IpfsApiUrl") ?? "http://localhost:5001");

        // The user agent value is "net-ipfs/M.N", where M is the major and N is minor version numbers of the assembly.
        var version = typeof(IpfsClient).GetTypeInfo().Assembly.GetName().Version;
        var userAgent = $"net-ipfs/{version?.Major ?? 0}.{version?.Minor ?? 0}";

        _ = services
            .AddHttpClient(
                IpfsClient.IpfsHttpClientName,
                client =>
                {
                    client.BaseAddress = ipfsApiUrl;
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .ConfigurePrimaryHttpMessageHandler(
                () =>
                {
                    var handler = new HttpClientHandler();
                    if (handler.SupportsAutomaticDecompression)
                    {
                        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    }

                    return handler;
                })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy());

        _ = services
            .AddLogging();

        _ = services
            .AddScoped<IBitswapApi, BitswapApi>()
            .AddScoped<IBlockApi, BlockApi>()
            .AddScoped<IBlockRepositoryApi, BlockRepositoryApi>()
            .AddScoped<IBootstrapApi, BootstrapApi>()
            .AddScoped<IConfigApi, ConfigApi>()
            .AddScoped<IDagApi, DagApi>()
            .AddScoped<IDhtApi, DhtApi>()
            .AddScoped<IDnsApi, DnsApi>()
            .AddScoped<IFileSystemApi, FileSystemApi>()
            .AddScoped<IGenericApi, GenericApi>()
            .AddScoped<IIpfsClient, IpfsClient>()
            .AddScoped<IKeyApi, KeyApi>()
            .AddScoped<INameApi, NameApi>()
            .AddScoped<IObjectApi, ObjectApi>()
            .AddScoped<IPinApi, PinApi>()
            .AddScoped<IPubSubApi, PubSubApi>()
            .AddScoped<IStatsApi, StatsApi>()
            .AddScoped<ISwarmApi, SwarmApi>()
            .AddScoped<IpfsContext>();

        return services;
    }

    /// <summary>
    /// Gets the retry policy.
    /// </summary>
    /// <returns>IAsyncPolicy&lt;HttpResponseMessage&gt;.</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
            .WaitAndRetryAsync(delay);
    }
}
