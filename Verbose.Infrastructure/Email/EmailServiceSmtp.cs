using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Email;

public sealed class EmailServiceSmtp(
    OpcoesDeEmail opcoes,
    GeradorDeHtmlDaPalavraDoDia geradorDeHtml) : IEmailService
{
    public async Task EnviarPalavraDoDiaAsync(
        RegistroPalavraDoDia registro,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(opcoes.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(opcoes.Destinatario));
        mensagem.Subject = $"Verbose — {registro.PalavraDoDia.Palavra}";
        mensagem.Body = new BodyBuilder
        {
            HtmlBody = geradorDeHtml.Gerar(registro),
            TextBody = CriarTextoAlternativo(registro)
        }.ToMessageBody();

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

    private static string CriarTextoAlternativo(RegistroPalavraDoDia registro)
    {
        return $"""
            Verbose — A palavra de hoje

            {registro.PalavraDoDia.Palavra}

            Significado: {registro.PalavraDoDia.Definicao}

            Em contexto: {registro.PalavraDoDia.Exemplo}

            Edição de {registro.DataReferencia:dd/MM/yyyy}
            """;
    }
}
