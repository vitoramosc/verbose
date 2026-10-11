using Verbose.Application.Mensagens;
using Verbose.Application.CasosDeUso;
using Verbose.Application.Servicos;
using Verbose.Infrastructure.Mensageria;

namespace Verbose.Worker;

public sealed class ServicoDeConsumoDeSolicitacoes(
    ConsumidorDeSolicitacoesNoRabbitMq consumidor,
    GerarERegistrarPalavraDoDia gerarERegistrar,
    NotificacaoService notificacaoService,
    ILogger<ServicoDeConsumoDeSolicitacoes> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consumidor.IniciarAsync(ProcessarSolicitacaoAsync, stoppingToken);
        logger.LogInformation("Consumidor RabbitMQ aguardando solicitações de geração da palavra do dia.");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProcessarSolicitacaoAsync(
        SolicitacaoDeGeracaoDaPalavraDoDia solicitacao,
        CancellationToken tokenCancelamento)
    {
        logger.LogInformation("Iniciando geração para a data de referência {DataReferencia}.", solicitacao.DataReferencia);
        var resultado = await gerarERegistrar.ExecutarAsync(solicitacao.DataReferencia, tokenCancelamento);

        if (resultado == ResultadoDoRegistroDaPalavraDoDia.PalavraJaUtilizada)
        {
            throw new InvalidOperationException(
                "A geração retornou uma palavra que já havia sido utilizada.");
        }

        await notificacaoService.EnviarPalavraDoDiaAsync(solicitacao.DataReferencia, tokenCancelamento);

        logger.LogInformation(
            "Geração e notificação para {DataReferencia} concluídas com resultado {Resultado}. ACK será enviado após este processamento.",
            solicitacao.DataReferencia,
            resultado);
    }
}
