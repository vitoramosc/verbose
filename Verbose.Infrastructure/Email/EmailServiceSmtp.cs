using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Email;

public sealed class EmailServiceSmtp(
    OpcoesDeEmail opcoes,
    GeradorDeHtmlDaPalavraDoDia geradorDeHtml,
    GeradorDeHtmlDeConfirmacaoDeInscricao geradorDeHtmlDeConfirmacao) : IEmailService
{
    public async Task EnviarPalavraDoDiaAsync(
        RegistroPalavraDoDia registro,
        string destinatario,
        Uri linkDeCancelamento,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(opcoes.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = $"Verbose — {registro.PalavraDoDia.Palavra}";
        mensagem.Body = new BodyBuilder
        {
            HtmlBody = geradorDeHtml.Gerar(registro, linkDeCancelamento),
            TextBody = CriarTextoAlternativo(registro, linkDeCancelamento)
        }.ToMessageBody();

        await EnviarAsync(mensagem, tokenCancelamento);
    }

    public async Task EnviarConfirmacaoDeInscricaoAsync(
        string email,
        Uri linkDeConfirmacao,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(linkDeConfirmacao);

        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(opcoes.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(email));
        mensagem.Subject = "Confirme sua inscrição no Verbose";
        mensagem.Body = new BodyBuilder
        {
            HtmlBody = geradorDeHtmlDeConfirmacao.Gerar(linkDeConfirmacao),
            TextBody = $"""
                Confirme sua inscrição no Verbose

                Para receber uma palavra nova por dia, acesse o link abaixo:
                {linkDeConfirmacao.AbsoluteUri}

                Se você não solicitou esta inscrição, ignore esta mensagem.
                """
        }.ToMessageBody();

        await EnviarAsync(mensagem, tokenCancelamento);
    }

    private async Task EnviarAsync(MimeMessage mensagem, CancellationToken tokenCancelamento)
    {
        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(
            opcoes.Servidor,
            opcoes.Porta,
            SecureSocketOptions.StartTls,
            tokenCancelamento);
        try
        {
            await cliente.AuthenticateAsync(
                opcoes.Remetente,
                opcoes.SenhaDeAplicativo,
                tokenCancelamento);
        }
        catch (SmtpProtocolException excecao) when (
            excecao.Message.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase))
        {
            throw new FalhaPermanenteDeEmailException(
                "O servidor SMTP recusou as credenciais configuradas.",
                excecao);
        }
        await cliente.SendAsync(mensagem, tokenCancelamento);
        await cliente.DisconnectAsync(quit: true, tokenCancelamento);
    }

    private static string CriarTextoAlternativo(RegistroPalavraDoDia registro, Uri linkDeCancelamento)
    {
        return $"""
            Verbose — A palavra de hoje

            {registro.PalavraDoDia.Palavra}

            Significado: {registro.PalavraDoDia.Definicao}

            Em contexto: {registro.PalavraDoDia.Exemplo}

            Edição de {registro.DataReferencia:dd/MM/yyyy}

            Para cancelar sua inscrição, acesse:
            {linkDeCancelamento.AbsoluteUri}
            """;
    }
}
