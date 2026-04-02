# Copilot Instructions for `BatchPipeline`

## Project context
- Runtime and target framework: `.NET 10` (`net10.0`).
- Main app: Worker Service in `BatchPipeline`.
- Tests: `xUnit` project in `BatchPipeline.Tests`.
- CI workflow: `.github/workflows/build-and-pr.yml`.

## Architecture and coding rules
- Keep `FilePipelineWorker` as orchestration only (`BackgroundService` loop, scheduling, cancellation, logging).
- Keep `FileProcessor` focused on file processing behavior and file movement.
- Use dependency injection registration in `Program.cs`.
- Prefer constructor injection; avoid static mutable state.
- Preserve nullable reference types and implicit usings.
- Follow existing style: minimal comments, clear method names, async APIs with `CancellationToken`.

## File processing behavior to preserve
- Read file content asynchronously.
- Empty/whitespace files must be treated as failure.
- On success, move file to `processed`.
- On failure, write `*.error.txt` and move original file to `failed`.
- If destination filename exists, append timestamp suffix before move.

## Testing guidance
- Add/update unit tests in `BatchPipeline.Tests` when changing `FileProcessor` behavior.
- Use isolated temp directories per test and clean up in `Dispose`.
- Keep tests deterministic and independent from environment-specific paths.

## CI expectations
- Ensure commands remain valid:
  - `dotnet restore BatchPipeline/BatchPipeline.csproj`
  - `dotnet build BatchPipeline/BatchPipeline.csproj --configuration Release --no-restore`
  - `dotnet test BatchPipeline.Tests/BatchPipeline.Tests.csproj --configuration Release --no-restore`
- Keep workflow order: `build` -> `test` -> `create-pr-to-main`.
