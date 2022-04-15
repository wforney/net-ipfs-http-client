namespace Ipfs.Http.Client;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
public partial class IpfsClient : IIpfsClient
{
    /// <summary>
    /// The IPFS HTTP client name
    /// </summary>
    public const string IpfsHttpClientName = "ipfs";

    private const string UnknownFilename = "unknown";
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<IpfsClient> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpfsClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public IpfsClient(IHttpClientFactory httpClientFactory, ILogger<IpfsClient> logger)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.logger = logger;

        this.TrustedPeers = new TrustedPeerCollection(this);
    }

    /// <inheritdoc />
    public TrustedPeerCollection TrustedPeers { get; }

    /// <inheritdoc />
    public async Task ExecuteCommand(string command, string? arg = null, CancellationToken cancellationToken = default, params string[] options)
    {
        var url = BuildCommandUrl(command, arg, options);
        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("POST {url}", url.ToString());
        }

        var client = this.httpClientFactory.CreateClient(IpfsClient.IpfsHttpClientName);
        var response = await client.PostAsync(url, null, cancellationToken);
        await this.ThrowOnErrorAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteCommand<T>(string command, string? arg = null, CancellationToken cancellationToken = default, params string[] options)
    {
        var url = BuildCommandUrl(command, arg, options);
        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("GET {url}", url.ToString());
        }

        var client = this.httpClientFactory.CreateClient(IpfsClient.IpfsHttpClientName);
        var response = await client.PostAsync(url, null, cancellationToken);
        return await this.HandleResponse<T>(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TR?> ExecuteCommand<T, TR>(string command, string? arg = null, T? data = default, string name = UnknownFilename, CancellationToken cancellationToken = default, params string[] options)
    {
        var content = GetMultipartFormDataContent(data, name);

        var url = BuildCommandUrl(command, arg, options);

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("POST {url}", url.ToString());
        }

        var client = this.httpClientFactory.CreateClient(IpfsClient.IpfsHttpClientName);
        var response = await client.PostAsync(url, content, cancellationToken);
        return await this.HandleResponse<TR>(response, cancellationToken);
    }

    /// <summary>
    /// Builds the relative command URL.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="arg">The argument.</param>
    /// <param name="options">The options.</param>
    /// <returns>The relative command URL.</returns>
    private static Uri BuildCommandUrl(string command, string? arg = null, params string[] options)
    {
        var q = new StringBuilder();
        if (arg is not null)
        {
            _ = q.Append("&arg=");
            _ = q.Append(WebUtility.UrlEncode(arg));
        }

        foreach (var option in options)
        {
            _ = q.Append('&');
            var i = option.IndexOf('=');
            _ = i < 0
                ? q.Append(option)
                : q.Append(option.AsSpan(0, i))
                    .Append('=')
                    .Append(WebUtility.UrlEncode(option[(i + 1)..]));
        }

        var url = $"/api/v0/{command}";
        if (q.Length > 0)
        {
            q[0] = '?';
            _ = q.Insert(0, url);
            url = q.ToString();
        }

        return new Uri(url, UriKind.Relative);
    }

    /// <summary>
    /// Gets the content of the multipart form data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="name">The name.</param>
    /// <returns>The content of the multipart form data.</returns>
    private static MultipartFormDataContent? GetMultipartFormDataContent<T>(T? data, string name = UnknownFilename)
    {
        HttpContent? httpContent = data switch
        {
            byte[] b => new ByteArrayContent(b),
            string s => new StringContent(s),
            Stream s => new StreamContent(s),
            null => null,
            _ => throw new NotSupportedException()
        };

        if (httpContent is null)
        {
            return null;
        }

        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return string.IsNullOrWhiteSpace(name)
            ? new MultipartFormDataContent { { httpContent, "file", UnknownFilename } }
            : new MultipartFormDataContent { { httpContent, "file", name } };
    }

    /// <summary>
    /// Handles the response.
    /// </summary>
    /// <typeparam name="T">The desired return type.</typeparam>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The handled response.</returns>
    private async Task<T?> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        await this.ThrowOnErrorAsync(response, cancellationToken);

        async Task<byte[]> HandleByteArrayResponse() => await response.Content.ReadAsByteArrayAsync(cancellationToken);

        async Task<Stream> HandleStreamResponse() => await response.Content.ReadAsStreamAsync(cancellationToken);

        async Task<string> HandleStringResponse()
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("RSP {text}", text);
            }

            return text;
        }

        async Task<T?> HandleOtherTypeResponse()
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("RSP {json}", json);
            }

            return System.Text.Json.JsonSerializer.Deserialize<T>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return typeof(T) switch
        {
            Type t when t == typeof(byte[]) => (T)(object)await HandleByteArrayResponse(),
            Type t when t == typeof(Stream) => (T)(object)await HandleStreamResponse(),
            Type t when t == typeof(string) => (T)(object)await HandleStringResponse(),
            _ => await HandleOtherTypeResponse()
        };
    }

    /// <summary>
    /// Throws an <see cref="HttpRequestException"/> if the response does not indicate success.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// The API server returns an JSON error in the form <c>{ "Message": "...", "Code": ... }</c>.
    /// </remarks>
    private async Task ThrowOnErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var error = $"Invalid IPFS command: {response.RequestMessage?.RequestUri?.ToString()}";
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("ERR {error}", error);
            }

            throw new HttpRequestException(error);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("ERR {body}", body);
        }

        var message = body;
        try
        {
            var res = JsonConvert.DeserializeObject<dynamic>(body);
            message = (string?)res?.Message;
        }
        catch
        {
            // ignored
        }

        throw new HttpRequestException(message);
    }
}
