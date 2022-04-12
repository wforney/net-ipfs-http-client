namespace Ipfs.Http.Client;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The IPFS client interfact.
/// </summary>
public interface IIpfsClient
{
    /// <summary>
    /// Gets the list of peers that are initially trusted by IPFS.
    /// </summary>
    /// <remarks>
    /// This is equilivent to <c>ipfs bootstrap list</c>.
    /// </remarks>
    TrustedPeerCollection TrustedPeers { get; }

    /// <summary>
    /// Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a task.
    /// </summary>
    /// <param name="command">
    /// The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
    /// </param>
    /// <param name="arg">
    /// The optional argument to the command.
    /// </param>
    /// <param name="cancellationToken">
    /// Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <param name="options">
    /// The optional flags to the command.
    /// </param>
    /// <returns>
    /// A task.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// When the IPFS server indicates an error.
    /// </exception>
    Task ExecuteCommand(string command, string? arg = null, CancellationToken cancellationToken = default, params string[] options);

    /// <summary>
    /// Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a byte array, string or <see cref="Stream"/>.
    /// </summary>
    /// <param name="command">
    /// The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
    /// </param>
    /// <param name="arg">
    /// The optional argument to the command.
    /// </param>
    /// <param name="cancellationToken">
    /// Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <param name="options">
    /// The optional flags to the command.
    /// </param>
    /// <returns>
    /// A byte array, string or <see cref="Stream"/> containing the command's result.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// When the IPFS server indicates an error.
    /// </exception>
    Task<T?> ExecuteCommand<T>(string command, string? arg = null, CancellationToken cancellationToken = default, params string[] options);

    /// <summary>
    /// Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a string or stream.
    /// </summary>
    /// <param name="command">
    /// The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as <see href="https://ipfs.io/docs/api/#apiv0add">"add"</see>.
    /// </param>
    /// <param name="arg">
    /// The optional argument to the command.
    /// </param>
    /// <param name="data">
    /// A <see cref="Stream"/> containing the data to upload.
    /// </param>
    /// <param name="name">
    /// The name associated with the <paramref name="data"/>, can be <b>null</b>. Typically a filename, such as "hello.txt".
    /// </param>
    /// <param name="cancellationToken">
    /// Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
    /// </param>
    /// <param name="options">
    /// The optional flags to the command.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task's value is the HTTP response as the type of a <c>byte[]</c>, <see cref="string"/> or <see cref="Stream"/>.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// When the IPFS server indicates an error.
    /// </exception>
    Task<TR?> ExecuteCommand<T, TR>(string command, string? arg = null, T? data = default, string name = IpfsClient.IpfsHttpClientName, CancellationToken cancellationToken = default, params string[] options);
}
