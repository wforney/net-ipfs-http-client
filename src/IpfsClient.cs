using Common.Logging;
using System;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using Ipfs.CoreApi;

namespace Ipfs.Http
{
   /// <summary>
   ///   A client that allows access to the InterPlanetary File System (IPFS).
   /// </summary>
   /// <remarks>
   ///   The API is based on the <see href="https://ipfs.io/docs/commands/">IPFS commands</see>.
   /// </remarks>
   /// <seealso href="https://ipfs.io/docs/api/">IPFS API</seealso>
   /// <seealso href="https://ipfs.io/docs/commands/">IPFS commands</seealso>
   /// <remarks>
   ///   <b>IpfsClient</b> is thread safe, only one instance is required
   ///   by the application.
   /// </remarks>
   public partial class IpfsClient : ICoreApi
   {
		#region Private

		private const string UNKNOWN_FILENAME = "unknown";
      private static readonly ILog _log = LogManager.GetLogger( typeof( IpfsClient ) );
      private static readonly object _lockObject = new object();
      private static HttpClient _api = null;

      /// <summary>
      ///   Throws an <see cref="HttpRequestException"/> if the response
      ///   does not indicate success.
      /// </summary>
      /// <param name="response"></param>
      /// <returns>
      ///    <b>true</b>
      /// </returns>
      /// <remarks>
      ///   The API server returns an JSON error in the form <c>{ "Message": "...", "Code": ... }</c>.
      /// </remarks>
      private async Task<bool> ThrowOnErrorAsync( HttpResponseMessage response )
      {
         if( response.IsSuccessStatusCode )
            return true;
         if( response.StatusCode == HttpStatusCode.NotFound )
         {
            var error = "Invalid IPFS command: " + response.RequestMessage.RequestUri.ToString();
            if( _log.IsDebugEnabled )
               _log.Debug( "ERR " + error );
            throw new HttpRequestException( error );
         }

         var body = await response.Content.ReadAsStringAsync();
         if( _log.IsDebugEnabled )
            _log.Debug( "ERR " + body );
         string message = body;
         try
         {
            var res = JsonConvert.DeserializeObject<dynamic>( body );
            message = (string)res.Message;
         }
         catch { }
         throw new HttpRequestException( message );
      }

      private static string CreateCommand( string command, string arg, string[] options )
      {
         var url = "/api/v0/" + command;
         var q = new StringBuilder();
         if( arg != null )
         {
            q.Append( "&arg=" );
            q.Append( WebUtility.UrlEncode( arg ) );
         }

         foreach( var option in options )
         {
            q.Append( '&' );
            var i = option.IndexOf( '=' );
            if( i < 0 )
            {
               q.Append( option );
            }
            else
            {
               q.Append( option.Substring( 0, i ) );
               q.Append( '=' );
               q.Append( WebUtility.UrlEncode( option[( i + 1 )..] ) );
            }
         }

         if( q.Length > 0 )
         {
            q[0] = '?';
            q.Insert( 0, url );
            url = q.ToString();
         }

         return url;
      }

      private Uri BuildCommand(
         string command,
         string arg = null,
         params string[] options )
         => new Uri( ApiUri, CreateCommand( command, arg, options ) );

      private static MultipartFormDataContent GetMultipartFormDataContent( 
         Stream data, string name = UNKNOWN_FILENAME )
      {
         var content = new MultipartFormDataContent();
         var streamContent = new StreamContent( data );
         streamContent.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );
         content.Add( streamContent, "file", name );
         return content;
      }

      private static MultipartFormDataContent GetMultipartFormDataContent(
         byte[] data, string name = UNKNOWN_FILENAME )
      {
         var content = new MultipartFormDataContent();
         var streamContent = new ByteArrayContent( data );
         streamContent.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );
         content.Add( streamContent, "file", name );
         return content;
      }

      /// <summary>
      ///  Get the IPFS API.
      /// </summary>
      /// <returns>
      ///   A <see cref="HttpClient"/>.
      /// </returns>
      /// <remarks>
      ///   Only one client is needed.  Its thread safe.
      /// </remarks>
      private HttpClient GetApiClient()
      {
         if( _api != null ) return _api;
         lock( _lockObject )
         {
            if( _api != null ) return _api;
            var handler = new HttpClientHandler();
            if( handler.SupportsAutomaticDecompression )
               handler.AutomaticDecompression = DecompressionMethods.GZip
                   | DecompressionMethods.Deflate;

            _api = new HttpClient( handler )
            {
               Timeout = Timeout.InfiniteTimeSpan
            };
            _api.DefaultRequestHeaders.Add( "User-Agent", UserAgent );
         }

         return _api;
      }

		#endregion

		#region Constructor

		/// <summary>
		///   Creates a new instance of the <see cref="IpfsClient"/> class and sets the
		///   default values.
		/// </summary>
		/// <param name="host">
		///   The URL of the API host. For example "http://localhost:5001" or "http://ipv4.fiddler:5001".
		/// </param>
		/// <remarks>
		///   All methods of IpfsClient are thread safe.  Typically, only one instance is required for
		///   an application.
		/// </remarks>
		public IpfsClient( string host = "http://localhost:5001" )
      {
         ApiUri = new Uri( host );

         var version = typeof( IpfsClient ).GetTypeInfo().Assembly.GetName().Version;
         UserAgent = string.Format( "net-ipfs/{0}.{1}", version.Major, version.Minor );
         TrustedPeers = new TrustedPeerCollection( this );

         Bootstrap = new BootstrapApi( this );
         Bitswap = new BitswapApi( this );
         Block = new BlockApi( this );
         BlockRepository = new BlockRepositoryApi( this );
         Config = new ConfigApi( this );
         Pin = new PinApi( this );
         Dht = new DhtApi( this );
         Swarm = new SwarmApi( this );
         Dag = new DagApi( this );
         Object = new ObjectApi( this );
         FileSystem = new FileSystemApi( this );
         PubSub = new PubSubApi( this );
         Key = new KeyApi( this );
         Generic = this;
         Name = new NameApi( this );
         Dns = new DnsApi( this );
         Stats = new StatApi( this );
      }

      #endregion

      #region Properties

      /// <summary>
      ///   The URL to the IPFS API server.  The default is "http://localhost:5001".
      /// </summary>
      public Uri ApiUri { get; set; }

      /// <summary>
      ///   The value of HTTP User-Agent header sent to the API server. 
      /// </summary>
      /// <value>
      ///   The default value is "net-ipfs/M.N", where M is the major and N is minor version
      ///   numbers of the assembly.
      /// </value>
      public string UserAgent { get; set; }

      /// <summary>
      ///   The list of peers that are initially trusted by IPFS.
      /// </summary>
      /// <remarks>
      ///   This is equilivent to <c>ipfs bootstrap list</c>.
      /// </remarks>
      public TrustedPeerCollection TrustedPeers { get; private set; }

      /// <inheritdoc />
      public IBitswapApi Bitswap { get; private set; }

      /// <inheritdoc />
      public IBootstrapApi Bootstrap { get; private set; }

      /// <inheritdoc />
      public IGenericApi Generic { get; private set; }

      /// <inheritdoc />
      public IDnsApi Dns { get; private set; }

      /// <inheritdoc />
      public IStatsApi Stats { get; private set; }

      /// <inheritdoc />
      public INameApi Name { get; private set; }

      /// <inheritdoc />
      public IBlockApi Block { get; private set; }

      /// <inheritdoc />
      public IBlockRepositoryApi BlockRepository { get; private set; }

      /// <inheritdoc />
      public IConfigApi Config { get; private set; }

      /// <inheritdoc />
      public IPinApi Pin { get; private set; }

      /// <inheritdoc />
      public IDagApi Dag { get; private set; }

      /// <inheritdoc />
      public IDhtApi Dht { get; private set; }

      /// <inheritdoc />
      public ISwarmApi Swarm { get; private set; }

      /// <inheritdoc />
      public IObjectApi Object { get; private set; }

      /// <inheritdoc />
      public IFileSystemApi FileSystem { get; private set; }

      /// <inheritdoc />
      public IPubSubApi PubSub { get; private set; }

      /// <inheritdoc />
      public IKeyApi Key { get; private set; }

      #endregion

      #region Internal

      internal async Task DoCommandAsync( Uri url, CancellationToken cancel )
      {
         if( _log.IsDebugEnabled )
            _log.Debug( "POST " + url.ToString() );
         using var response = await GetApiClient().PostAsync( url, null, cancel );
         await ThrowOnErrorAsync( response );
         var body = await response.Content.ReadAsStringAsync();
         if( _log.IsDebugEnabled )
            _log.Debug( "RSP " + body );
         return;
      }

      /// <summary>
      ///   Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> that
      ///   requires uploading of a "file".
      /// </summary>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0add">"add"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="data">
      ///   A <see cref="Stream"/> containing the data to upload.
      /// </param>
      /// <param name="name">
      ///   The name associated with the <paramref name="data"/>, can be <b>null</b>.
      ///   Typically a filename, such as "hello.txt".
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A task that represents the asynchronous operation. The task's value is 
      ///   the HTTP response as a string.
      /// </returns>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      internal async Task<string> UploadAsync(
         string command,
         CancellationToken cancel,
         Stream data,
         string name,
         params string[] options )
      {
         var content = GetMultipartFormDataContent( data, name );
         var url = BuildCommand( command, null, options );
         if( _log.IsDebugEnabled )
            _log.Debug( "POST " + url.ToString() );
         using var response = await GetApiClient().PostAsync( url, content, cancel );
         await ThrowOnErrorAsync( response );
         var json = await response.Content.ReadAsStringAsync();
         if( _log.IsDebugEnabled )
            _log.Debug( "RSP " + json );
         return json;
      }

      /// <summary>
      ///   Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> that
      ///   requires uploading of a "file".
      /// </summary>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0add">"add"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="data">
      ///   A <see cref="Stream"/> containing the data to upload.
      /// </param>
      /// <param name="name">
      ///   The name associated with the <paramref name="data"/>, can be <b>null</b>.
      ///   Typically a filename, such as "hello.txt".
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A task that represents the asynchronous operation. The task's value is 
      ///   the HTTP response as a <see cref="Stream"/>.
      /// </returns>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      internal async Task<Stream> Upload2Async(
         string command,
         CancellationToken cancel,
         Stream data,
         string name,
         params string[] options )
		{
			var content = GetMultipartFormDataContent( data, name );
			var url = BuildCommand( command, null, options );
			if( _log.IsDebugEnabled )
				_log.Debug( "POST " + url.ToString() );
			var response = await GetApiClient().PostAsync( url, content, cancel );
			await ThrowOnErrorAsync( response );
			return await response.Content.ReadAsStreamAsync();
		}

		/// <summary>
		///  TODO
		/// </summary>
		internal async Task<string> UploadAsync(
         string command,
         CancellationToken cancel,
         byte[] data,
         params string[] options )
      {
         var content = GetMultipartFormDataContent( data );
         var url = BuildCommand( command, null, options );
         if( _log.IsDebugEnabled )
            _log.Debug( "POST " + url.ToString() );
         using var response = await GetApiClient().PostAsync( url, content, cancel );
         await ThrowOnErrorAsync( response );
         var json = await response.Content.ReadAsStringAsync();
         if( _log.IsDebugEnabled )
            _log.Debug( "RSP " + json );
         return json;
      }

      /// <summary>
      ///  Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a string.
      /// </summary>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="arg">
      ///   The optional argument to the command.
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A string representation of the command's result.
      /// </returns>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      internal async Task<string> DoCommandAsync( string command, CancellationToken cancel, string arg = null, params string[] options )
      {
         var url = BuildCommand( command, arg, options );
         if( _log.IsDebugEnabled )
            _log.Debug( "POST " + url.ToString() );
         using var response = await GetApiClient().PostAsync( url, null, cancel );
         await ThrowOnErrorAsync( response );
         var body = await response.Content.ReadAsStringAsync();
         if( _log.IsDebugEnabled )
            _log.Debug( "RSP " + body );
         return body;
      }

      /// <summary>
      ///   Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning 
      ///   a specific <see cref="Type"/>.
      /// </summary>
      /// <typeparam name="T">
      ///   The <see cref="Type"/> of object to return.
      /// </typeparam>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="arg">
      ///   The optional argument to the command.
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A <typeparamref name="T"/>.
      /// </returns>
      /// <remarks>
      ///   The command's response is converted to <typeparamref name="T"/> using
      ///   <c>JsonConvert</c>.
      /// </remarks>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      public async Task<T> DoCommandAsync<T>( string command, CancellationToken cancel, string arg = null, params string[] options )
      {
         var json = await DoCommandAsync( command, cancel, arg, options );
         return JsonConvert.DeserializeObject<T>( json );
      }

      /// <summary>
      ///  Post an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a stream.
      /// </summary>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="arg">
      ///   The optional argument to the command.
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A <see cref="Stream"/> containing the command's result.
      /// </returns>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      internal async Task<Stream> PostDownloadAsync( string command, CancellationToken cancel, string arg = null, params string[] options )
      {
         var url = BuildCommand( command, arg, options );
         if( _log.IsDebugEnabled )
            _log.Debug( "POST " + url.ToString() );
         var request = new HttpRequestMessage( HttpMethod.Post, url );

         var response = await GetApiClient().SendAsync( request, HttpCompletionOption.ResponseHeadersRead, cancel );
         await ThrowOnErrorAsync( response );
         return await response.Content.ReadAsStreamAsync();
      }

      /// <summary>
      ///  Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a
      ///  <see cref="Stream"/>.
      /// </summary>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="arg">
      ///   The optional argument to the command.
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A <see cref="Stream"/> containing the command's result.
      /// </returns>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      internal async Task<Stream> DownloadAsync( 
         string command, 
         CancellationToken cancel, 
         string arg = null, 
         params string[] options )
      {
         var url = BuildCommand( command, arg, options );
         if( _log.IsDebugEnabled )
            _log.Debug( "GET " + url.ToString() );
         var response = await GetApiClient().GetAsync( url, HttpCompletionOption.ResponseHeadersRead, cancel );
         await ThrowOnErrorAsync( response );
         return await response.Content.ReadAsStreamAsync();
      }

      /// <summary>
      ///  Perform an <see href="https://ipfs.io/docs/api/">IPFS API command</see> returning a
      ///  a byte array.
      /// </summary>
      /// <param name="command">
      ///   The <see href="https://ipfs.io/docs/api/">IPFS API command</see>, such as
      ///   <see href="https://ipfs.io/docs/api/#apiv0filels">"file/ls"</see>.
      /// </param>
      /// <param name="cancel">
      ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
      /// </param>
      /// <param name="arg">
      ///   The optional argument to the command.
      /// </param>
      /// <param name="options">
      ///   The optional flags to the command.
      /// </param>
      /// <returns>
      ///   A byte array containing the command's result.
      /// </returns>
      /// <exception cref="HttpRequestException">
      ///   When the IPFS server indicates an error.
      /// </exception>
      internal async Task<byte[]> DownloadBytesAsync( 
         string command, 
         CancellationToken cancel, 
         string arg = null, 
         params string[] options )
      {
         var url = BuildCommand( command, arg, options );
         if( _log.IsDebugEnabled )
            _log.Debug( "GET " + url.ToString() );
         var response = await GetApiClient().GetAsync( url, HttpCompletionOption.ResponseHeadersRead, cancel );
         await ThrowOnErrorAsync( response );
         return await response.Content.ReadAsByteArrayAsync();
      }

      #endregion

   }
}
