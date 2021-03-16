using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using System.IO;

namespace Ipfs.Http
{

	class BlockApi : BaseApi, IBlockApi
   {
      internal BlockApi( IpfsClient ipfs )
         : base( ipfs ) { }

      public async Task<IDataBlock> GetAsync( Cid id, CancellationToken cancel = default( CancellationToken ) )
      {
         var data = await Client.DownloadBytesAsync( "block/get", cancel, id );
         return new Block
         {
            DataBytes = data,
            Id = id
         };
      }

      public async Task<Cid> PutAsync(
          byte[] data,
          string contentType = Cid.DefaultContentType,
          string multiHash = MultiHash.DefaultAlgorithmName,
          string encoding = MultiBase.DefaultAlgorithmName,
          bool pin = false,
          CancellationToken cancel = default( CancellationToken ) )
      {
         var options = new List<string>();
         if( multiHash != MultiHash.DefaultAlgorithmName ||
             contentType != Cid.DefaultContentType ||
             encoding != MultiBase.DefaultAlgorithmName )
         {
            options.Add( $"mhtype={multiHash}" );
            options.Add( $"format={contentType}" );
            options.Add( $"cid-base={encoding}" );
         }
         var json = await Client.UploadAsync( "block/put", cancel, data, options.ToArray() );
         var info = JObject.Parse( json );
         Cid cid = (string)info["Key"];

         if( pin )
         {
            await Client.Pin.AddAsync( cid, recursive: false, cancel: cancel );
         }

         return cid;
      }

      public async Task<Cid> PutAsync(
          Stream data,
          string contentType = Cid.DefaultContentType,
          string multiHash = MultiHash.DefaultAlgorithmName,
          string encoding = MultiBase.DefaultAlgorithmName,
          bool pin = false,
          CancellationToken cancel = default( CancellationToken ) )
      {
         var options = new List<string>();
         if( multiHash != MultiHash.DefaultAlgorithmName ||
             contentType != Cid.DefaultContentType ||
             encoding != MultiBase.DefaultAlgorithmName )
         {
            options.Add( $"mhtype={multiHash}" );
            options.Add( $"format={contentType}" );
            options.Add( $"cid-base={encoding}" );
         }
         var json = await Client.UploadAsync( "block/put", cancel, data, null, options.ToArray() );
         var info = JObject.Parse( json );
         Cid cid = (string)info["Key"];

         if( pin )
         {
            await Client.Pin.AddAsync( cid, recursive: false, cancel: cancel );
         }

         return cid;
      }

      public async Task<IDataBlock> StatAsync( Cid id, CancellationToken cancel = default( CancellationToken ) )
      {
         var json = await Client.DoCommandAsync( "block/stat", cancel, id );
         var info = JObject.Parse( json );
         return new Block
         {
            Size = (long)info["Size"],
            Id = (string)info["Key"]
         };
      }

      public async Task<Cid> RemoveAsync( Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default( CancellationToken ) )
      {
         var json = await Client.DoCommandAsync( "block/rm", cancel, id, "force=" + ignoreNonexistent.ToString().ToLowerInvariant() );
         if( json.Length == 0 )
            return null;
         var result = JObject.Parse( json );
         var error = (string)result["Error"];
         if( error != null )
            throw new HttpRequestException( error );
         return (Cid)(string)result["Hash"];
      }

   }

}
