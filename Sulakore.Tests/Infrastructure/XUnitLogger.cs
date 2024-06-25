using Microsoft.Extensions.Logging;
using System.Text;
using Xunit.Abstractions;

namespace Sulakore.Tests.Infrastructure;

internal sealed class XUnitLogger<T>(
    ITestOutputHelper testOutputHelper, 
    LoggerExternalScopeProvider scopeProvider, 
    string? categoryName) : ILogger<T>
{
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var sb = new StringBuilder($"{logLevel} [{categoryName}]");
        sb.Append(logLevel.ToString())
          .Append(" [").Append(categoryName).Append("] ")
          .Append(formatter(state, exception));

        if (exception is not null)
        {
            sb.Append('\n').Append(exception);
        }

        scopeProvider.ForEachScope(static (scope, state) =>
        {
            state.Appe($"\n => {}");
            state.Append(scope);
        }, sb);

        testOutputHelper.WriteLine(sb.ToString());
    }

    public static ILogger<T> Create(ITestOutputHelper testOutputHelper) => new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider(), typeof(T).FullName);
}
