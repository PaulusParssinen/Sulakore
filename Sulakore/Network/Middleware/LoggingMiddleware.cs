// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;

using Sulakore.Network.Internal;

namespace Sulakore.Network.Middleware;

internal sealed partial class LoggingMiddleware
{
    private readonly ConnectionDelegate _next;
    private readonly ILogger _logger;

    public LoggingMiddleware(ConnectionDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task OnConnectionAsync(ConnectionContext context)
    {
        var originalTransport = context.Transport;
        try
        {
            await using var loggingDuplexPipe = new LoggingDuplexPipe(context.Transport, _logger);
            context.Transport = loggingDuplexPipe;

            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Transport = originalTransport;
        }
    }

    private sealed class LoggingDuplexPipe : DuplexPipeStreamAdapter<LoggingStream>
    {
        public LoggingDuplexPipe(IDuplexPipe transport, ILogger logger) 
            : base(transport, stream => new LoggingStream(stream, logger))
        { }
    }
    private sealed partial class LoggingStream : Stream
    {
        [LoggerMessage(0, LogLevel.Debug, "Read {read} bytes")]
        static partial void LogRead(ILogger logger, int read);

        [LoggerMessage(1, LogLevel.Debug, "Read {read} bytes")]
        static partial void LogReadAsync(ILogger logger, int read);

        [LoggerMessage(2, LogLevel.Debug, "Write {count} bytes")]
        static partial void LogWrite(ILogger logger, int count);

        [LoggerMessage(3, LogLevel.Debug, "Write {count} bytes")]
        static partial void LogWriteAsync(ILogger logger, int count);

        private readonly Stream _inner;
        private readonly ILogger _logger;

        public LoggingStream(Stream inner, ILogger logger)
            => (_inner, _logger) = (inner, logger);

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) 
            => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            LogRead(_logger, read);
            return read;
        }
        public override int Read(Span<byte> destination)
        {
            int read = _inner.Read(destination);
            LogRead(_logger, read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            LogReadAsync(_logger, read);
            return read;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            int read = await _inner.ReadAsync(destination, cancellationToken);
            LogReadAsync(_logger, read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            LogWrite(_logger, count);
            _inner.Write(buffer, offset, count);
        }
        public override void Write(ReadOnlySpan<byte> source)
        {
            LogWrite(_logger, source.Length);
            _inner.Write(source);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            LogWriteAsync(_logger, count);
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            LogWriteAsync(_logger, source.Length);
            return _inner.WriteAsync(source, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();

        public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();
        public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
    }
}
