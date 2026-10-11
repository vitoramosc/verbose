namespace Verbose.Infrastructure.Ollama;

public sealed class OpcoesDoOllama
{
    public const string NomeSecao = "Ollama";

    public string UrlBase { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int TempoLimiteEmSegundos { get; set; }
}
