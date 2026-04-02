using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<FileProcessor>();
        services.AddHostedService<FilePipelineWorker>();
    })
    .Build();

await host.RunAsync();