using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Application.CasosDeUso;

public sealed class CadastrarDestinatario(
    IRepositorioDeDestinatarios repositorio,
    IEmailService emailService,
    TimeProvider relogio,
    Uri urlBaseDeConfirmacao,
    TimeSpan validadeDaConfirmacao)
{
    public async Task<ResultadoDoCadastroDeDestinatario> ExecutarAsync(
        string email,
        CancellationToken tokenCancelamento = default)
    {
        if (validadeDaConfirmacao <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("A validade da confirmação deve ser positiva.");
        }

        var instanteAtual = relogio.GetUtcNow();
        var token = ProtecaoDoTokenDeConfirmacao.CriarToken();
        var hashDoToken = ProtecaoDoTokenDeConfirmacao.GerarHash(token);
        var destinatario = Destinatario.CriarPendente(
            email,
            instanteAtual,
            hashDoToken,
            instanteAtual.Add(validadeDaConfirmacao));
        var resultadoDaPersistencia = await repositorio.CriarOuRenovarPendenteAsync(
            destinatario,
            tokenCancelamento);

        if (resultadoDaPersistencia == ResultadoDaPersistenciaDaInscricao.JaConfirmada)
        {
            return ResultadoDoCadastroDeDestinatario.JaConfirmado;
        }

        var linkDeConfirmacao = CriarLinkDeConfirmacao(token);
        await emailService.EnviarConfirmacaoDeInscricaoAsync(
            destinatario.Email,
            linkDeConfirmacao,
            tokenCancelamento);

        return ResultadoDoCadastroDeDestinatario.ConfirmacaoEnviada;
    }

    private Uri CriarLinkDeConfirmacao(string token)
    {
        var urlBase = urlBaseDeConfirmacao.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? urlBaseDeConfirmacao
            : new Uri($"{urlBaseDeConfirmacao.AbsoluteUri}/", UriKind.Absolute);

        return new Uri(urlBase, $"api/inscricoes/confirmar?token={Uri.EscapeDataString(token)}");
    }
}
