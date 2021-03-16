using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Http
{
   class DnsApi : BaseApi, IDnsApi
   {
      internal DnsApi( IpfsClient ipfs ) : base( ipfs ) { }

      public async Task<string> ResolveAsync( 
         string name, 
         bool recursive = false, 
         CancellationToken cancel = default )
      {
         var json = await Client.DoCommandAsync( "dns", cancel,
             name,
             $"recursive={recursive.ToString().ToLowerInvariant()}" );
         var path = (string)( JObject.Parse( json )["Path"] );
         return path;
      }
   }
}
