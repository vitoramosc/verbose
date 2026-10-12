using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace Verbose.Domain;

public sealed class Destinatario
{
    public Destinatario(string email, DateTimeOffset dataDeInscricao)
        : this(email, dataDeInscricao, StatusDaInscricao.PendenteConfirmacao, null, null, null)
    {
    }

    private Destinatario(
        string email,
        DateTimeOffset dataDeInscricao,
        StatusDaInscricao status,
        string? hashDoTokenDeConfirmacao,
        DateTimeOffset? dataDeExpiracaoDaConfirmacao,
        string? hashDoTokenDeCancelamento)
    {
        Email = NormalizarEValidarEmail(email);

        if (dataDeInscricao == default)
        {
            throw new ArgumentException("A data de inscrição deve ser informada.", nameof(dataDeInscricao));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("O status da inscrição informado não é válido.", nameof(status));
        }

        DataDeInscricao = dataDeInscricao;
        Status = status;
        HashDoTokenDeConfirmacao = hashDoTokenDeConfirmacao;
        DataDeExpiracaoDaConfirmacao = dataDeExpiracaoDaConfirmacao;
        HashDoTokenDeCancelamento = hashDoTokenDeCancelamento;
    }

    public string Email { get; }

    public DateTimeOffset DataDeInscricao { get; }

    public StatusDaInscricao Status { get; private set; }

    public string? HashDoTokenDeConfirmacao { get; private set; }

    public DateTimeOffset? DataDeExpiracaoDaConfirmacao { get; private set; }

    public string? HashDoTokenDeCancelamento { get; private set; }

    public static Destinatario CriarPendente(
        string email,
        DateTimeOffset dataDeInscricao,
        string hashDoTokenDeConfirmacao,
        DateTimeOffset dataDeExpiracaoDaConfirmacao)
    {
        ValidarConfirmacao(hashDoTokenDeConfirmacao, dataDeExpiracaoDaConfirmacao, dataDeInscricao);

        return new Destinatario(
            email,
            dataDeInscricao,
            StatusDaInscricao.PendenteConfirmacao,
            hashDoTokenDeConfirmacao,
            dataDeExpiracaoDaConfirmacao,
            hashDoTokenDeCancelamento: null);
    }

    public static Destinatario Reidratar(
        string email,
        DateTimeOffset dataDeInscricao,
        StatusDaInscricao status,
        string? hashDoTokenDeConfirmacao,
        DateTimeOffset? dataDeExpiracaoDaConfirmacao,
        string? hashDoTokenDeCancelamento)
    {
        return new Destinatario(
            email,
            dataDeInscricao,
            status,
            hashDoTokenDeConfirmacao,
            dataDeExpiracaoDaConfirmacao,
            hashDoTokenDeCancelamento);
    }

    public void RenovarConfirmacao(
        string hashDoTokenDeConfirmacao,
        DateTimeOffset dataDeExpiracaoDaConfirmacao,
        DateTimeOffset instanteAtual)
    {
        if (Status == StatusDaInscricao.Confirmada)
        {
            throw new InvalidOperationException("Não é possível renovar uma inscrição já confirmada.");
        }

        ValidarConfirmacao(hashDoTokenDeConfirmacao, dataDeExpiracaoDaConfirmacao, instanteAtual);
        Status = StatusDaInscricao.PendenteConfirmacao;
        HashDoTokenDeConfirmacao = hashDoTokenDeConfirmacao;
        DataDeExpiracaoDaConfirmacao = dataDeExpiracaoDaConfirmacao;
        HashDoTokenDeCancelamento = null;
    }

    public void Confirmar()
    {
        Status = StatusDaInscricao.Confirmada;
        HashDoTokenDeConfirmacao = null;
        DataDeExpiracaoDaConfirmacao = null;
    }

    public void DefinirTokenDeCancelamento(string hashDoTokenDeCancelamento)
    {
        if (Status != StatusDaInscricao.Confirmada)
        {
            throw new InvalidOperationException("A inscrição precisa estar confirmada para receber um token de cancelamento.");
        }

        if (string.IsNullOrWhiteSpace(hashDoTokenDeCancelamento))
        {
            throw new ArgumentException(
                "O hash do token de cancelamento deve ser informado.",
                nameof(hashDoTokenDeCancelamento));
        }

        HashDoTokenDeCancelamento = hashDoTokenDeCancelamento;
    }

    public bool CancelarSeTokenCorresponder(string hashDoTokenRecebido)
    {
        if (Status != StatusDaInscricao.Confirmada ||
            string.IsNullOrWhiteSpace(HashDoTokenDeCancelamento) ||
            !SaoIguais(HashDoTokenDeCancelamento, hashDoTokenRecebido))
        {
            return false;
        }

        Status = StatusDaInscricao.Cancelada;
        HashDoTokenDeCancelamento = null;
        return true;
    }

    public bool ConfirmarSeTokenCorresponder(string hashDoTokenRecebido, DateTimeOffset instanteAtual)
    {
        if (Status != StatusDaInscricao.PendenteConfirmacao ||
            string.IsNullOrWhiteSpace(HashDoTokenDeConfirmacao) ||
            DataDeExpiracaoDaConfirmacao is null ||
            DataDeExpiracaoDaConfirmacao < instanteAtual)
        {
            return false;
        }

        if (!SaoIguais(HashDoTokenDeConfirmacao, hashDoTokenRecebido))
        {
            return false;
        }

        Confirmar();
        return true;
    }

    private static bool SaoIguais(string valorEsperado, string valorRecebido)
    {
        var tokenEsperado = Encoding.UTF8.GetBytes(valorEsperado);
        var tokenRecebido = Encoding.UTF8.GetBytes(valorRecebido);

        return CryptographicOperations.FixedTimeEquals(tokenEsperado, tokenRecebido);
    }

    private static string NormalizarEValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("O e-mail deve ser informado.", nameof(email));
        }

        var emailNormalizado = email.Trim().ToLowerInvariant();

        if (emailNormalizado.Length > 254 || emailNormalizado.Any(char.IsControl))
        {
            throw new ArgumentException("O e-mail informado não é válido.", nameof(email));
        }

        try
        {
            var endereco = new MailAddress(emailNormalizado);

            if (!string.Equals(endereco.Address, emailNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("O e-mail informado não é válido.", nameof(email));
            }
        }
        catch (FormatException excecao)
        {
            throw new ArgumentException("O e-mail informado não é válido.", nameof(email), excecao);
        }

        return emailNormalizado;
    }

    private static void ValidarConfirmacao(
        string hashDoTokenDeConfirmacao,
        DateTimeOffset dataDeExpiracaoDaConfirmacao,
        DateTimeOffset instanteInicial)
    {
        if (string.IsNullOrWhiteSpace(hashDoTokenDeConfirmacao))
        {
            throw new ArgumentException("O hash do token de confirmação deve ser informado.", nameof(hashDoTokenDeConfirmacao));
        }

        if (dataDeExpiracaoDaConfirmacao <= instanteInicial)
        {
            throw new ArgumentException(
                "A expiração da confirmação deve ser posterior ao instante atual.",
                nameof(dataDeExpiracaoDaConfirmacao));
        }
    }
}
