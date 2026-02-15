using FluentAssertions;
using GlyphGit.Logging;
using GlyphGit.Logging.Sinks;

namespace GlyphGit.Tests.Unit;

public sealed class ExecutionLoggerTests
{
    [Fact]
    public async Task SuccessfulStep_ShouldEmitExpectedEventChain()
    {
        var sink = new InMemoryEventSink();
        var logger = new ExecutionLogger([sink]);

        await using (var scope = await logger.BeginCommandAsync("status"))
        {
            await scope.RunStepAsync("scan", _ => Task.CompletedTask);
        }

        var names = sink.Events.Select(x => x.EventName).ToArray();
        names.Should().ContainInOrder("CommandStarted", "StepStarted", "StepCompleted", "CommandCompleted");

        var correlationId = sink.Events.First().CorrelationId;
        sink.Events.Should().OnlyContain(x => x.CorrelationId == correlationId);
    }

    [Fact]
    public async Task FailedStep_ShouldEmitStepFailedAndCommandCompleted()
    {
        var sink = new InMemoryEventSink();
        var logger = new ExecutionLogger([sink]);

        await using (var scope = await logger.BeginCommandAsync("status"))
        {
            var act = () => scope.RunStepAsync("scan", _ => Task.FromException(new InvalidOperationException("boom")));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        sink.Events.Select(x => x.EventName).Should().Contain("StepFailed");
        sink.Events.Select(x => x.EventName).Should().Contain("CommandCompleted");
    }
}