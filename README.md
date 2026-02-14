# GlyphGit

GlyphGit is a terminal-first, git-inspired VCS learning project in C# (.NET 10).
It is a full rewrite with clean architecture, Spectre.Console CLI UX, and structured step logging.

## Features (MVP)

- `init`
- `add <paths>`
- `commit -m "message"`
- `status`
- `log --oneline`
- `diff` (worktree <-> index, index <-> HEAD)
- `restore --staged / --worktree`

## Architecture

- `GlyphGit.Cli`: command-line interface, Spectre rendering
- `GlyphGit.Application`: use cases + interfaces
- `GlyphGit.Domain`: core models + deterministic object codecs
- `GlyphGit.Infrastructure`: filesystem object/ref/index stores, locking, hashing
- `GlyphGit.Logging`: command/step structured logging
- `GlyphGit.Tests`: unit + integration tests

## Repository Layout

GlyphGit stores repository data in `.glyphgit`:

- `.glyphgit/objects/<xx>/<hash>`
- `.glyphgit/refs/heads/<branch>`
- `.glyphgit/HEAD`
- `.glyphgit/index`
- `.glyphgit/config`
- `.glyphgit/logs/YYYYMMDD.jsonl`

## Build and Test

```powershell
dotnet restore
dotnet build
dotnet test
```

## Quickstart

```powershell
glyphgit init
'hello' | Out-File note.txt -Encoding utf8
glyphgit add note.txt
glyphgit commit -m "first commit"
glyphgit status
glyphgit log --oneline
glyphgit diff
```

If you run from source without global install:

```powershell
dotnet run --project .\src\GlyphGit.Cli -- status
```

## Logging Schema

Each line in `.glyphgit/logs/YYYYMMDD.jsonl` is one JSON event:

- `timestampUtc`: UTC event time
- `level`: `Trace|Info|Warning|Error`
- `eventName`: e.g. `CommandStarted`, `StepStarted`, `StepCompleted`, `StepFailed`, `CommandCompleted`
- `message`: human-readable event message
- `command`: command name (`add`, `commit`, ...)
- `correlationId`: unique id for one command run
- `data`: structured metadata map (`step`, `durationMs`, `path`, `blob`, `tree`, `commit`, ...)

## Exit Codes

- `0`: success
- `2`: invalid usage
- `3`: repository error
- `4`: conflict
- `10`: unexpected error

## Roadmap (Phase 2)

- `branch`
- `switch/checkout`
- `merge`
- `tag`
- `stash`

## License

MIT (see `LICENSE`).
