// <copyright file="ConfigApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Class ConfigApi.
/// Implements the <see cref="Ipfs.CoreApi.IConfigApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IConfigApi" />
public class ConfigApi : IConfigApi
{
    /// <summary>
    /// The ipfs
    /// </summary>
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public ConfigApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <summary>
    /// Get as an asynchronous operation.
    /// </summary>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A Task&lt;Newtonsoft.Json.Linq.JObject?&gt; representing the asynchronous operation.</returns>
    public async Task<JObject?> GetAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("config/show", cancellationToken: cancel);
        return json is null ? null : JObject.Parse(json);
    }

    /// <summary>
    /// Get as an asynchronous operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A Task&lt;Newtonsoft.Json.Linq.JToken?&gt; representing the asynchronous operation.</returns>
    public async Task<JToken?> GetAsync(string key, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("config", cancellationToken: cancel, arg: key);
        if (json is null)
        {
            return null;
        }

        var r = JObject.Parse(json);
        return r?["Value"];
    }

    /// <summary>
    /// Replace as an asynchronous operation.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
    public async Task ReplaceAsync(JObject config)
    {
        var data = Encoding.UTF8.GetBytes(config.ToString(Formatting.None));
        if (data is null)
        {
            return;
        }

        _ = await this.ipfs.ExecuteCommand<byte[]?, byte[]?>("config/replace", cancellationToken: CancellationToken.None, data: data);
    }

    /// <summary>
    /// Set as an asynchronous operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
    public async Task SetAsync(string key, string value, CancellationToken cancel = default) =>
        await this.ipfs.ExecuteCommand<string?>(
            "config",
            cancellationToken: cancel,
            arg: key,
            options: "arg=" + value);

    /// <summary>
    /// Set as an asynchronous operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
    public async Task SetAsync(string key, JToken value, CancellationToken cancel = default) =>
        await this.ipfs.ExecuteCommand<string?>(
            "config",
            key,
            cancel,
            "arg=" + value.ToString(Formatting.None),
            "json=true");
}
