namespace Verbose.Worker.Configuracao;

public sealed class OpcoesDeExecucaoManual
{
    public const string NomeSecao = "ExecucaoManual";

    public bool Ativa { get; set; }

    public string DataReferencia { get; set; } = string.Empty;

    public string Palavra { get; set; } = string.Empty;

    public string Definicao { get; set; } = string.Empty;

    public string Exemplo { get; set; } = string.Empty;
}
