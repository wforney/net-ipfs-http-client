namespace Ipfs.Http.Client;

using System.Collections;

/// <summary>
/// A list of trusted peers.
/// </summary>
/// <remarks>
/// This is the list of peers that are initially trusted by IPFS. Its equivalent to the
/// <see href="https://ipfs.io/ipfs/QmTkzDwWqPbnAh5YiV5VwcTLnGdwSNsNTn2aDxdXBFca7D/example#/ipfs/QmThrNbvLj7afQZhxH72m5Nn1qiVn3eMKWFYV49Zp2mv9B/bootstrap/readme.md">ipfs bootstrap</see> command.
/// </remarks>
/// <returns>
/// A series of <see cref="MultiAddress"/>.  Each address ends with an IPNS node id, for example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ".
/// </returns>
public class TrustedPeerCollection : ICollection<MultiAddress>
{
    private readonly IIpfsClient ipfs;

    private MultiAddress[] peers = Array.Empty<MultiAddress>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TrustedPeerCollection"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    internal TrustedPeerCollection(IpfsClient ipfs) => this.ipfs = ipfs;

    /// <inheritdoc />
    public int Count
    {
        get
        {
            if (this.peers is null || this.peers.Length == 0)
            {
                this.Fetch().Wait();
            }

            return this.peers?.Length ?? 0;
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(MultiAddress peer)
    {
        _ = peer ?? throw new ArgumentNullException(nameof(peer));
        this.ipfs.ExecuteCommand("bootstrap/add", peer.ToString()).Wait();
        this.peers = Array.Empty<MultiAddress>();
    }

    /// <summary>
    ///    Add the default bootstrap nodes to the trusted peers.
    /// </summary>
    /// <remarks>
    ///    Equivalent to <c>ipfs bootstrap add --default</c>.
    /// </remarks>
    public void AddDefaultNodes()
    {
        this.ipfs.ExecuteCommand("bootstrap/add", null, default, "default=true").Wait();
        this.peers = Array.Empty<MultiAddress>();
    }

    /// <summary>
    ///    Remove all the trusted peers.
    /// </summary>
    /// <remarks>
    ///    Equivalent to <c>ipfs bootstrap rm --all</c>.
    /// </remarks>
    public void Clear()
    {
        this.ipfs.ExecuteCommand("bootstrap/rm", null, default, "all=true").Wait();
        this.peers = Array.Empty<MultiAddress>();
    }

    /// <inheritdoc />
    public bool Contains(MultiAddress item)
    {
        this.Fetch().Wait();
        return this.peers.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(MultiAddress[] array, int index)
    {
        this.Fetch().Wait();
        this.peers.CopyTo(array, index);
    }

    /// <inheritdoc />
    public IEnumerator<MultiAddress> GetEnumerator()
    {
        this.Fetch().Wait();
        return ((IEnumerable<MultiAddress>)this.peers).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        this.Fetch().Wait();
        return this.peers.GetEnumerator();
    }

    /// <summary>
    ///    Remove the trusted peer.
    /// </summary>
    /// <remarks>
    ///    Equivalent to <c>ipfs bootstrap rm <i>peer</i></c>.
    /// </remarks>
    public bool Remove(MultiAddress peer)
    {
        _ = peer ?? throw new ArgumentNullException(nameof(peer));
        this.ipfs.ExecuteCommand("bootstrap/rm", null, default, peer.ToString()).Wait();
        this.peers = Array.Empty<MultiAddress>();
        return true;
    }

    private async Task Fetch() => this.peers = (await this.ipfs.ExecuteCommand<BootstrapListResponse?>("bootstrap/list"))?.Peers ?? Array.Empty<MultiAddress>();

    private class BootstrapListResponse
    {
        private MultiAddress[] peers = Array.Empty<MultiAddress>();

        /// <summary>
        /// Gets or sets the peers.
        /// </summary>
        /// <value>The peers.</value>
        public MultiAddress[] Peers { get => this.peers; set => this.peers = value ?? Array.Empty<MultiAddress>(); }
    }
}
