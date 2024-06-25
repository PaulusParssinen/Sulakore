
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Connections;

var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddConsole();
    })
    .BuildServiceProvider();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => 
    {

    });

var builder2 = new ConnectionBuilder(serviceProvider);

// 
// var builder = WebApplication.CreateBuilder(args);
// 