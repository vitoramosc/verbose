using Microsoft.Extensions.Options;
using Verbose.Application.Mensagens;
using Verbose.Application.PortasDeSaida;
using Verbose.Worker.Configuracao;

namespace Verbose.Worker;

public sealed class ServicoDeAgendamentoDiario(
    IPublicadorDeSolicitacaoDePalavraDoDia publicador,
    IOptions<OpcoesDeAgendamento> opcoes,
    TimeProvider relogio,
    ILogger<ServicoDeAgendamentoDiario> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuracao = opcoes.Value;
        configuracao.TentarObterHorario(out var horario);
        var fusoHorario = TimeZoneInfo.FindSystemTimeZoneById(configuracao.FusoHorario);
        var primeiraExecucao = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            var agoraNoFuso = TimeZoneInfo.ConvertTime(relogio.GetUtcNow(), fusoHorario);
            var dataDeHoje = DateOnly.FromDateTime(agoraNoFuso.DateTime);
            var horarioAtual = TimeOnly.FromDateTime(agoraNoFuso.DateTime);

            if (primeiraExecucao && horarioAtual > horario)
            {
                logger.LogInformation(
                    "O horário diário já passou. Publicando imediatamente a solicitação para {DataReferencia}.",
                    dataDeHoje);
                await PublicarAsync(dataDeHoje, stoppingToken);
                primeiraExecucao = false;
                continue;
            }

            var dataDaProximaExecucao = horarioAtual <= horario
                ? dataDeHoje
                : dataDeHoje.AddDays(1);
            var proximaExecucao = CriarDataHoraNoFuso(dataDaProximaExecucao, horario, fusoHorario);
            var espera = proximaExecucao - relogio.GetUtcNow();

            logger.LogInformation(
                "Próxima publicação diária agendada para {ProximaExecucao} ({FusoHorario}).",
                proximaExecucao,
                configuracao.FusoHorario);

            await Task.Delay(espera, relogio, stoppingToken);
            await PublicarAsync(dataDaProximaExecucao, stoppingToken);
            primeiraExecucao = false;
        }
    }

    private async Task PublicarAsync(DateOnly dataReferencia, CancellationToken tokenCancelamento)
    {
        await publicador.PublicarAsync(
            new SolicitacaoDeGeracaoDaPalavraDoDia(dataReferencia),
            tokenCancelamento);
        logger.LogInformation(
            "Solicitação diária publicada para a data de referência {DataReferencia}.",
            dataReferencia);
    }

    private static DateTimeOffset CriarDataHoraNoFuso(
        DateOnly data,
        TimeOnly horario,
        TimeZoneInfo fusoHorario)
    {
        var dataHoraLocal = data.ToDateTime(horario, DateTimeKind.Unspecified);

        return new DateTimeOffset(dataHoraLocal, fusoHorario.GetUtcOffset(dataHoraLocal));
    }
}
