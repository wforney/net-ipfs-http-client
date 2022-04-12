namespace Ipfs.Http.Client;

/// <summary>
/// A link to another file system node in IPFS. Implements the <see cref="IFileSystemLink" />.
/// </summary>
/// <seealso cref="IFileSystemLink" />
public class FileSystemLink : IFileSystemLink
{
    /// <inheritdoc />
    public Cid? Id { get; set; }

    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public long Size { get; set; }
}
