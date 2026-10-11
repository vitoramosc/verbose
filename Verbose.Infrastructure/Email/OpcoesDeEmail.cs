namespace Verbose.Infrastructure.Email;

public sealed class OpcoesDeEmail
{
    public const string NomeSecao = "Email";

    public string Servidor { get; init; } = string.Empty;

    public int Porta { get; init; }

    public string Remetente { get; init; } = string.Empty;

    public string SenhaDeAplicativo { get; init; } = string.Empty;

    public string Destinatario { get; init; } = string.Empty;
}
