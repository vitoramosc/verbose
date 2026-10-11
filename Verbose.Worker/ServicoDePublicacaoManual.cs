using System.Globalization;
using Microsoft.Extensions.Options;
using Verbose.Application.Mensagens;
using Verbose.Application.PortasDeSaida;
using Verbose.Infrastructure.Mensageria;
using Verbose.Worker.Configuracao;

namespace Verbose.Worker;

public sealed class ServicoDePublicacaoManual(
    IHostEnvironment ambiente,
    IHostApplicationLifetime cicloDeVidaDaAplicacao,
    ILogger<ServicoDePublicacaoManual> logger,
    IOptions<OpcoesDePublicacaoManual> opcoes,
    IPublicadorDeSolicitacaoDePalavraDoDia publicador,
    PublicadorDeSolicitacaoNoRabbitMq publicadorDeTeste) : IHostedService
{
    public async Task StartAsync(CancellationToken tokenCancelamento)
    {
        var publicacaoManual = opcoes.Value;

        if (!publicacaoManual.Ativa)
        {
            return;
        }

        if (!ambiente.IsDevelopment())
        {
            throw new InvalidOperationException(
                "A publicação manual só pode ser ativada no ambiente Development.");
        }

        if (publicacaoManual.EnviarMensagemInvalida)
        {
            await publicadorDeTeste.PublicarMensagemInvalidaParaTesteAsync(tokenCancelamento);
            logger.LogWarning("Mensagem JSON inválida publicada exclusivamente para teste da DLQ.");
            cicloDeVidaDaAplicacao.StopApplication();
            return;
        }

        if (!DateOnly.TryParseExact(
                publicacaoManual.DataReferencia,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dataReferencia))
        {
            throw new InvalidOperationException(
                "PublicacaoManual:DataReferencia deve estar no formato yyyy-MM-dd.");
        }

        await publicador.PublicarAsync(
            new SolicitacaoDeGeracaoDaPalavraDoDia(dataReferencia),
            tokenCancelamento);

        logger.LogInformation(
            "Publicação manual concluída para {DataReferencia}.",
            dataReferencia);

        cicloDeVidaDaAplicacao.StopApplication();
    }

    public Task StopAsync(CancellationToken tokenCancelamento)
    {
        return Task.CompletedTask;
    }
}
