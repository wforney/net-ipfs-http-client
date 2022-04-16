// <copyright file="KeyApi.cs" company="improvGroup, LLC">
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
/// Class KeyApi.
/// Implements the <see cref="Ipfs.CoreApi.IKeyApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IKeyApi" />
public partial class KeyApi : IKeyApi
{
    private readonly IIpfsClient ipfs;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    public KeyApi(IIpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public async Task<IKey?> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default) =>
        await this.ipfs.ExecuteCommand<KeyInfo?>(
            "key/gen",
            name,
            cancel,
            $"type={keyType}",
            $"size={size}");

    /// <inheritdoc />
    public Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<IKey> ImportAsync(string name, string pem, char[]? password = null, CancellationToken cancel = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public async Task<IEnumerable<IKey?>?> ListAsync(CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("key/list", null, cancel, "l=true");

        return FromJson(json);
    }

    /// <inheritdoc />
    public async Task<IKey?> RemoveAsync(string name, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("key/rm", name, cancel);

        return FromJson(json).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IKey?> RenameAsync(string oldName, string newName, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("key/rename", oldName, cancel, $"arg={newName}");
        if (json is null)
        {
            return null;
        }

        var key = JObject.Parse(json);
        return new KeyInfo
        {
            Id = (string?)key?["Id"],
            Name = (string?)key?["Now"]
        };
    }

    /// <summary>
    /// Returns an <see cref="IKey" /> from the specified JSON <see cref="string" />.
    /// </summary>
    /// <param name="json">The JSON <see cref="string" />.</param>
    /// <returns>An <see cref="IKey"/>.</returns>
    private static IEnumerable<IKey> FromJson(string? json)
    {
        if (json is null)
        {
            yield break;
        }

        var obj = JObject.Parse(json);
        if (obj is null)
        {
            yield break;
        }

        var keys = (JArray?)obj["Keys"];
        if (keys is null)
        {
            yield break;
        }

        foreach (var key in keys)
        {
            var id = (string?)key["Id"];
            var name = ((string?)key["Name"])?.Trim();

            yield return new KeyInfo
            {
                Id = string.IsNullOrWhiteSpace(id) ? null : new MultiHash(id),
                Name = name
            };
        }
    }
}
