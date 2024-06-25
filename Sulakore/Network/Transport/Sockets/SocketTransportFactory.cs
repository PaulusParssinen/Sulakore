// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Transport.Sockets;

/// <summary>
/// A factory for socket based connections.
/// </summary>
public sealed class SocketTransportFactory : IConnectionListenerFactory
{
    private readonly SocketTransportOptions _options;
    private readonly ILoggerFactory _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketTransportFactory"/> class.
    /// </summary>
    /// <param name="options">The transport options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public SocketTransportFactory(
        IOptions<SocketTransportOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var transport = new SocketConnectionListener(endpoint, _options, _logger);
        transport.Bind();
        return new ValueTask<IConnectionListener>(transport);
    }
}