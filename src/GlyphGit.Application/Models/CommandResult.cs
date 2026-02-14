namespace GlyphGit.Application.Models;

public sealed record CommandResult(ExitCode ExitCode, string Message)
{
    public bool IsSuccess => ExitCode == ExitCode.Success;

    public static CommandResult Ok(string message) => new(ExitCode.Success, message);
    public static CommandResult Fail(ExitCode code, string message) => new(code, message);
}

public sealed record CommandResult<T>(ExitCode ExitCode, string Message, T? Payload)
{
    public bool IsSuccess => ExitCode == ExitCode.Success;

    public static CommandResult<T> Ok(string message, T payload) => new(ExitCode.Success, message, payload);
    public static CommandResult<T> Fail(ExitCode code, string message) => new(code, message, default);
}
