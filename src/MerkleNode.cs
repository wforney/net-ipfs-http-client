using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Ipfs.Http
{
   /// <summary>
   ///   The IPFS <see href="https://github.com/ipfs/specs/tree/master/merkledag">MerkleDag</see> is the datastructure at the heart of IPFS. It is an acyclic directed graph whose edges are hashes.
   /// </summary>
   /// <remarks>
   ///   Initially an <b>MerkleNode</b> is just constructed with its Cid.
   /// </remarks>
   [DataContract]
   public class MerkleNode : IMerkleNode<IMerkleLink>, IEquatable<MerkleNode>
   {
      bool hasBlockStats;
      long blockSize;
      string name;
      IEnumerable<IMerkleLink> links;
      IpfsClient ipfsClient;

      /// <summary>
      ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
      ///   <see cref="Cid"/> and optional <see cref="Name"/>.
      /// </summary>
      /// <param name="id">
      ///   The <see cref="Cid"/> of the node.
      /// </param>
      /// <param name="name">A name for the node.</param>
      public MerkleNode( Cid id, string name = null )
      {
         Id = id ?? throw new ArgumentNullException( "id" );
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
      public MerkleNode( string path, string name = null )
      {
         if( string.IsNullOrWhiteSpace( path ) )
            throw new ArgumentNullException( "path" );

         if( path.StartsWith( "/ipfs/" ) )
            path = path[6..];

         Id = Cid.Decode( path );
         Name = name;
      }

      /// <summary>
      ///   Creates a new instance of the <see cref="MerkleNode"/> from the
      ///   <see cref="IMerkleLink"/>.
      /// </summary>
      /// <param name="link">The link to a node.</param>
      public MerkleNode( IMerkleLink link )
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
            if( ipfsClient == null )
            {
               lock( this )
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
      ///   The name for the node.  If unknown it is "" (not null).
      /// </summary>
      [DataMember]
      public string Name
      {
         get { return name; }
         set { name = value ?? string.Empty; }
      }

      /// <summary>
      ///   Size of the raw, encoded node.
      /// </summary>
      [DataMember]
      public long BlockSize
      {
         get
         {
            GetBlockStats();
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
         => links ??= IpfsClient.Object.LinksAsync( Id ).Result ;

      /// <inheritdoc />
      [DataMember]
      public byte[] DataBytes
       => IpfsClient.Block.GetAsync( Id ).Result.DataBytes;


      /// <inheritdoc />
      public Stream DataStream
       => IpfsClient.Block.GetAsync( Id ).Result.DataStream;

      /// <inheritdoc />
      public IMerkleLink ToLink( string name = null )
       => new DagLink( name ?? Name, Id, BlockSize );

      /// <summary>
      ///   Get block statistics about the node, <c>ipfs block stat <i>key</i></c>
      /// </summary>
      /// <remarks>
      ///   The object stats include the block stats.
      /// </remarks>
      void GetBlockStats()
      {
         if( hasBlockStats ) return;

         var stats = IpfsClient.Block.StatAsync( Id ).Result;
         blockSize = stats.Size;

         hasBlockStats = true;
      }

      /// <inheritdoc />
      public override int GetHashCode()
       => Id.GetHashCode();

      /// <inheritdoc />
      public override bool Equals( object obj )
      {
         var that = obj as MerkleNode;
         return that != null && this.Id == that.Id;
      }

      /// <inheritdoc />
      public bool Equals( MerkleNode that )
       => that != null && this.Id == that.Id;

      /// <summary>
      ///  TODO
      /// </summary>
      public static bool operator ==( MerkleNode a, MerkleNode b )
      {
         if( ReferenceEquals( a, b ) ) return true;
         if( a is null ) return false;
         if( b is null ) return false;

         return a.Equals( b );
      }

      /// <summary>
      ///  TODO
      /// </summary>
      public static bool operator !=( MerkleNode a, MerkleNode b )
      {
         if( ReferenceEquals( a, b ) ) return false;
         if( a is null ) return true;
         if( b is null ) return true;

         return !a.Equals( b );
      }

      /// <inheritdoc />
      public override string ToString()
      => "/ipfs/" + Id;

      /// <summary>
      ///  TODO
      /// </summary>
      static public implicit operator MerkleNode( string hash )
      => new MerkleNode( hash );

   }
}
