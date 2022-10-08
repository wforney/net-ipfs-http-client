namespace Ipfs.Http
{
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Threading.Tasks;

	/// <inheritdoc />
	[DataContract]
	public class FileSystemNode : IFileSystemNode
	{
		private IpfsClient ipfsClient;
		private bool? isDirectory;
		private IEnumerable<IFileSystemLink> links;
		private long? size;

		/// <inheritdoc />
		public byte[] DataBytes
		{
			get
			{
				using (var stream = DataStream)
				{
					if (DataStream is null)
					{
						return null;
					}

					using (var data = new MemoryStream())
					{
						stream.CopyTo(data);
						return data.ToArray();
					}
				}
			}
		}

		/// <inheritdoc />
		public Stream DataStream => IpfsClient?.FileSystem.ReadFileAsync(Id).Result;

		/// <inheritdoc />
		[DataMember]
		public Cid Id { get; set; }

		/// <summary>
		/// The client to IPFS.
		/// </summary>
		/// <value>Used to fetch additional information on the node.</value>
		public IpfsClient IpfsClient
		{
			get
			{
				if (ipfsClient is null)
				{
					lock (this)
					{
						ipfsClient = new IpfsClient();
					}
				}

				return ipfsClient;
			}

			set => ipfsClient = value;
		}

		/// <summary>
		/// Determines if the link is a directory (folder).
		/// </summary>
		/// <value>
		/// <b>true</b> if the link is a directory; Otherwise <b>false</b>, the link is some type of
		/// a file.
		/// </value>
		[DataMember]
		public bool IsDirectory
		{
			get
			{
				if (!isDirectory.HasValue)
				{
					GetInfo().Wait();
				}

				return isDirectory.Value;
			}
			set => isDirectory = value;
		}

		/// <inheritdoc />
		[DataMember]
		public IEnumerable<IFileSystemLink> Links
		{
			get
			{
				if (links is null)
				{
					GetInfo().Wait();
				}

				return links;
			}
			set => links = value;
		}

		/// <summary>
		/// The file name of the IPFS node.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Size of the file contents.
		/// </summary>
		/// <value>This is the size of the file not the raw encoded contents of the block.</value>
		[DataMember]
		public long Size
		{
			get
			{
				if (!size.HasValue)
				{
					GetInfo().Wait();
				}

				return size.Value;
			}
			set => size = value;
		}

		/// <inheritdoc />
		public IFileSystemLink ToLink(string name = "")
		{
			var link = new FileSystemLink
			{
				Name = string.IsNullOrWhiteSpace(name) ? Name : name,
				Id = Id,
				Size = Size,
			};
			return link;
		}

		private async Task GetInfo()
		{
			var node = await IpfsClient.FileSystem.ListFileAsync(Id);
			this.IsDirectory = node.IsDirectory;
			this.Links = node.Links;
			this.Size = node.Size;
		}
	}
}
