namespace Ipfs.Http;

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
  public Cid Id { get; set; }

  /// <inheritdoc />
  [DataMember]
  public byte[] DataBytes { get; set; }

  /// <inheritdoc />
  public Stream DataStream
   => new MemoryStream( DataBytes, false );

  /// <inheritdoc />
  [DataMember]
  public long Size
  {
     get => size ?? DataBytes.Length;
     set => size = value;
  }
}

