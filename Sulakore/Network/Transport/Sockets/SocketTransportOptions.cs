// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;

using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Transport.Sockets;

/// <summary>
/// Options for socket based transports.
/// </summary>
public sealed class SocketTransportOptions
{
    /// <summary>
    /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

    /// <summary>
    /// Set to false to enable Nagle's algorithm for all connections.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    /// The maximum length of the pending connection queue.
    /// </summary>
    /// <remarks>
    /// Defaults to 512.
    /// </remarks>
    public int Backlog { get; set; } = 512;

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
    /// </summary>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
    /// </summary>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Inline application and transport continuations instead of dispatching to the threadpool.
    /// </summary>
    /// <remarks>
    /// This will run application code on the IO thread which is why this is unsafe.
    /// It is recommended to set the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS environment variable to '1' when using this setting to also inline the completions
    /// at the runtime layer as well.
    /// This setting can make performance worse if there is expensive work that will end up holding onto the IO thread for longer than needed.
    /// Test to make sure this setting helps performance.
    /// </remarks>
    public bool UnsafePreferInlineScheduling { get; set; }

    /// <summary>
    /// A function used to create a new <see cref="Socket"/> to listen with. If
    /// not set, <see cref="CreateDefaultBoundListenSocket" /> is used.
    /// </summary>
    /// <remarks>
    /// Implementors are expected to call <see cref="Socket.Bind"/> on the
    /// <see cref="Socket"/>. Please note that <see cref="CreateDefaultBoundListenSocket"/>
    /// calls <see cref="Socket.Bind"/> as part of its implementation, so implementors
    /// using this method do not need to call it again.
    /// </remarks>
    public Func<EndPoint, Socket> CreateBoundListenSocket { get; set; } = CreateDefaultBoundListenSocket;

    /// <summary>
    /// Creates a default instance of <see cref="Socket"/> for the given <see cref="EndPoint"/>
    /// that can be used by a connection listener to listen for inbound requests. <see cref="Socket.Bind"/>
    /// is called by this method.
    /// </summary>
    /// <param name="endpoint">
    /// An <see cref="EndPoint"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Socket"/> instance.
    /// </returns>
    public static Socket CreateDefaultBoundListenSocket(EndPoint endpoint)
    {
        Socket listenSocket = endpoint switch
        {
            // We're passing "ownsHandle: false" to avoid side-effects on the
            // handle when disposing the socket.
            //
            // When the non-owning SafeSocketHandle gets disposed (on .NET 7+),
            // on-going async operations are aborted.
            FileHandleEndPoint fileHandle => new Socket(new SafeSocketHandle((nint)fileHandle.FileHandle, ownsHandle: false)),
            UnixDomainSocketEndPoint unix => new Socket(unix.AddressFamily, SocketType.Stream, ProtocolType.Unspecified),
            IPEndPoint ip => new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp),

            _ => new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
        };

        // we only call Bind on sockets that were _not_ created
        // using a file handle; the handle is already bound
        // to an underlying socket so doing it again causes the
        // underlying PAL call to throw
        if (endpoint is not FileHandleEndPoint)
        {
            listenSocket.Bind(endpoint);
        }
        return listenSocket;
    }
}