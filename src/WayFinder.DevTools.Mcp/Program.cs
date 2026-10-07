using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WayFinder.DevTools.Infrastructure.Composition;

var builder =
    Host.CreateApplicationBuilder(
        args
    );

builder.Logging.AddConsole(
    options =>
    {
        options.LogToStandardErrorThreshold =
            LogLevel.Trace;
    }
);

var services =
    WayFinderComposition.Create();

builder.Services.AddSingleton(
    services
);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder
    .Build()
    .RunAsync();
