namespace Verbose.Worker;

public sealed class ServicoInicial(ILogger<ServicoInicial> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken tokenParada)
    {
        logger.LogInformation("O Worker do Verbose foi iniciado.");

        return Task.Delay(Timeout.InfiniteTimeSpan, tokenParada);
    }
}
