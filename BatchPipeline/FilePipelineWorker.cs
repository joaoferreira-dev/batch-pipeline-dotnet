using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class FilePipelineWorker : BackgroundService
{
    private readonly ILogger<FilePipelineWorker> _logger;
    private readonly FileProcessor _processor;

    // v0.1: caminhos hardcoded para facilitar visualizar
    private readonly string _baseDir = Path.Combine(AppContext.BaseDirectory, "data");
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public FilePipelineWorker(ILogger<FilePipelineWorker> logger, FileProcessor processor)
    {
        _logger = logger;
        _processor = processor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var inbox = Path.Combine(_baseDir, "inbox");
        var processed = Path.Combine(_baseDir, "processed");
        var failed = Path.Combine(_baseDir, "failed");

        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(processed);
        Directory.CreateDirectory(failed);

        _logger.LogInformation("File pipeline v0.1 started. Watching: {Inbox}", inbox);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(inbox);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list inbox directory");
                continue;
            }

            foreach (var filePath in files)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await _processor.ProcessAsync(filePath, processed, failed, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Segurança: se o processor falhar de forma inesperada, não derruba o worker.
                    _logger.LogError(ex, "Unexpected error processing file {File}", filePath);
                }
            }
        }
    }
}