namespace Ipfs.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    ///   The IPFS <see href="https://github.com/ipfs/specs/tree/master/merkledag">MerkleDag</see> is the datastructure at the heart of IPFS. It is an acyclic directed graph whose edges are hashes.
    /// </summary>
    /// <remarks>
    ///   Initially an <b>MerkleNode</b> is just constructed with its Cid.
    /// </remarks>
    [DataContract]
    public class MerkleNode : IMerkleNode<IMerkleLink>, IEquatable<MerkleNode>
    {
        private bool hasBlockStats;
        private long blockSize;
        private string name;
        private IEnumerable<IMerkleLink> links;
        private IpfsClient ipfsClient;

        /// <summary>
        ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
        ///   <see cref="Cid"/> and optional <see cref="Name"/>.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the node.
        /// </param>
        /// <param name="name">A name for the node.</param>
        public MerkleNode(Cid id, string name = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
        ///   <see cref="Id">cid</see> and optional <see cref="Name"/>.
        /// </summary>
        /// <param name="path">
        ///   The string representation of a <see cref="Cid"/> of the node or "/ipfs/cid".
        /// </param>
        /// <param name="name">A name for the node.</param>
        public MerkleNode(string path, string name = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            if (path.StartsWith("/ipfs/"))
            {
                path = path.Substring(6);
            }

            Id = Cid.Decode(path);
            Name = name;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="MerkleNode"/> from the
        ///   <see cref="IMerkleLink"/>.
        /// </summary>
        /// <param name="link">The link to a node.</param>
        public MerkleNode(IMerkleLink link)
        {
            Id = link.Id;
            Name = link.Name;
            blockSize = link.Size;
            hasBlockStats = true;
        }

        internal IpfsClient IpfsClient
        {
            get
            {
                if (ipfsClient is null)
                {
                    lock (this)
                    {
                        ipfsClient = new IpfsClient();
                    }
                }

                return ipfsClient;
            }

            set => ipfsClient = value;
        }

        /// <inheritdoc />
        [DataMember]
        public Cid Id { get; private set; }

        /// <summary>
        ///   Gets or sets the name for the node.  If unknown it is "" (not null).
        /// </summary>
        [DataMember]
        public string Name
        {
            get => name;
            set => name = value ?? string.Empty;
        }

        /// <summary>
        ///   Gets the size of the raw, encoded node.
        /// </summary>
        [DataMember]
        public long BlockSize
        {
            get
            {
                if (hasBlockStats)
                {
                    return blockSize;
                }

                var stats = IpfsClient.Block.StatAsync(Id).GetAwaiter().GetResult();
                blockSize = stats.Size;

                hasBlockStats = true;

                return blockSize;
            }
        }

        /// <inheritdoc />
        /// <seealso cref="BlockSize"/>
        [DataMember]
        public long Size => BlockSize;

        /// <inheritdoc />
        [DataMember]
        public IEnumerable<IMerkleLink> Links
        {
            get
            {
                if (links is null)
                {
                    links = IpfsClient.Object.LinksAsync(Id).GetAwaiter().GetResult();
                }

                return links;
            }
        }

        /// <inheritdoc />
        [DataMember]
        public byte[] DataBytes => IpfsClient.Block.GetAsync(Id).Result.DataBytes;

        /// <inheritdoc />
        public Stream DataStream => IpfsClient.Block.GetAsync(Id).Result.DataStream;

        /// <inheritdoc />
        public IMerkleLink ToLink(string name = null) => new DagLink(name ?? Name, Id, BlockSize);

        /// <inheritdoc />
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as MerkleNode;
            return that != null && this.Id == that.Id;
        }

        /// <inheritdoc />
        public bool Equals(MerkleNode that) => that != null && this.Id == that.Id;

        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="a">The first.</param>
        /// <param name="b">The second.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MerkleNode a, MerkleNode b) => ReferenceEquals(a, b) || (!(a is null) && !(b is null) && a.Equals(b));

        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MerkleNode a, MerkleNode b) => !ReferenceEquals(a, b) && (a is null || b is null || !a.Equals(b));

        /// <inheritdoc />
        public override string ToString() => $"/ipfs/{Id}";

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="MerkleNode"/>.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator MerkleNode(string hash) => new MerkleNode(hash);
    }
}
