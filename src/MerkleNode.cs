namespace Ipfs.Http.Client;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

/// <summary>
/// The IPFS <see href="https://github.com/ipfs/specs/tree/master/merkledag">MerkleDag</see> is the datastructure at the heart of IPFS. It is an acyclic directed graph whose edges are hashes.
/// </summary>
/// <remarks>
/// Initially an <b>MerkleNode</b> is just constructed with its Cid.
/// </remarks>
[DataContract]
public class MerkleNode : IMerkleNode<IMerkleLink>, IEquatable<MerkleNode>
{
    internal readonly IpfsContext ipfs;

    private long blockSize;
    private bool hasBlockStats;
    private IEnumerable<IMerkleLink>? links;
    private string? name = string.Empty;

    /// <summary>
    /// Creates a new instance of the <see cref="MerkleNode"/> with the specified
    /// <see cref="Cid"/> and optional <see cref="Name"/>.
    /// </summary>
    /// <param name="ipfs">The IPFS context.</param>
    /// <param name="id">
    /// The <see cref="Cid"/> of the node.
    /// </param>
    /// <param name="name">A name for the node.</param>
    public MerkleNode(IpfsContext ipfs, Cid id, string? name = null)
    {
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
        this.Id = id ?? throw new ArgumentNullException(nameof(id));
        this.Name = name;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MerkleNode"/> with the specified
    /// <see cref="Id">cid</see> and optional <see cref="Name"/>.
    /// </summary>
    /// <param name="ipfs">The IPFS context.</param>
    /// <param name="path">
    /// The string representation of a <see cref="Cid"/> of the node or "/ipfs/cid".
    /// </param>
    /// <param name="name">A name for the node.</param>
    public MerkleNode(IpfsContext ipfs, string path, string? name = null)
    {
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path.StartsWith("/ipfs/"))
        {
            path = path[6..];
        }

        this.Id = Cid.Decode(path);
        this.Name = name;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MerkleNode"/> from the
    /// <see cref="IMerkleLink"/>.
    /// </summary>
    /// <param name="ipfs">The IPFS context.</param>
    /// <param name="link">The link to a node.</param>
    public MerkleNode(IpfsContext ipfs, IMerkleLink link)
    {
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
        this.Id = link.Id;
        this.Name = link.Name;
        this.blockSize = link.Size;
        this.hasBlockStats = true;
    }

    /// <summary>
    /// Size of the raw, encoded node.
    /// </summary>
    [DataMember]
    public long BlockSize
    {
        get
        {
            this.GetBlockStats();
            return this.blockSize;
        }
    }

    /// <inheritdoc />
    [DataMember]
    public byte[] DataBytes => this.ipfs.Block.GetAsync(this.Id).Result.DataBytes;

    /// <inheritdoc />
    public Stream DataStream => this.ipfs.Block.GetAsync(this.Id).Result.DataStream;

    /// <inheritdoc />
    [DataMember]
    public Cid Id
    {
        get; private set;
    }

    /// <inheritdoc />
    [DataMember]
    public IEnumerable<IMerkleLink> Links
    {
        get
        {
            if (this.links is null)
            {
                this.links = this.ipfs.Object.LinksAsync(this.Id).Result;
            }

            return this.links;
        }
    }

    /// <summary>
    /// The name for the node.  If unknown it is "" (not null).
    /// </summary>
    [DataMember]
    public string? Name
    {
        get => this.name ?? string.Empty;

        set => this.name = value ?? string.Empty;
    }

    /// <inheritdoc />
    /// <seealso cref="BlockSize"/>
    [DataMember]
    public long Size => this.BlockSize;

    /// <summary>
    ///
    /// </summary>
    /// <param name="hash"></param>
    public static implicit operator MerkleNode(string hash) => new(IpfsContext.GetServiceProvider().GetRequiredService<IpfsContext>(), hash);

    /// <summary>
    ///
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator !=(MerkleNode a, MerkleNode b) => !ReferenceEquals(a, b) && (a is null || b is null || !a.Equals(b));

    /// <summary>
    ///
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator ==(MerkleNode a, MerkleNode b) => ReferenceEquals(a, b) || (a is not null && b is not null && a.Equals(b));

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var that = obj as MerkleNode;
        return that is not null && this.Id == that.Id;
    }

    /// <inheritdoc />
    public bool Equals(MerkleNode? that) => that is not null && this.Id == that.Id;

    /// <inheritdoc />
    public override int GetHashCode() => this.Id.GetHashCode();

    /// <inheritdoc />
    public IMerkleLink ToLink(string? name = null) => new DagLink(name ?? this.Name, this.Id, this.BlockSize);

    /// <inheritdoc />
    public override string ToString() => $"/ipfs/{this.Id}";

    /// <summary>
    /// Get block statistics about the node, <c>ipfs block stat <i>key</i></c>
    /// </summary>
    /// <remarks>
    /// The object stats include the block stats.
    /// </remarks>
    private void GetBlockStats()
    {
        if (this.hasBlockStats)
        {
            return;
        }

        var stats = this.ipfs.Block.StatAsync(this.Id).Result;
        this.blockSize = stats.Size;
        this.hasBlockStats = true;
    }
}
