namespace Verbose.Worker.Configuracao;

public sealed class OpcoesDePublicacaoManual
{
    public const string NomeSecao = "PublicacaoManual";

    public bool Ativa { get; set; }

    public bool EnviarMensagemInvalida { get; set; }

    public string DataReferencia { get; set; } = string.Empty;
}
