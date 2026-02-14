namespace GlyphGit.Application.Models;

public enum ExitCode
{
    Success = 0,
    InvalidUsage = 2,
    RepositoryError = 3,
    Conflict = 4,
    UnexpectedError = 10
}
