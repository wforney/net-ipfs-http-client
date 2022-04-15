// <copyright file="FileSystemApi.cs" company="improvGroup, LLC">
//     Copyright © 2015-2022 Richard Schneider, Marshall Rosenstein, William Forney
// </copyright>
namespace Ipfs.Http.Client.CoreApi;

using DotNext.Threading;
using Ipfs.CoreApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

/// <summary>
/// Class FileSystemApi.
/// Implements the <see cref="Ipfs.CoreApi.IFileSystemApi" />
/// </summary>
/// <seealso cref="Ipfs.CoreApi.IFileSystemApi" />
public class FileSystemApi : IFileSystemApi
{
    private readonly AsyncLazy<DagNode> emptyFolder;
    private readonly IIpfsClient ipfs;
    private readonly ILogger<FileSystemApi> logger;
    private readonly IObjectApi objectApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemApi"/> class.
    /// </summary>
    /// <param name="ipfs">The ipfs.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="objectApi">The object API.</param>
    /// <exception cref="System.ArgumentNullException">ipfs</exception>
    /// <exception cref="System.ArgumentNullException">logger</exception>
    /// <exception cref="System.ArgumentNullException">objectApi</exception>
    public FileSystemApi(IIpfsClient ipfs, ILogger<FileSystemApi> logger, IObjectApi objectApi)
    {
        this.ipfs = ipfs ?? throw new ArgumentNullException(nameof(ipfs));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.objectApi = objectApi ?? throw new ArgumentNullException(nameof(objectApi));

        this.emptyFolder = new AsyncLazy<DagNode>(async () => await objectApi.NewDirectoryAsync());
    }

    /// <inheritdoc />
    public async Task<IFileSystemNode> AddAsync(Stream stream, string name = "", AddFileOptions? options = null, CancellationToken cancel = default)
    {
        if (options is null)
        {
            options = new AddFileOptions();
        }

        var opts = new List<string>();
        if (!options.Pin)
        {
            opts.Add("pin=false");
        }

        if (options.Wrap)
        {
            opts.Add("wrap-with-directory=true");
        }

        if (options.RawLeaves)
        {
            opts.Add("raw-leaves=true");
        }

        if (options.OnlyHash)
        {
            opts.Add("only-hash=true");
        }

        if (options.Trickle)
        {
            opts.Add("trickle=true");
        }

        if (options.Progress is not null)
        {
            opts.Add("progress=true");
        }

        if (options.Hash != MultiHash.DefaultAlgorithmName)
        {
            opts.Add($"hash=${options.Hash}");
        }

        if (options.Encoding != MultiBase.DefaultAlgorithmName)
        {
            opts.Add($"cid-base=${options.Encoding}");
        }

        if (!string.IsNullOrWhiteSpace(options.ProtectionKey))
        {
            opts.Add($"protect={options.ProtectionKey}");
        }

        opts.Add($"chunker=size-{options.ChunkSize}");

        var response = await this.ipfs.ExecuteCommand<Stream?, Stream?>("add", null, stream, name, cancel, opts.ToArray());

        // The result is a stream of LDJSON objects.
        // See https://github.com/ipfs/go-ipfs/issues/4852
        FileSystemNode? fsn = null;
        if (response is not null && response.CanRead)
        {
            using var sr = new StreamReader(response);
            using var jr = new JsonTextReader(sr) { SupportMultipleContent = true };
            while (await jr.ReadAsync(cancel))
            {
                var r = await JObject.LoadAsync(jr, cancel);

                // If a progress report.
                if (r?.ContainsKey("Bytes") ?? false)
                {
                    options.Progress?.Report(
                        new TransferProgress
                        {
                            Name = (string?)r?["Name"],
                            Bytes = (ulong?)r?["Bytes"] ?? 0
                        });
                }
                else
                {
                    // Else must be an added file.
                    fsn = new FileSystemNode(this)
                    {
                        Id = (string?)r?["Hash"],
                        Size = (string?)r?["Size"] is null ? 0L : long.Parse((string?)r["Size"] ?? "0"),
                        IsDirectory = false,
                        Name = name,
                    };
                    if (this.logger.IsEnabled(LogLevel.Debug))
                    {
                        this.logger.LogDebug("added {fsnId} {fsnName}", fsn.Id, fsn.Name);
                    }
                }
            }
        }

        if (fsn is null)
        {
            throw new InvalidOperationException("No file added.");
        }

        fsn.IsDirectory = options.Wrap;
        return fsn;
    }

    /// <inheritdoc />
    public async Task<IFileSystemNode> AddDirectoryAsync(string path, bool recursive = true, AddFileOptions? options = null, CancellationToken cancel = default)
    {
        if (options is null)
        {
            options = new AddFileOptions();
        }

        options.Wrap = false;

        // Add the files and sub-directories.
        path = Path.GetFullPath(path);
        var files = Directory
            .EnumerateFiles(path)
            .Select(p => this.AddFileAsync(p, options, cancel));
        if (recursive)
        {
            var folders = Directory
                .EnumerateDirectories(path)
                .Select(dir => this.AddDirectoryAsync(dir, recursive, options, cancel));
            files = files.Union(folders);
        }

        // go-ipfs v0.4.14 sometimes fails when sending lots of 'add file'
        // requests.  It's happy with adding one file at a time.
        var links = new List<IFileSystemLink>();
        foreach (var file in files)
        {
            var node = await file;
            links.Add(node.ToLink());
        }

        // Create the directory with links to the created files and sub-directories
        var folder = (await this.emptyFolder)?.AddLinks(links);
        var directory = await this.objectApi.PutAsync(folder, cancel);

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("added {directoryId} {fileName}", directory.Id, Path.GetFileName(path));
        }

        return new FileSystemNode(this)
        {
            Id = directory.Id,
            Name = Path.GetFileName(path),
            Links = links,
            IsDirectory = true,
            Size = directory.Size,
        };
    }

    /// <inheritdoc />
    public async Task<IFileSystemNode> AddFileAsync(string path, AddFileOptions? options = null, CancellationToken cancel = default)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var node = await this.AddAsync(stream, Path.GetFileName(path), options, cancel);
        return node;
    }

    /// <inheritdoc />
    public Task<IFileSystemNode> AddTextAsync(string text, AddFileOptions? options = null, CancellationToken cancel = default) =>
        this.AddAsync(new MemoryStream(Encoding.UTF8.GetBytes(text), false), "", options, cancel);

    /// <inheritdoc />
    public Task<Stream?> GetAsync(string path, bool compress = false, CancellationToken cancel = default) =>
            this.ipfs.ExecuteCommand<Stream>("get", path, cancel, $"compress={compress}");

    /// <summary>
    /// Get information about the file or directory.
    /// </summary>
    /// <param name="path">A path to an existing file or directory, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
    /// or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"</param>
    /// <param name="cancel">Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.</param>
    /// <returns>A Task&lt;IFileSystemNode&gt; representing the asynchronous operation.</returns>
    public async Task<IFileSystemNode?> ListFileAsync(string path, CancellationToken cancel = default)
    {
        var json = await this.ipfs.ExecuteCommand<string?>("file/ls", path, cancel);
        if (json is null)
        {
            return null;
        }

        var r = JObject.Parse(json);
        var hash = (string?)r?["Arguments"]?[path];
        var o = hash is null ? null : (JObject?)r?["Objects"]?[hash];
        var node = new FileSystemNode(this)
        {
            Id = (string?)o?["Hash"],
            Size = (long)(o?["Size"] ?? 0),
            IsDirectory = (string?)o?["Type"] == "Directory",
            Links = Array.Empty<FileSystemLink>(),
        };
        var links = o?["Links"] as JArray;
        if (links is not null)
        {
            node.Links = links
                .Select(l => new FileSystemLink()
                {
                    Name = (string?)l?["Name"],
                    Id = (string?)l?["Hash"],
                    Size = (long)(l?["Size"] ?? 0),
                })
                .ToArray();
        }

        return node;
    }

    /// <summary>
    /// Reads the content of an existing IPFS file as text.
    /// </summary>
    /// <param name="path">A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
    /// or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"</param>
    /// <param name="cancel">Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.</param>
    /// <returns>The contents of the <paramref name="path" /> as a <see cref="string" />.</returns>
    public async Task<string?> ReadAllTextAsync(string path, CancellationToken cancel = default)
    {
        using var data = await this.ReadFileAsync(path, cancel);
        if (data is null)
        {
            return null;
        }

        using var text = new StreamReader(data);
        return await text.ReadToEndAsync();
    }

    /// <summary>
    /// Reads the content of an existing IPFS file as text.
    /// </summary>
    /// <param name="path">A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
    /// or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"</param>
    /// <param name="host">Set a host to override the base ApiUrl</param>
    /// <param name="cancel">Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.</param>
    /// <returns>The contents of the <paramref name="path" /> as a <see cref="string" />.</returns>
    public async Task<string?> ReadAllTextHostAsync(string path, string host, CancellationToken cancel = default)
    {
        using var data = await this.ReadFileHostAsync(path, host, cancel);
        if (data is null)
        {
            return null;
        }

        using var text = new StreamReader(data);
        return await text.ReadToEndAsync();
    }

    /// <summary>
    /// Opens an existing IPFS file for reading.
    /// </summary>
    /// <param name="path">A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
    /// or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"</param>
    /// <param name="cancel">Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.</param>
    /// <returns>A <see cref="Stream" /> to the file contents.</returns>
    /// <remarks>The returned <see cref="T:System.IO.Stream" /> must be disposed.</remarks>
    public Task<Stream?> ReadFileAsync(string path, CancellationToken cancel = default) =>
        this.ipfs.ExecuteCommand<Stream>("cat", path, cancel);

    /// <summary>
    /// Reads the file asynchronous.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length.</param>
    /// <param name="cancel">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Task&lt;Stream&gt;.</returns>
    /// <exception cref="System.NotSupportedException">Only int offsets are currently supported.</exception>
    /// <exception cref="System.NotSupportedException">Only int lengths are currently supported.</exception>
    public Task<Stream?> ReadFileAsync(string path, long offset, long length = 0, CancellationToken cancel = default)
    {
        // https://github.com/ipfs/go-ipfs/issues/5380
        if (offset > int.MaxValue)
        {
            throw new NotSupportedException("Only int offsets are currently supported.");
        }

        if (length > int.MaxValue)
        {
            throw new NotSupportedException("Only int lengths are currently supported.");
        }

        if (length == 0)
        {
            length = int.MaxValue; // go-ipfs only accepts int lengths
        }

        return this.ipfs.ExecuteCommand<Stream>("cat", path, cancel, $"offset={offset}", $"length={length}");
    }

    /// <summary>
    /// Opens an existing IPFS file for reading.
    /// </summary>
    /// <param name="path">A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
    /// or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"</param>
    /// <param name="host">Set a host to override the base ApiUrl</param>
    /// <param name="cancel">Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.</param>
    /// <returns>A <see cref="Stream" /> to the file contents.</returns>
    public Task<Stream?> ReadFileHostAsync(string path, string host, CancellationToken cancel = default) =>
        this.ipfs.ExecuteCommand<Stream>("cat", host, cancel, path);
}
