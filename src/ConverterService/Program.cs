using ConverterService;
using ConverterService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {

        services.AddSingleton<FFmpegService>();
        services.AddSingleton<StorageService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
