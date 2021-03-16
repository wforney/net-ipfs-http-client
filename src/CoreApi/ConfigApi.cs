using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Http
{
   class ConfigApi : BaseApi, IConfigApi
   {
      internal ConfigApi( IpfsClient ipfs ) : base( ipfs ) { }

      public async Task<JObject> GetAsync( CancellationToken cancel = default )
      {
         var json = await Client.DoCommandAsync( "config/show", cancel );
         return JObject.Parse( json );
      }

      public async Task<JToken> GetAsync( string key, CancellationToken cancel = default )
      {
         var json = await Client.DoCommandAsync( "config", cancel, key );
         var r = JObject.Parse( json );
         return r["Value"];
      }

      public async Task SetAsync( string key, string value, CancellationToken cancel = default )
      {
         var _ = await Client.DoCommandAsync( "config", cancel, key, "arg=" + value );
         return;
      }

      public async Task SetAsync( string key, JToken value, CancellationToken cancel = default )
      {
         var _ = await Client.DoCommandAsync( "config", cancel,
             key,
             "arg=" + value.ToString( Formatting.None ),
             "json=true" );
         return;
      }

      public async Task ReplaceAsync( JObject config )
      {
         var data = Encoding.UTF8.GetBytes( config.ToString( Formatting.None ) );
         await Client.UploadAsync( "config/replace", CancellationToken.None, data );
      }
   }

}
