#region License

/*
 * HttpListenerRequest.cs
 *
 * This code is derived from HttpListenerRequest.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2015 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

#endregion License

#region Authors

/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */

#endregion Authors

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WebSocketSharp.Net
{
    /// <summary>
    /// Provides the access to a request to the <see cref="HttpListener"/>.
    /// </summary>
    /// <remarks>
    /// The HttpListenerRequest class cannot be inherited.
    /// </remarks>
    public sealed class HttpListenerRequest
    {
        #region Private Fields

        private static readonly byte[] _100continue;
        private string[] _acceptTypes;
        private bool _chunked;
        private HttpConnection _connection;
        private Encoding _contentEncoding;
        private long _contentLength;
        private HttpListenerContext _context;
        private CookieCollection _cookies;
        private WebHeaderCollection _headers;
        private Guid _identifier;
        private Stream _inputStream;
        private string _method;
        private NameValueCollection _queryString;
        private Uri _referer;
        private string _uri;
        private Uri _url;
        private string[] _userLanguages;
        private Version _version;
        private bool _websocketRequest;
        private bool _websocketRequestSet;

        #endregion Private Fields

        #region Static Constructor

        static HttpListenerRequest()
        {
            _100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
        }

        #endregion Static Constructor

        #region Internal Constructors

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;

            _connection = context.Connection;
            _contentLength = -1;
            _headers = new WebHeaderCollection();
            _identifier = Guid.NewGuid();
        }

        #endregion Internal Constructors

        #region Public Properties

        /// <summary>
        /// Gets the media types which are acceptable for the response.
        /// </summary>
        /// <value>
        /// An array of <see cref="string"/> that contains the media type names in
        /// the Accept request-header, or <see langword="null"/> if the request didn't include
        /// the Accept header.
        /// </value>
        public string[] AcceptTypes
        {
            get
            {
                return _acceptTypes;
            }
        }

        /// <summary>
        /// Gets an error code that identifies a problem with the client's certificate.
        /// </summary>
        /// <value>
        /// Always returns <c>0</c>.
        /// </value>
        public int ClientCertificateError
        {
            get
            {
                return 0; // TODO: Always returns 0.
            }
        }

        /// <summary>
        /// Gets the encoding for the entity body data included in the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Encoding"/> from the charset value of the Content-Type
        ///   header.
        ///   </para>
        ///   <para>
        ///   <see cref="Encoding.UTF8"/> if the charset value is not available.
        ///   </para>
        /// </value>
        public Encoding ContentEncoding
        {
            get
            {
                return _contentEncoding ?? Encoding.UTF8;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the entity body data included in
        /// the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="long"/> from the value of the Content-Length header.
        ///   </para>
        ///   <para>
        ///   -1 if the value is not known.
        ///   </para>
        /// </value>
        public long ContentLength64
        {
            get
            {
                return _contentLength;
            }
        }

        /// <summary>
        /// Gets the media type of the entity body data included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> from the value of the Content-Type header.
        /// </value>
        public string ContentType
        {
            get
            {
                return _headers["Content-Type"];
            }
        }

        /// <summary>
        /// Gets the cookies included in the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="CookieCollection"/> that contains the cookies.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = _headers.GetCookies(false);

                return _cookies;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the request has the entity body data.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request has the entity body data; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool HasEntityBody
        {
            get
            {
                return _contentLength > 0 || _chunked;
            }
        }

        /// <summary>
        /// Gets the HTTP headers used in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the HTTP headers used in the request.
        /// </value>
        public NameValueCollection Headers
        {
            get
            {
                return _headers;
            }
        }

        /// <summary>
        /// Gets the HTTP method used in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the HTTP method used in the request.
        /// </value>
        public string HttpMethod
        {
            get
            {
                return _method;
            }
        }

        /// <summary>
        /// Gets a stream that contains the entity body data included in
        /// the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Stream"/> that contains the entity body data.
        ///   </para>
        ///   <para>
        ///   <see cref="Stream.Null"/> if no entity body data is included.
        ///   </para>
        /// </value>
        public Stream InputStream
        {
            get
            {
                if (!HasEntityBody)
                    return Stream.Null;

                if (_inputStream == null)
                {
                    _inputStream = _connection.GetRequestStream(
                                     _contentLength, _chunked
                                   );
                }

                return _inputStream;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the client that sent the request is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public bool IsAuthenticated
        {
            get
            {
                return _context.User != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the request is sent from the local
        /// computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is sent from the same computer as the server;
        /// otherwise, <c>false</c>.
        /// </value>
        public bool IsLocal
        {
            get
            {
                return _connection.IsLocal;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a secure connection is used to send
        /// the request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is a secure connection; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool IsSecureConnection
        {
            get
            {
                return _connection.IsSecure;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake
        /// request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool IsWebSocketRequest
        {
            get
            {
                if (!_websocketRequestSet)
                {
                    _websocketRequest = _method == "GET"
                                        && _version > HttpVersion.Version10
                                        && _headers.Upgrades("websocket");

                    _websocketRequestSet = true;
                }

                return _websocketRequest;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a persistent connection is requested.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request specifies that the connection is kept open;
        /// otherwise, <c>false</c>.
        /// </value>
        public bool KeepAlive
        {
            get
            {
                return _headers.KeepsAlive(_version);
            }
        }

        /// <summary>
        /// Gets the endpoint to which the request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server IP
        /// address and port number.
        /// </value>
        public System.Net.IPEndPoint LocalEndPoint
        {
            get
            {
                return _connection.LocalEndPoint;
            }
        }

        /// <summary>
        /// Gets the HTTP version used in the request.
        /// </summary>
        /// <value>
        /// A <see cref="Version"/> that represents the HTTP version used in the request.
        /// </value>
        public Version ProtocolVersion
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// Gets the query string included in the request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="NameValueCollection"/> that contains the query
        ///   parameters.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        public NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    _queryString = HttpUtility.InternalParseQueryString(
                                     _url.Query, Encoding.UTF8
                                   );
                }

                return _queryString;
            }
        }

        /// <summary>
        /// Gets the raw URL (without the scheme, host, and port) requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the raw URL requested by the client.
        /// </value>
        public string RawUrl
        {
            get
            {
                return _url.PathAndQuery; // TODO: Should decode?
            }
        }

        /// <summary>
        /// Gets the endpoint from which the request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client IP
        /// address and port number.
        /// </value>
        public System.Net.IPEndPoint RemoteEndPoint
        {
            get
            {
                return _connection.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Gets the request identifier of a incoming HTTP request.
        /// </summary>
        /// <value>
        /// A <see cref="Guid"/> that represents the identifier of a request.
        /// </value>
        public Guid RequestTraceIdentifier
        {
            get
            {
                return _identifier;
            }
        }

        /// <summary>
        /// Gets the URL requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the URL requested by the client.
        /// </value>
        public Uri Url
        {
            get
            {
                return _url;
            }
        }

        /// <summary>
        /// Gets the URL of the resource from which the requested URL was obtained.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the value of the Referer request-header,
        /// or <see langword="null"/> if the request didn't include an Referer header.
        /// </value>
        public Uri UrlReferrer
        {
            get
            {
                return _referer;
            }
        }

        /// <summary>
        /// Gets the information about the user agent originating the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the User-Agent request-header.
        /// </value>
        public string UserAgent
        {
            get
            {
                return _headers["User-Agent"];
            }
        }

        /// <summary>
        /// Gets the IP address and port number to which the request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the server IP address and port
        /// number.
        /// </value>
        public string UserHostAddress
        {
            get
            {
                return _connection.LocalEndPoint.ToString();
            }
        }

        /// <summary>
        /// Gets the internet host name and port number (if present) specified by the client.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Host request-header.
        /// </value>
        public string UserHostName
        {
            get
            {
                return _headers["Host"];
            }
        }

        /// <summary>
        /// Gets the natural languages which are preferred for the response.
        /// </summary>
        /// <value>
        /// An array of <see cref="string"/> that contains the natural language names in
        /// the Accept-Language request-header, or <see langword="null"/> if the request
        /// didn't include an Accept-Language header.
        /// </value>
        public string[] UserLanguages
        {
            get
            {
                return _userLanguages;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        internal void AddHeader(string headerField)
        {
            var colon = headerField.IndexOf(':');
            if (colon < 1)
            {
                _context.ErrorMessage = "Invalid header field";
                return;
            }

            var name = headerField.Substring(0, colon).Trim();
            if (name.Length == 0 || !name.IsToken())
            {
                _context.ErrorMessage = "Invalid header name";
                return;
            }

            var val = colon < headerField.Length - 1
                      ? headerField.Substring(colon + 1).Trim()
                      : String.Empty;

            _headers.InternalSet(name, val, false);

            var lower = name.ToLower(CultureInfo.InvariantCulture);
            if (lower == "accept")
            {
                _acceptTypes = val.SplitHeaderValue(',').ToList().ToArray();
                return;
            }

            if (lower == "accept-language")
            {
                _userLanguages = val.Split(',');
                return;
            }

            if (lower == "content-length")
            {
                long len;
                if (!Int64.TryParse(val, out len))
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                    return;
                }

                if (len < 0)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";
                    return;
                }

                _contentLength = len;
                return;
            }

            if (lower == "content-type")
            {
                try
                {
                    _contentEncoding = HttpUtility.GetEncoding(val);
                }
                catch
                {
                    _context.ErrorMessage = "Invalid Content-Type header";
                }

                return;
            }

            if (lower == "referer")
            {
                var referer = val.ToUri();
                if (referer == null)
                {
                    _context.ErrorMessage = "Invalid Referer header";
                    return;
                }

                _referer = referer;
                return;
            }
        }

        internal void FinishInitialization()
        {
            var host = _headers["Host"];
            var hasHost = host != null && host.Length > 0;
            if (_version > HttpVersion.Version10 && !hasHost)
            {
                _context.ErrorMessage = "Invalid Host header";
                return;
            }

            _url = HttpUtility.CreateRequestUrl(
                     _uri,
                     hasHost ? host : UserHostAddress,
                     IsWebSocketRequest,
                     IsSecureConnection
                   );

            if (_url == null)
            {
                _context.ErrorMessage = "Invalid request url";
                return;
            }

            var transferEnc = _headers["Transfer-Encoding"];
            if (transferEnc != null)
            {
                if (_version < HttpVersion.Version11)
                {
                    _context.ErrorMessage = "Invalid Transfer-Encoding header";
                    return;
                }

                var comparison = StringComparison.OrdinalIgnoreCase;
                if (!transferEnc.Equals("chunked", comparison))
                {
                    _context.ErrorMessage = String.Empty;
                    _context.ErrorStatus = 501;

                    return;
                }

                _chunked = true;
            }

            if (_contentLength == -1 && !_chunked)
            {
                if (_method == "POST" || _method == "PUT")
                {
                    _context.ErrorMessage = String.Empty;
                    _context.ErrorStatus = 411;

                    return;
                }
            }

            var expect = _headers["Expect"];
            if (_version > HttpVersion.Version10 && expect != null)
            {
                var comparison = StringComparison.OrdinalIgnoreCase;
                if (!expect.Equals("100-continue", comparison))
                {
                    _context.ErrorMessage = "Invalid Expect header";
                    return;
                }

                var output = _connection.GetResponseStream();
                output.InternalWrite(_100continue, 0, _100continue.Length);
            }
        }

        internal bool FlushInput()
        {
            if (!HasEntityBody)
                return true;

            var len = 2048;
            if (_contentLength > 0 && _contentLength < len)
                len = (int)_contentLength;

            var buff = new byte[len];

            while (true)
            {
                try
                {
                    var ares = InputStream.BeginRead(buff, 0, len, null, null);
                    if (!ares.IsCompleted)
                    {
                        var timeout = 100;
                        if (!ares.AsyncWaitHandle.WaitOne(timeout))
                            return false;
                    }

                    if (InputStream.EndRead(ares) <= 0)
                        return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal bool IsUpgradeRequest(string protocol)
        {
            return _headers.Upgrades(protocol);
        }

        internal void SetRequestLine(string requestLine)
        {
            var parts = requestLine.Split(new[] { ' ' }, 3);
            if (parts.Length < 3)
            {
                _context.ErrorMessage = "Invalid request line (parts)";
                return;
            }

            var method = parts[0];
            if (method.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (method)";
                return;
            }

            if (!method.IsToken())
            {
                _context.ErrorMessage = "Invalid request line (method)";
                return;
            }

            var uri = parts[1];
            if (uri.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (uri)";
                return;
            }

            var rawVer = parts[2];
            if (rawVer.Length != 8)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            if (rawVer.IndexOf("HTTP/") != 0)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            Version ver;
            if (!rawVer.Substring(5).TryCreateVersion(out ver))
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            if (ver.Major < 1)
            {
                _context.ErrorMessage = "Invalid request line (version)";
                return;
            }

            _method = method;
            _uri = uri;
            _version = ver;
        }

        #endregion Internal Methods

        #region Public Methods

        /// <summary>
        /// Begins getting the client's X.509 v.3 certificate asynchronously.
        /// </summary>
        /// <remarks>
        /// This asynchronous operation must be completed by calling
        /// the <see cref="EndGetClientCertificate"/> method. Typically,
        /// that method is invoked by the <paramref name="requestCallback"/> delegate.
        /// </remarks>
        /// <returns>
        /// An <see cref="IAsyncResult"/> that contains the status of the asynchronous operation.
        /// </returns>
        /// <param name="requestCallback">
        /// An <see cref="AsyncCallback"/> delegate that references the method(s) called when
        /// the asynchronous operation completes.
        /// </param>
        /// <param name="state">
        /// An <see cref="object"/> that contains a user defined object to pass to
        /// the <paramref name="requestCallback"/> delegate.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// This method isn't implemented.
        /// </exception>
        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            // TODO: Not implemented.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ends an asynchronous operation to get the client's X.509 v.3 certificate.
        /// </summary>
        /// <remarks>
        /// This method completes an asynchronous operation started by calling
        /// the <see cref="BeginGetClientCertificate"/> method.
        /// </remarks>
        /// <returns>
        /// A <see cref="X509Certificate2"/> that contains the client's X.509 v.3 certificate.
        /// </returns>
        /// <param name="asyncResult">
        /// An <see cref="IAsyncResult"/> obtained by calling
        /// the <see cref="BeginGetClientCertificate"/> method.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// This method isn't implemented.
        /// </exception>
        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            // TODO: Not implemented.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the client's X.509 v.3 certificate.
        /// </summary>
        /// <returns>
        /// A <see cref="X509Certificate2"/> that contains the client's X.509 v.3 certificate.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// This method isn't implemented.
        /// </exception>
        public X509Certificate2 GetClientCertificate()
        {
            // TODO: Not implemented.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents
        /// the current <see cref="HttpListenerRequest"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current <see cref="HttpListenerRequest"/>.
        /// </returns>
        public override string ToString()
        {
            var buff = new StringBuilder(64);
            buff.AppendFormat("{0} {1} HTTP/{2}\r\n", _method, _uri, _version);
            buff.Append(_headers.ToString());

            return buff.ToString();
        }

        #endregion Public Methods
    }
}