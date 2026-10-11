namespace Verbose.Infrastructure.Mensageria;

public sealed class OpcoesDoRabbitMq
{
    public const string NomeSecao = "RabbitMq";

    public string Host { get; set; } = string.Empty;

    public int Porta { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    public string NomeDaExchange { get; set; } = string.Empty;

    public string NomeDaFila { get; set; } = string.Empty;

    public string ChaveDeRoteamento { get; set; } = string.Empty;

    public string NomeDaExchangeDeFalhas { get; set; } = string.Empty;

    public string NomeDaFilaDeFalhas { get; set; } = string.Empty;

    public string ChaveDeRoteamentoDeFalhas { get; set; } = string.Empty;

    public string NomeDaExchangeDeTentativas { get; set; } = string.Empty;

    public string NomeDaFilaDeTentativas { get; set; } = string.Empty;

    public string ChaveDeRoteamentoDeTentativas { get; set; } = string.Empty;

    public int TempoDeEsperaDaTentativaEmSegundos { get; set; }

    public int MaximoDeNovasTentativas { get; set; }
}
