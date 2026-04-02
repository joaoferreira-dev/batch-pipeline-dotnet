using Microsoft.Extensions.Logging;

public sealed class FileProcessor
{
    private readonly ILogger<FileProcessor> _logger;

    public FileProcessor(ILogger<FileProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(
        string filePath,
        string processedDir,
        string failedDir,
        CancellationToken ct)
    {
        var fileName = Path.GetFileName(filePath);
        _logger.LogInformation("Processing {FileName}", fileName);

        // v0.1: "processar" = só ler o arquivo inteiro e garantir que não está vazio
        try
        {
            var content = await File.ReadAllTextAsync(filePath, ct);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("File is empty");

            // (Opcional v0.1) Exemplo: contar linhas
            var lineCount = content.Split('\n').Length;
            _logger.LogInformation("{FileName}: {LineCount} lines", fileName, lineCount);

            MoveTo(filePath, Path.Combine(processedDir, fileName));
            _logger.LogInformation("{FileName} -> processed", fileName);
        }
        catch (Exception ex)
        {
            var errorPath = Path.Combine(failedDir, fileName + ".error.txt");
            await File.WriteAllTextAsync(errorPath, ex.ToString(), ct);

            MoveTo(filePath, Path.Combine(failedDir, fileName));
            _logger.LogWarning(ex, "{FileName} -> failed", fileName);
        }
    }

    private static void MoveTo(string sourcePath, string destPath)
    {
        // Evita falhar se já existir (v0.1): adiciona sufixo.
        var finalPath = destPath;
        if (File.Exists(finalPath))
        {
            var dir = Path.GetDirectoryName(destPath)!;
            var name = Path.GetFileNameWithoutExtension(destPath);
            var ext = Path.GetExtension(destPath);
            finalPath = Path.Combine(dir, $"{name}_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}{ext}");
        }

        File.Move(sourcePath, finalPath);
    }
}