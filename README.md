# GlyphGit (MiniGit 2.0)

GlyphGit is a modern terminal-first VCS learning project in C# (.NET 10), built as a full rewrite with clean architecture.

## Architecture

- `GlyphGit.Cli`: Spectre.Console based command UI
- `GlyphGit.Application`: UseCases + interfaces
- `GlyphGit.Domain`: core git-like model + deterministic codecs
- `GlyphGit.Infrastructure`: filesystem stores, hashing, locking, path safety
- `GlyphGit.Logging`: custom step-based execution logging with JSONL sink
- `GlyphGit.Tests`: unit + integration tests

## Repository Format

GlyphGit stores data in `.glyphgit`:

- `.glyphgit/objects/<xx>/<hash>`
- `.glyphgit/refs/heads/<branch>`
- `.glyphgit/HEAD`
- `.glyphgit/index`
- `.glyphgit/config`
- `.glyphgit/logs/*.jsonl`

## Commands (MVP)

- `glyphgit init`
- `glyphgit add <paths>`
- `glyphgit commit -m "message"`
- `glyphgit status`
- `glyphgit log --oneline`
- `glyphgit diff`
- `glyphgit restore --staged [paths]`
- `glyphgit restore --worktree [paths]`

## Build / Test

```powershell
dotnet restore
dotnet build
dotnet test
