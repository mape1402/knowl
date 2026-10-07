using System.Reflection;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Bootstrap;
using KnOwl.Runtime.Bootstrap.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnOwl.Tests;

public sealed class RuntimeArtifactPullWorkerCoverageTests
{
    [Fact]
    public async Task PullWorkerExecuteCycleLogsSuccessfulErrorsAndFailureBranches()
    {
        var orchestrator = new RecordingPullOrchestrator
        {
            Result = new ControlPlaneArtifactPullExecutionResult
            {
                SourcesScanned = 2,
                PackagesFound = 3,
                Applied = 2,
                Failed = 1,
                Errors = ["first error", "second error"]
            }
        };
        var worker = CreateWorker(orchestrator);

        await InvokeExecuteCycle(worker, CancellationToken.None);
        Assert.Equal(1, orchestrator.Calls);

        orchestrator.NextException = new InvalidOperationException("boom");
        await InvokeExecuteCycle(worker, CancellationToken.None);
        Assert.Equal(2, orchestrator.Calls);

        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();
        orchestrator.NextException = new OperationCanceledException(canceled.Token);
        await Assert.ThrowsAsync<OperationCanceledException>(() => InvokeExecuteCycle(worker, canceled.Token));
        Assert.Equal(3, orchestrator.Calls);
    }

    [Fact]
    public async Task PullWorkerExecuteAsyncRunsOneEnabledCycleBeforeCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var orchestrator = new RecordingPullOrchestrator
        {
            OnPull = () => cancellation.Cancel()
        };
        var worker = CreateWorker(
            orchestrator,
            new KnOwlRuntimeArtifactPullOptions
            {
                Enabled = true,
                InitialDelaySeconds = 0,
                IntervalSeconds = 1
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeExecuteAsync(worker, cancellation.Token));

        Assert.Equal(1, orchestrator.Calls);
    }

    [Fact]
    public async Task PullWorkerHonorsInitialDelayCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var orchestrator = new RecordingPullOrchestrator();
        var worker = CreateWorker(
            orchestrator,
            new KnOwlRuntimeArtifactPullOptions
            {
                Enabled = true,
                InitialDelaySeconds = 1,
                IntervalSeconds = 1
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeExecuteAsync(worker, cancellation.Token));

        Assert.Equal(0, orchestrator.Calls);
    }

    private static object CreateWorker(
        IControlPlaneArtifactPullOrchestrator orchestrator,
        KnOwlRuntimeArtifactPullOptions? options = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(orchestrator)
            .BuildServiceProvider();
        var workerType = typeof(KnOwlRuntimeBootstrapExtensions).Assembly.GetType(
                "KnOwl.Runtime.Bootstrap.Runtime.KnOwlRuntimeArtifactPullWorker",
                throwOnError: true)!
            ;
        var loggerType = typeof(NullLogger<>).MakeGenericType(workerType);
        var logger = loggerType
            .GetField(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null)
            ?? loggerType.GetProperty(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null)
            ?? Activator.CreateInstance(loggerType)!;

        return Activator.CreateInstance(
                workerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args:
                [
                    services.GetRequiredService<IServiceScopeFactory>(),
                    Options.Create(options ?? new KnOwlRuntimeArtifactPullOptions
                    {
                        Enabled = true,
                        InitialDelaySeconds = 0,
                        IntervalSeconds = 1
                    }),
                    logger
                ],
                culture: null)!
            ;
    }

    private static async Task InvokeExecuteCycle(object worker, CancellationToken cancellationToken)
    {
        var method = worker.GetType().GetMethod("ExecuteCycle", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(worker.GetType().FullName, "ExecuteCycle");
        var task = (Task)method.Invoke(worker, [cancellationToken])!;
        await task;
    }

    private static async Task InvokeExecuteAsync(object worker, CancellationToken cancellationToken)
    {
        var method = worker.GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(worker.GetType().FullName, "ExecuteAsync");
        var task = (Task)method.Invoke(worker, [cancellationToken])!;
        await task;
    }

    private sealed class RecordingPullOrchestrator : IControlPlaneArtifactPullOrchestrator
    {
        public int Calls { get; private set; }
        public ControlPlaneArtifactPullExecutionResult Result { get; init; } = new();
        public Exception? NextException { get; set; }
        public Action? OnPull { get; init; }

        public Task<ControlPlaneArtifactPullExecutionResult> PullAvailable(CancellationToken cancellationToken = default)
        {
            Calls++;
            OnPull?.Invoke();
            if (NextException is { } exception)
            {
                NextException = null;
                throw exception;
            }

            return Task.FromResult(Result);
        }
    }
}
