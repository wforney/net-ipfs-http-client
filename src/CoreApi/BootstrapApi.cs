using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Http
{

   class BootstrapApi : BaseApi, IBootstrapApi
   {

      internal BootstrapApi( IpfsClient ipfs ) : base( ipfs ) { }

      public async Task<MultiAddress> AddAsync( MultiAddress address, CancellationToken cancel = default )
      {
         var json = await Client.DoCommandAsync( "bootstrap/add", cancel, address.ToString() );
         var addrs = (JArray)( JObject.Parse( json )["Peers"] );
         var a = addrs.FirstOrDefault();
         if( a == null )
            return null;
         return new MultiAddress( (string)a );
      }

      public async Task<IEnumerable<MultiAddress>> AddDefaultsAsync( CancellationToken cancel = default )
      {
         var json = await Client.DoCommandAsync( "bootstrap/add/default", cancel );
         var addrs = (JArray)( JObject.Parse( json )["Peers"] );
         return addrs
             .Select( a => MultiAddress.TryCreate( (string)a ) )
             .Where( ma => ma != null );
      }

      public async Task<IEnumerable<MultiAddress>> ListAsync( CancellationToken cancel = default )
      {
         var json = await Client.DoCommandAsync( "bootstrap/list", cancel );
         var addrs = (JArray)( JObject.Parse( json )["Peers"] );
         return addrs
             .Select( a => MultiAddress.TryCreate( (string)a ) )
             .Where( ma => ma != null );
      }

      public Task RemoveAllAsync( CancellationToken cancel = default )
      => Client.DoCommandAsync( "bootstrap/rm/all", cancel );

      public async Task<MultiAddress> RemoveAsync( MultiAddress address, CancellationToken cancel = default( CancellationToken ) )
      {
         var json = await Client.DoCommandAsync( "bootstrap/rm", cancel, address.ToString() );
         var addrs = (JArray)( JObject.Parse( json )["Peers"] );
         var a = addrs.FirstOrDefault();
         if( a == null )
            return null;
         return new MultiAddress( (string)a );
      }
   }

}
