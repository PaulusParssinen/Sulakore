using System.IO.Pipelines;
using System.Net.WebSockets;

namespace Sulakore.Network.Transport.WebSockets;

// Built directly on top of the SocketConnection. 
internal class WebSocketTransport
{
    private IDuplexPipe _innerTransport;
    private IDuplexPipe _application;

    public WebSocket WebSocket { get; }

    public WebSocketTransport(IDuplexPipe transport, WebSocketCreationOptions options)
    {
        WebSocket = WebSocket.CreateFromStream(transport.Input.AsStream(leaveOpen: true), options);

        // TODO: 
        (_innerTransport, _application) = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
    }

    public void Start()
    {
        // Start pumping a new duplex pair from the WebSocket
    }
}
