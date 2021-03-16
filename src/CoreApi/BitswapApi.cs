using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Http
{

   class BitswapApi : BaseApi, IBitswapApi
   {

      internal BitswapApi( IpfsClient ipfs )
         : base( ipfs ) { }

      public Task<IDataBlock> GetAsync( Cid id, CancellationToken cancel = default )
      => Client.Block.GetAsync( id, cancel );

      public async Task<IEnumerable<Cid>> WantsAsync( MultiHash peer = null, CancellationToken cancel = default( CancellationToken ) )
      {
         var json = await Client.DoCommandAsync( "bitswap/wantlist", cancel, peer?.ToString() );
         var keys = (JArray)( JObject.Parse( json )["Keys"] );
         // https://github.com/ipfs/go-ipfs/issues/5077
         return keys
             .Select( k =>
              {
                if( k.Type == JTokenType.String )
                   return Cid.Decode( k.ToString() );
                var obj = (JObject)k;
                return Cid.Decode( obj["/"].ToString() );
             } );
      }

      public async Task UnwantAsync( Cid id, CancellationToken cancel = default )
      => await Client.DoCommandAsync( "bitswap/unwant", cancel, id );

      public async Task<BitswapLedger> LedgerAsync( Peer peer, CancellationToken cancel = default( CancellationToken ) )
      {
         var json = await Client.DoCommandAsync( "bitswap/ledger", cancel, peer.Id.ToString() );
         var o = JObject.Parse( json );
         return new BitswapLedger
         {
            Peer = (string)o["Peer"],
            DataReceived = (ulong)o["Sent"],
            DataSent = (ulong)o["Recv"],
            BlocksExchanged = (ulong)o["Exchanged"]
         };
      }
   }

}
