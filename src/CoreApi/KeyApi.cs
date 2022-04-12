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
        if (json is null)
        {
            return null;
        }

        var keys = (JArray?)JObject.Parse(json)?["Keys"];
        return keys
            ?.Select(
                k => new KeyInfo
                {
                    Id = (string?)k?["Id"],
                    Name = (string?)k?["Name"]
                });
    }

    /// <inheritdoc />
    public async Task<IKey?> RemoveAsync(string name, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("key/rm", name, cancel);
        if (json is null)
        {
            return null;
        }

        var keys = JObject.Parse(json)?["Keys"] as JArray;

        return keys?
            .Select(k => new KeyInfo
            {
                Id = (string?)k?["Id"],
                Name = (string?)k?["Name"]
            })
            .First();
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
}
