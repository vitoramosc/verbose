namespace Verbose.Worker.Configuracao;

public sealed class OpcoesDeInscricao
{
    public const string NomeSecao = "Inscricao";

    public string UrlBaseDeConfirmacao { get; init; } = string.Empty;

    public int HorasDeValidadeDaConfirmacao { get; init; }
}
