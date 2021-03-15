using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Http
{
	class BlockRepositoryApi : IBlockRepositoryApi
    {
		readonly IpfsClient ipfs;

        internal BlockRepositoryApi(IpfsClient ipfs)
        => this.ipfs = ipfs;

        public async Task RemoveGarbageAsync(CancellationToken cancel = default)
        => await ipfs.DoCommandAsync("repo/gc", cancel);

        public Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default)
        => ipfs.DoCommandAsync<RepositoryData>("repo/stat", cancel);

        public async Task VerifyAsync(CancellationToken cancel = default)
        => await ipfs.DoCommandAsync("repo/verify", cancel);

        public async Task<string> VersionAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("repo/version", cancel);
            var info = JObject.Parse(json);
            return (string)info["Version"];
        }
    }
}
