using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class FileProcessorTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _inbox;
    private readonly string _processed;
    private readonly string _failed;
    private readonly FileProcessor _sut;

    public FileProcessorTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "BatchPipeline.Tests", Guid.NewGuid().ToString("N"));
        _inbox = Path.Combine(_testRoot, "inbox");
        _processed = Path.Combine(_testRoot, "processed");
        _failed = Path.Combine(_testRoot, "failed");

        Directory.CreateDirectory(_inbox);
        Directory.CreateDirectory(_processed);
        Directory.CreateDirectory(_failed);

        _sut = new FileProcessor(NullLogger<FileProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_WithValidContent_MovesFileToProcessed()
    {
        var sourceFile = Path.Combine(_inbox, "sample.txt");
        await File.WriteAllTextAsync(sourceFile, "line1\nline2");

        await _sut.ProcessAsync(sourceFile, _processed, _failed, CancellationToken.None);

        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(Path.Combine(_processed, "sample.txt")));
        Assert.Empty(Directory.GetFiles(_failed));
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyContent_MovesFileToFailedAndWritesError()
    {
        var sourceFile = Path.Combine(_inbox, "empty.txt");
        await File.WriteAllTextAsync(sourceFile, "   ");

        await _sut.ProcessAsync(sourceFile, _processed, _failed, CancellationToken.None);

        var failedFile = Path.Combine(_failed, "empty.txt");
        var errorFile = Path.Combine(_failed, "empty.txt.error.txt");

        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(failedFile));
        Assert.True(File.Exists(errorFile));

        var errorContent = await File.ReadAllTextAsync(errorFile);
        Assert.Contains("File is empty", errorContent);
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessedFileAlreadyExists_CreatesRenamedFile()
    {
        var existingProcessed = Path.Combine(_processed, "duplicate.txt");
        await File.WriteAllTextAsync(existingProcessed, "existing-content");

        var sourceFile = Path.Combine(_inbox, "duplicate.txt");
        await File.WriteAllTextAsync(sourceFile, "new-content");

        await _sut.ProcessAsync(sourceFile, _processed, _failed, CancellationToken.None);

        var processedFiles = Directory.GetFiles(_processed, "duplicate*.txt");

        Assert.Equal(2, processedFiles.Length);
        Assert.Equal("existing-content", await File.ReadAllTextAsync(existingProcessed));

        var renamedFile = processedFiles.Single(path => Path.GetFileName(path) != "duplicate.txt");
        Assert.Equal("new-content", await File.ReadAllTextAsync(renamedFile));
        Assert.Empty(Directory.GetFiles(_failed));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
