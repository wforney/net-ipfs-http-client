namespace Ipfs.Http.Client;

using DotNext.Threading;
using Ipfs.CoreApi;
using System.Runtime.Serialization;

/// <inheritdoc />
[DataContract]
public class FileSystemNode : IFileSystemNode
{
    private readonly AsyncLazy<Stream> dataStream;
    private readonly IFileSystemApi fileSystemApi;
    private bool? isDirectory;
    private IEnumerable<IFileSystemLink>? links;
    private long? size;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemNode"/> class.
    /// </summary>
    /// <param name="fileSystemApi">The file system API.</param>
    public FileSystemNode(IFileSystemApi fileSystemApi)
    {
        this.fileSystemApi = fileSystemApi ?? throw new ArgumentNullException(nameof(fileSystemApi));
        this.dataStream = new(() => this.fileSystemApi.ReadFileAsync(this.Id));
    }

    /// <inheritdoc />
    public byte[] DataBytes
    {
        get
        {
            if (this.DataStream is null)
            {
                return Array.Empty<byte>();
            }

            using var data = new MemoryStream();
            this.DataStream.CopyTo(data);
            return data.ToArray();
        }
    }

    /// <inheritdoc />
    public Stream DataStream => this.dataStream.GetAwaiter().GetResult();

    /// <inheritdoc />
    [DataMember]
    public Cid? Id { get; set; }

    /// <summary>
    /// Determines if the link is a directory (folder).
    /// </summary>
    /// <value>
    /// <b>true</b> if the link is a directory; Otherwise <b>false</b>, the link is some type of a file.
    /// </value>
    [DataMember]
    public bool IsDirectory
    {
        get
        {
            if (!this.isDirectory.HasValue)
            {
                this.GetInfo().Wait();
            }

            return this.isDirectory.GetValueOrDefault();
        }

        set => this.isDirectory = value;
    }

    /// <inheritdoc />
    [DataMember]
    public IEnumerable<IFileSystemLink> Links
    {
        get
        {
            if (this.links is null)
            {
                this.GetInfo().Wait();
            }

            return this.links ?? Enumerable.Empty<IFileSystemLink>();
        }

        set => this.links = value;
    }

    /// <summary>
    /// Gets or sets the file name of the IPFS node.
    /// </summary>
    /// <value>The file name of the IPFS node.</value>
    [DataMember]
    public string? Name { get; set; }

    /// <inheritdoc />
    [DataMember]
    public long Size
    {
        get
        {
            if (!this.size.HasValue)
            {
                this.GetInfo().Wait();
            }

            return this.size.GetValueOrDefault();
        }

        set => this.size = value;
    }

    /// <inheritdoc />
    public IFileSystemLink ToLink(string name = "") =>
        new FileSystemLink
        {
            Name = string.IsNullOrWhiteSpace(name) ? this.Name : name,
            Id = this.Id,
            Size = this.Size,
        };

    /// <summary>
    /// Gets the information.
    /// </summary>
    private async Task GetInfo()
    {
        var node = await this.fileSystemApi.ListFileAsync(this.Id);

        this.IsDirectory = node?.IsDirectory ?? false;
        this.Links = node?.Links ?? Enumerable.Empty<IFileSystemLink>();
        this.Size = node?.Size ?? 0L;
    }
}
