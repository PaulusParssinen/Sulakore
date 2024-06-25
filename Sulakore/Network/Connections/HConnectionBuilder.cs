using Microsoft.AspNetCore.Connections;

namespace Sulakore.Network.Connections;

/// <summary>
/// A builder for configuring <see cref="HConnection"/> instances
/// </summary>
public class HConnectionBuilder : IConnectionBuilder
{
    private readonly ConnectionBuilder _connectionBuilder;
    private readonly List<Func<ConnectionDelegate, ConnectionDelegate>> _middleware;

    public IServiceProvider ApplicationServices => _connectionBuilder.ApplicationServices;

    public HConnectionBuilder()
        : this(EmptyServiceProvider.Default)
    { }
    public HConnectionBuilder(IServiceProvider serviceProvider)
    {
        _middleware = new List<Func<ConnectionDelegate, ConnectionDelegate>>();
    }

    public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    public ConnectionDelegate Build()
    {
        throw new NotImplementedException();
    }

    internal sealed class EmptyServiceProvider : IServiceProvider
    {
        public static IServiceProvider Default { get; } = new EmptyServiceProvider();

        public object? GetService(Type serviceType) => null;
    }
}
