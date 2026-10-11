using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
using Verbose.Application.Servicos;
using Verbose.Domain;

namespace Verbose.Application.Testes;

public sealed class GerarERegistrarPalavraDoDiaTestes
{
    [Fact]
    public async Task ExecutarAsync_QuandoPrimeiraCandidataEReprovada_RegistraAProximaAprovada()
    {
        var repositorio = new RepositorioEmMemoria();
        var gerador = new GeradorSequencial(
            CriarPalavra("corriqueiro", "O uso corriqueiro da expressão passou despercebido."),
            CriarPalavra("lacônico", "Ele foi lacônico ao responder à pergunta."));
        var revisor = new RevisorSequencial(
            new ResultadoDaRevisaoEditorial(false, "A palavra é comum demais."),
            new ResultadoDaRevisaoEditorial(true, "A palavra atende aos critérios."));
        var casoDeUso = new GerarERegistrarPalavraDoDia(
            repositorio,
            gerador,
            revisor,
            new ValidadorEditorialDaPalavraDoDia(),
            new RegistrarPalavraDoDia(repositorio));

        var resultado = await casoDeUso.ExecutarAsync(new DateOnly(2026, 10, 16));

        Assert.Equal(ResultadoDoRegistroDaPalavraDoDia.Registrada, resultado);
        Assert.Equal("lacônico", Assert.Single(repositorio.Registros).PalavraDoDia.Palavra);
        Assert.Contains("corriqueiro", gerador.ListasDePalavrasParaEvitar[1]);
    }

    [Fact]
    public void Validar_QuandoExemploNaoUsaExatamenteAPalavra_RejeitaCandidata()
    {
        var palavraDoDia = CriarPalavra("sobrecarga", "A equipe ficou sobrecarregada.");
        var validador = new ValidadorEditorialDaPalavraDoDia();

        var excecao = Assert.Throws<ArgumentException>(() => validador.Validar(palavraDoDia));

        Assert.StartsWith("O exemplo deve conter exatamente a palavra escolhida.", excecao.Message);
    }

    private static PalavraDoDia CriarPalavra(string palavra, string exemplo)
    {
        return new PalavraDoDia(palavra, "Definição de teste adequada.", exemplo);
    }

    private sealed class GeradorSequencial(params PalavraDoDia[] palavras) : IGeradorDePalavraDoDia
    {
        private readonly Queue<PalavraDoDia> palavras = new(palavras);

        public List<IReadOnlyCollection<string>> ListasDePalavrasParaEvitar { get; } = [];

        public Task<PalavraDoDia> GerarAsync(
            IReadOnlyCollection<string> palavrasParaEvitar,
            CancellationToken tokenCancelamento = default)
        {
            ListasDePalavrasParaEvitar.Add(palavrasParaEvitar.ToArray());

            return Task.FromResult(this.palavras.Dequeue());
        }
    }

    private sealed class RevisorSequencial(params ResultadoDaRevisaoEditorial[] revisoes)
        : IRevisorEditorialDePalavraDoDia
    {
        private readonly Queue<ResultadoDaRevisaoEditorial> revisoes = new(revisoes);

        public Task<ResultadoDaRevisaoEditorial> RevisarAsync(
            PalavraDoDia palavraDoDia,
            CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult(revisoes.Dequeue());
        }
    }

    private sealed class RepositorioEmMemoria : IRepositorioDePalavrasDoDia
    {
        public List<RegistroPalavraDoDia> Registros { get; } = [];

        public Task<RegistroPalavraDoDia?> ObterPorDataAsync(DateOnly dataReferencia, CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult<RegistroPalavraDoDia?>(
                Registros.SingleOrDefault(registro => registro.DataReferencia == dataReferencia));
        }

        public Task<bool> PalavraJaFoiUsadaAsync(string palavra, CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult(Registros.Any(registro => string.Equals(
                registro.PalavraDoDia.Palavra,
                palavra.Trim(),
                StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyCollection<string>> ObterPalavrasUsadasAsync(CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(
                Registros.Select(registro => registro.PalavraDoDia.Palavra).ToArray());
        }

        public Task SalvarAsync(RegistroPalavraDoDia registro, CancellationToken tokenCancelamento = default)
        {
            Registros.Add(registro);
            return Task.CompletedTask;
        }

        public Task AtualizarAsync(RegistroPalavraDoDia registro, CancellationToken tokenCancelamento = default)
        {
            return Task.CompletedTask;
        }
    }
}
