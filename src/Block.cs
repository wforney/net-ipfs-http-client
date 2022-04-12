namespace Ipfs.Http.Client;

using System.IO;
using System.Runtime.Serialization;

/// <inheritdoc />
[DataContract]
[Serializable]
public class Block : IDataBlock
{
    private long? size;

    /// <inheritdoc />
    [DataMember]
    public byte[] DataBytes { get; set; } = Array.Empty<byte>();

    /// <inheritdoc />
    public Stream DataStream => new MemoryStream(this.DataBytes, false);

    /// <inheritdoc />
    [DataMember]
    public Cid Id { get; set; } = new Cid(); // TODO: Generate this on the fly based on content?

    /// <inheritdoc />
    [DataMember]
    public long Size
    {
        get => this.size ?? this.DataBytes.Length; // TODO: Why the backing variable here? Shouldn't this always be computed?
        set => this.size = value;
    }
}
