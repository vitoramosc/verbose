using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Application.Testes;

public sealed class RegistrarPalavraDoDiaTestes
{
    [Fact]
    public async Task ExecutarAsync_QuandoDataEPalavraSaoIneditas_RegistraPalavraPendente()
    {
        var repositorio = new RepositorioDePalavrasDoDiaEmMemoria();
        var casoDeUso = new RegistrarPalavraDoDia(repositorio);
        var palavraDoDia = CriarPalavraDoDia("irascível");

        var resultado = await casoDeUso.ExecutarAsync(new DateOnly(2026, 10, 10), palavraDoDia);

        Assert.Equal(ResultadoDoRegistroDaPalavraDoDia.Registrada, resultado);
        var registro = Assert.Single(repositorio.Registros);
        Assert.Equal(StatusDeEnvio.PendenteEnvio, registro.StatusDeEnvio);
        Assert.Equal("irascível", registro.PalavraDoDia.Palavra);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoDataJaFoiUsada_NaoSalvaNovoRegistro()
    {
        var repositorio = new RepositorioDePalavrasDoDiaEmMemoria();
        await repositorio.SalvarAsync(new RegistroPalavraDoDia(
            new DateOnly(2026, 10, 10),
            CriarPalavraDoDia("irascível")));
        var casoDeUso = new RegistrarPalavraDoDia(repositorio);

        var resultado = await casoDeUso.ExecutarAsync(
            new DateOnly(2026, 10, 10),
            CriarPalavraDoDia("prolixo"));

        Assert.Equal(ResultadoDoRegistroDaPalavraDoDia.DataJaUtilizada, resultado);
        Assert.Single(repositorio.Registros);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoPalavraJaFoiUsada_IgnoraMaiusculasEMargens()
    {
        var repositorio = new RepositorioDePalavrasDoDiaEmMemoria();
        await repositorio.SalvarAsync(new RegistroPalavraDoDia(
            new DateOnly(2026, 10, 9),
            CriarPalavraDoDia("irascível")));
        var casoDeUso = new RegistrarPalavraDoDia(repositorio);

        var resultado = await casoDeUso.ExecutarAsync(
            new DateOnly(2026, 10, 10),
            CriarPalavraDoDia("  IRASCÍVEL  "));

        Assert.Equal(ResultadoDoRegistroDaPalavraDoDia.PalavraJaUtilizada, resultado);
        Assert.Single(repositorio.Registros);
    }

    private static PalavraDoDia CriarPalavraDoDia(string palavra)
    {
        return new PalavraDoDia(
            palavra,
            "Que se irrita com facilidade; propenso à ira.",
            "Mesmo diante de uma crítica simples, ele ficou irascível.");
    }

    private sealed class RepositorioDePalavrasDoDiaEmMemoria : IRepositorioDePalavrasDoDia
    {
        private readonly List<RegistroPalavraDoDia> registros = [];

        public IReadOnlyCollection<RegistroPalavraDoDia> Registros => registros;

        public Task<RegistroPalavraDoDia?> ObterPorDataAsync(
            DateOnly dataReferencia,
            CancellationToken tokenCancelamento = default)
        {
            var registro = registros.SingleOrDefault(item => item.DataReferencia == dataReferencia);

            return Task.FromResult(registro);
        }

        public Task<bool> PalavraJaFoiUsadaAsync(
            string palavra,
            CancellationToken tokenCancelamento = default)
        {
            var palavraNormalizada = palavra.Trim();
            var palavraJaFoiUsada = registros.Any(registro => string.Equals(
                registro.PalavraDoDia.Palavra,
                palavraNormalizada,
                StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(palavraJaFoiUsada);
        }

        public Task<IReadOnlyCollection<string>> ObterPalavrasUsadasAsync(
            CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(
                registros.Select(registro => registro.PalavraDoDia.Palavra).ToArray());
        }

        public Task SalvarAsync(
            RegistroPalavraDoDia registro,
            CancellationToken tokenCancelamento = default)
        {
            registros.Add(registro);

            return Task.CompletedTask;
        }

        public Task AtualizarAsync(
            RegistroPalavraDoDia registro,
            CancellationToken tokenCancelamento = default)
        {
            var indice = registros.FindIndex(item => item.DataReferencia == registro.DataReferencia);

            if (indice < 0)
            {
                throw new InvalidOperationException("Registro não encontrado.");
            }

            registros[indice] = registro;

            return Task.CompletedTask;
        }
    }
}
