﻿namespace Ipfs.Http.Client;

using Ipfs.Http.Client.CoreApi;
using Multiformats.Base;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Text;

/// <summary>
///   A published message.
/// </summary>
/// <remarks>
///   The <see cref="PubSubApi"/> is used to publish and subsribe to a message.
/// </remarks>
[DataContract]
public class PublishedMessage : IPublishedMessage
{
    /// <summary>
    ///   Creates a new instance of <see cref="PublishedMessage"/> from the
    ///   specified JSON string.
    /// </summary>
    /// <param name="json">
    ///   The JSON representation of a published message.
    /// </param>
    public PublishedMessage(string json)
    {
        var o = JObject.Parse(json);
        if (o is null)
        {
            return;
        }

        this.Sender = (string?)o["from"];
        this.SequenceNumber = Multibase.Decode((string?)o["seqno"], out MultibaseEncoding _);
        this.DataBytes = Multibase.Decode((string?)o["data"], out MultibaseEncoding _);

        var topics = (JArray?)o["topicIDs"];
        this.Topics = topics?.Select(t => Encoding.UTF8.GetString(Multibase.Decode((string?)t, out MultibaseEncoding _)));
    }

    /// <inheritdoc />
    [DataMember]
    public byte[]? DataBytes { get; private set; }

    /// <inheritdoc />
    public Stream? DataStream => this.DataBytes is null ? null : new MemoryStream(this.DataBytes, false);

    /// <summary>
    ///   Contents as a string.
    /// </summary>
    /// <value>
    ///   The contents interpreted as a UTF-8 string.
    /// </value>
    public string? DataString => this.DataBytes is null ? null : Encoding.UTF8.GetString(this.DataBytes);

    /// <summary>>
    ///   NOT SUPPORTED.
    /// </summary>
    /// <exception cref="NotSupportedException">
    ///   A published message does not have a content id.
    /// </exception>
    public Cid Id => throw new NotSupportedException();

    /// <inheritdoc />
    [DataMember]
    public Peer? Sender { get; private set; }

    /// <inheritdoc />
    [DataMember]
    public byte[]? SequenceNumber { get; private set; }

    /// <inheritdoc />
    [DataMember]
    public long Size => this.DataBytes?.Length ?? 0L;

    /// <inheritdoc />
    [DataMember]
    public IEnumerable<string?>? Topics { get; private set; }
}
