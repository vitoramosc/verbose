using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Application.Servicos;

public sealed class NotificacaoService(
    IRepositorioDePalavrasDoDia repositorio,
    IEmailService emailService)
{
    public async Task EnviarPalavraDoDiaAsync(
        DateOnly dataReferencia,
        CancellationToken tokenCancelamento = default)
    {
        var registro = await repositorio.ObterPorDataAsync(dataReferencia, tokenCancelamento)
            ?? throw new InvalidOperationException(
                $"Não existe palavra registrada para a data {dataReferencia:yyyy-MM-dd}.");

        if (registro.StatusDeEnvio == StatusDeEnvio.Enviada)
        {
            return;
        }

        await emailService.EnviarPalavraDoDiaAsync(registro, tokenCancelamento);

        registro.MarcarComoEnviada();
        await repositorio.AtualizarAsync(registro, tokenCancelamento);
    }
}
