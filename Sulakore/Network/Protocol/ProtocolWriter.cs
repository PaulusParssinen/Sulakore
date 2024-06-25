﻿using System.IO.Pipelines;

namespace Sulakore.Network.Protocol;

internal sealed class ProtocolWriter : IAsyncDisposable
{
    private readonly PipeWriter _writer;
    private readonly SemaphoreSlim _semaphore;
    
    private bool _disposed;

    public ProtocolWriter(PipeWriter writer)
        : this(writer, new SemaphoreSlim(1))
    { }
    public ProtocolWriter(PipeWriter writer, SemaphoreSlim semaphore)
    {
        _writer = writer;
        _semaphore = semaphore;
    }

    public async ValueTask WriteAsync<TWriteMessage>(IMessageWriter<TWriteMessage> writer, TWriteMessage message, 
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            if (_disposed) return;

            writer.WriteMessage(message, _writer);

            var result = await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsCanceled) throw new OperationCanceledException();
            if (result.IsCompleted) _disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask WriteManyAsync<TWriteMessage>(IMessageWriter<TWriteMessage> writer, IEnumerable<TWriteMessage> messages, 
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_disposed) return;

            foreach (var message in messages)
            {
                writer.WriteMessage(message, _writer);
            }

            var result = await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsCanceled) throw new OperationCanceledException();
            if (result.IsCompleted) _disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_disposed) return;
            _disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
