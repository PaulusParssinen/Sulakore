// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks.Sources;

namespace Sulakore.Network.Internal;

#nullable enable

internal class SocketAwaitableEventArgs : SocketAsyncEventArgs, IValueTaskSource<int>
{
    private static readonly Action<object?> _continuationCompleted = _ => { };

    private readonly PipeScheduler _ioScheduler;

    private Action<object?>? _continuation;

    public SocketAwaitableEventArgs(PipeScheduler ioScheduler)
        : base(unsafeSuppressExecutionContextFlow: true)
    {
        _ioScheduler = ioScheduler;
    }

    protected override void OnCompleted(SocketAsyncEventArgs _)
    {
        var c = _continuation;

        if (c != null || (c = Interlocked.CompareExchange(ref _continuation, _continuationCompleted, null)) != null)
        {
            var continuationState = UserToken;
            UserToken = null;
            _continuation = _continuationCompleted; // in case someone's polling IsCompleted

            _ioScheduler.Schedule(c, continuationState);
        }
    }

    public int GetResult(short token)
    {
        _continuation = null;

        if (SocketError != SocketError.Success)
        {
            ThrowSocketException(SocketError);
        }

        return BytesTransferred;

        static void ThrowSocketException(SocketError e)
        {
            throw CreateException(e);
        }
    }

    protected static SocketException CreateException(SocketError e) => new((int)e);

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return !ReferenceEquals(_continuation, _continuationCompleted) ? ValueTaskSourceStatus.Pending :
                SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                ValueTaskSourceStatus.Faulted;
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        UserToken = state;
        var prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
        if (ReferenceEquals(prevContinuation, _continuationCompleted))
        {
            UserToken = null;
            ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
        }
    }
}