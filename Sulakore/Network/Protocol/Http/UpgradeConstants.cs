namespace Sulakore.Network.Protocol.Http;

internal static class UpgradeConstants
{
    private static ReadOnlySpan<byte> SecWebSocketKeyBytes => "Sec-WebSocket-Key: "u8;

    private static ReadOnlySpan<byte> UpgradeWebSocketResponseBytes
        => "HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: "u8;
}