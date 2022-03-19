namespace Ipfs.Http;

/// <summary>
/// Base class for an api.
/// </summary>
public class BaseApi
{
	/// <summary>
	/// Gets the client context.
	/// </summary>
	protected IpfsClient Client { get; }

	/// <summary>
	/// Creates a new instance of a <see cref="BaseApi"/>
	/// </summary>
	/// <param name="client"></param>
	protected BaseApi( IpfsClient client )
		=> Client = client;
}
