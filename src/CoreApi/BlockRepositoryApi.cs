using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Http
{
	class BlockRepositoryApi : BaseApi, IBlockRepositoryApi
    {
      internal BlockRepositoryApi( IpfsClient ipfs )
       : base( ipfs ) { }

        public async Task RemoveGarbageAsync(CancellationToken cancel = default)
        => await Client.DoCommandAsync("repo/gc", cancel);

        public Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default)
        => Client.DoCommandAsync<RepositoryData>("repo/stat", cancel);

        public async Task VerifyAsync(CancellationToken cancel = default)
        => await Client.DoCommandAsync("repo/verify", cancel);

        public async Task<string> VersionAsync(CancellationToken cancel = default)
        {
            var json = await Client.DoCommandAsync("repo/version", cancel);
            var info = JObject.Parse(json);
            return (string)info["Version"];
        }
    }
}
