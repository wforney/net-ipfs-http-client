// <copyright file="PubSubApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Microsoft.Extensions.Logging;
using Multiformats.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class PubSubApi.
/// Implements the <see cref="Ipfs.CoreApi.IPubSubApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IPubSubApi" />
public class PubSubApi : IPubSubApi
{
    private readonly IIpfsClient ipfs;
    private readonly ILogger<PubSubApi> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="System.ArgumentNullException">ipfs</exception>
    /// <exception cref="System.ArgumentNullException">logger</exception>
    public PubSubApi(IIpfsClient ipfs, ILogger<PubSubApi> logger)
    {
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Peer?>?> PeersAsync(string? topic = null, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("pubsub/peers", topic, cancel);
        if (json is null)
        {
            return null;
        }

        var result = JObject.Parse(json);
        return result["Strings"] is JArray strings ? strings.Select(s => new Peer { Id = (string?)s }) : (IEnumerable<Peer>)Array.Empty<Peer>();
    }

    /// <inheritdoc />
    public async Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default)
    {
        var url = new StringBuilder()
            .Append("/api/v0/pubsub/pub")
            .Append("?arg=u")
            .Append(Multibase.Encode(MultibaseEncoding.Base64Url, Encoding.UTF8.GetBytes(topic)));

        _ = await this.ipfs.ExecuteCommand<byte[], byte[]>(url.ToString(), data: message, cancellationToken: cancel);
        //url.Append("?arg=");
        //url.Append(System.Net.WebUtility.UrlEncode(topic));
        //url.Append("&arg=");
        //var data = Encoding.ASCII.GetString(System.Net.WebUtility.UrlEncodeToBytes(message, 0, message.Length));
        //url.Append(data);
        //return Client.ExecuteCommand(new Uri(Client.ApiUri, url.ToString()), cancel);
    }

    /// <inheritdoc />
    public Task PublishAsync(string topic, Stream message, CancellationToken cancel = default)
    {
        var url = new StringBuilder()
            .Append("/api/v0/pubsub/pub")
            .Append("?arg=u")
            .Append(Multibase.Encode(MultibaseEncoding.Base64Url, Encoding.UTF8.GetBytes(topic)));

        return this.ipfs.ExecuteCommand<Stream, Stream>(url.ToString(), data: message, cancellationToken: cancel);
    }

    /// <inheritdoc />
    public Task PublishAsync(string topic, string message, CancellationToken cancel = default)
    {
        var url = new StringBuilder()
            .Append("/api/v0/pubsub/pub")
            .Append("?arg=u")
            .Append(Multibase.Encode(MultibaseEncoding.Base64Url, Encoding.UTF8.GetBytes(topic)));

        return this.ipfs.ExecuteCommand(url.ToString(), message, cancel);
        //await Client.DoCommandAsync("pubsub/pub", cancel, topic, "arg=" + message);
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
    {
        var messageStream = await this.ipfs.ExecuteCommand<Stream>(
            "pubsub/sub",
            $"u{Multibase.Encode(MultibaseEncoding.Base64Url, Encoding.UTF8.GetBytes(topic))}",
            cancellationToken);
        if (messageStream is null)
        {
            return;
        }

        var sr = new StreamReader(messageStream);

        _ = Task.Run(() => this.ProcessMessages(topic, handler, sr, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string?>?> SubscribedTopicsAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("pubsub/ls", null, cancel);
        if (json is null)
        {
            return null;
        }

        var result = JObject.Parse(json);
        return result["Strings"] is not JArray strings ? (IEnumerable<string?>)Array.Empty<string?>() : strings.Select(s => (string?)s);
    }

    private async Task ProcessMessages(string topic, Action<PublishedMessage> handler, StreamReader sr, CancellationToken ct)
    {
        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("Start listening for '{topic}' messages", topic);
        }

        // .Net needs a ReadLine(CancellationToken)
        // As a work-around, we register a function to close the stream
        _ = ct.Register(() => sr.Dispose());
        try
        {
            while (!sr.EndOfStream && !ct.IsCancellationRequested)
            {
                var json = await sr.ReadLineAsync();
                if (json is null)
                {
                    break;
                }

                if (this.logger.IsEnabled(LogLevel.Debug))
                {
                    this.logger.LogDebug("PubSub message {json}", json);
                }

                // go-ipfs 0.4.13 and earlier always send empty JSON
                // as the first response.
                if (json == "{}")
                {
                    continue;
                }

                if (!ct.IsCancellationRequested)
                {
                    handler(new PublishedMessage(json));
                }
            }
        }
        catch (Exception e)
        {
            // Do not report errors when cancelled.
            if (!ct.IsCancellationRequested)
            {
                this.logger.LogError(e, "Error while processing pubsub messages");
            }
        }
        finally
        {
            sr.Dispose();
        }

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("Stop listening for '{topic}' messages", topic);
        }
    }
}
