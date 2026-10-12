using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Application.Testes;

public sealed class ConfirmarInscricaoTestes
{
    [Fact]
    public async Task ExecutarAsync_QuandoTokenValido_EnviaHashAoRepositorio()
    {
        var repositorio = new RepositorioDeDestinatariosFalso();
        var casoDeUso = new ConfirmarInscricao(
            repositorio,
            new RelogioFixo(new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero)));

        var resultado = await casoDeUso.ExecutarAsync("token-de-confirmacao");

        Assert.Equal(ResultadoDaConfirmacaoDaInscricao.Confirmada, resultado);
        Assert.Equal(
            ProtecaoDoTokenDeConfirmacao.GerarHash("token-de-confirmacao"),
            repositorio.HashRecebido);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoTokenVazio_NaoConsultaRepositorio()
    {
        var repositorio = new RepositorioDeDestinatariosFalso();
        var casoDeUso = new ConfirmarInscricao(repositorio, TimeProvider.System);

        var resultado = await casoDeUso.ExecutarAsync(string.Empty);

        Assert.Equal(ResultadoDaConfirmacaoDaInscricao.InvalidaOuExpirada, resultado);
        Assert.Null(repositorio.HashRecebido);
    }

    private sealed class RepositorioDeDestinatariosFalso : IRepositorioDeDestinatarios
    {
        public string? HashRecebido { get; private set; }

        public Task<IReadOnlyCollection<Destinatario>> ObterConfirmadosAsync(
            CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult<IReadOnlyCollection<Destinatario>>([]);
        }

        public Task<ResultadoDaPersistenciaDaInscricao> CriarOuRenovarPendenteAsync(
            Destinatario destinatario,
            CancellationToken tokenCancelamento = default)
        {
            throw new NotSupportedException();
        }

        public Task<ResultadoDaConfirmacaoDaInscricao> ConfirmarPeloHashDoTokenAsync(
            string hashDoToken,
            DateTimeOffset instanteAtual,
            CancellationToken tokenCancelamento = default)
        {
            HashRecebido = hashDoToken;
            return Task.FromResult(ResultadoDaConfirmacaoDaInscricao.Confirmada);
        }

        public Task DefinirTokenDeCancelamentoAsync(
            string email,
            string hashDoToken,
            CancellationToken tokenCancelamento = default) => throw new NotSupportedException();

        public Task<ResultadoDoCancelamentoDaInscricao> CancelarPeloHashDoTokenAsync(
            string hashDoToken,
            CancellationToken tokenCancelamento = default) => throw new NotSupportedException();
    }

    private sealed class RelogioFixo(DateTimeOffset instante) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instante;
    }
}
