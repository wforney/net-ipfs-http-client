namespace Ipfs.Http.Client.Tests;

using Microsoft.Extensions.DependencyInjection;
using System;

public static class TestFixture
{
    /// <summary>
    /// Fiddler cannot see localhost traffic because .Net bypasses the network stack for localhost/127.0.0.1.
    /// By using "127.0.0.1." (note trailing dot) fiddler will receive the traffic and if its not running
    /// the localhost will get it!
    /// </summary>
    private static readonly Uri defaultApiUri = new("http://127.0.0.1:5001");

    /// <summary>
    /// The service provider
    /// </summary>
    private static IServiceProvider serviceProvider;

    /// <summary>
    /// Gets the ipfs context.
    /// </summary>
    /// <value>The ipfs context.</value>
    public static IpfsContext IpfsContext { get; } = GetServiceProvider().GetRequiredService<IpfsContext>();

    /// <summary>
    /// The IPFS client
    /// </summary>
    public static IIpfsClient Ipfs { get; } = GetServiceProvider().GetRequiredService<IIpfsClient>();

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    /// <returns>IServiceProvider.</returns>
    public static IServiceProvider GetServiceProvider() => serviceProvider ??= new ServiceCollection().AddIpfsClient(defaultApiUri).BuildServiceProvider();
}
