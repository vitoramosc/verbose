using Verbose.Application.PortasDeSaida;
using Verbose.Application.CasosDeUso;
using Verbose.Domain;

namespace Verbose.Application.Servicos;

public sealed class NotificacaoService(
    IRepositorioDePalavrasDoDia repositorioDePalavras,
    IRepositorioDeDestinatarios repositorioDeDestinatarios,
    IRepositorioDeEnviosDaPalavraDoDia repositorioDeEnvios,
    IEmailService emailService,
    Uri urlBaseDeCancelamento)
{
    public async Task EnviarPalavraDoDiaAsync(
        DateOnly dataReferencia,
        CancellationToken tokenCancelamento = default)
    {
        var registro = await repositorioDePalavras.ObterPorDataAsync(dataReferencia, tokenCancelamento)
            ?? throw new InvalidOperationException(
                $"Não existe palavra registrada para a data {dataReferencia:yyyy-MM-dd}.");

        if (registro.StatusDeEnvio == StatusDeEnvio.Enviada)
        {
            return;
        }

        var destinatarios = await repositorioDeDestinatarios.ObterConfirmadosAsync(tokenCancelamento);

        foreach (var destinatario in destinatarios)
        {
            var envio = await repositorioDeEnvios.ObterOuCriarAsync(
                registro.DataReferencia,
                destinatario.Email,
                tokenCancelamento);

            if (envio.FoiEnviado)
            {
                continue;
            }

            var tokenDeCancelamento = ProtecaoDoTokenDeConfirmacao.CriarToken();
            await repositorioDeDestinatarios.DefinirTokenDeCancelamentoAsync(
                destinatario.Email,
                ProtecaoDoTokenDeConfirmacao.GerarHash(tokenDeCancelamento),
                tokenCancelamento);

            await emailService.EnviarPalavraDoDiaAsync(
                registro,
                destinatario.Email,
                CriarLinkDeCancelamento(tokenDeCancelamento),
                tokenCancelamento);

            envio.MarcarComoEnviado();
            await repositorioDeEnvios.AtualizarAsync(envio, tokenCancelamento);
        }

        registro.MarcarComoEnviada();
        await repositorioDePalavras.AtualizarAsync(registro, tokenCancelamento);
    }

    private Uri CriarLinkDeCancelamento(string token)
    {
        var urlBase = urlBaseDeCancelamento.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? urlBaseDeCancelamento
            : new Uri($"{urlBaseDeCancelamento.AbsoluteUri}/", UriKind.Absolute);

        return new Uri(urlBase, $"api/inscricoes/cancelar?token={Uri.EscapeDataString(token)}");
    }
}
