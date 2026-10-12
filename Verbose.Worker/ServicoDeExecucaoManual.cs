using System.Globalization;
using Microsoft.Extensions.Options;
using Verbose.Application.CasosDeUso;
using Verbose.Domain;
using Verbose.Worker.Configuracao;

namespace Verbose.Worker;

public sealed class ServicoDeExecucaoManual(
    IHostEnvironment ambiente,
    IHostApplicationLifetime cicloDeVidaDaAplicacao,
    ILogger<ServicoDeExecucaoManual> logger,
    IOptions<OpcoesDeExecucaoManual> opcoes,
    RegistrarPalavraDoDia registrarPalavraDoDia) : IHostedService
{
    public async Task StartAsync(CancellationToken tokenCancelamento)
    {
        var execucaoManual = opcoes.Value;

        if (!execucaoManual.Ativa)
        {
            return;
        }

        if (!ambiente.IsDevelopment())
        {
            throw new InvalidOperationException(
                "A execução manual só pode ser ativada no ambiente Development.");
        }

        if (!DateOnly.TryParseExact(
                execucaoManual.DataReferencia,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dataReferencia))
        {
            throw new InvalidOperationException(
                "ExecucaoManual:DataReferencia deve estar no formato yyyy-MM-dd.");
        }

        var palavraDoDia = new PalavraDoDia(
            execucaoManual.Palavra,
            execucaoManual.Definicao,
            execucaoManual.Exemplo);
        var resultado = await registrarPalavraDoDia.ExecutarAsync(
            dataReferencia,
            palavraDoDia,
            tokenCancelamento);

        logger.LogInformation(
            "Execução manual concluída para {DataReferencia}. Resultado: {Resultado}.",
            dataReferencia,
            resultado);

        cicloDeVidaDaAplicacao.ApplicationStarted.Register(cicloDeVidaDaAplicacao.StopApplication);
    }

    public Task StopAsync(CancellationToken tokenCancelamento)
    {
        return Task.CompletedTask;
    }
}
