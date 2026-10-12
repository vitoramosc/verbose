using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;
using Verbose.Infrastructure.Persistencia;

namespace Verbose.Infrastructure.Testes;

public sealed class RepositorioDeDestinatariosEmJsonTestes
{
    [Fact]
    public async Task CriarOuRenovarPendenteAsync_QuandoEmailInedito_PersisteApenasHashDoToken()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDeDestinatarios repositorio = new RepositorioDeDestinatariosEmJson(diretorioTemporario.Caminho);
        var token = "token-secreto-de-teste";

        var resultado = await repositorio.CriarOuRenovarPendenteAsync(CriarDestinatario("pessoa@exemplo.com", token));

        var conteudo = await File.ReadAllTextAsync(Path.Combine(diretorioTemporario.Caminho, "destinatarios.json"));
        Assert.Equal(ResultadoDaPersistenciaDaInscricao.PendenteDeConfirmacao, resultado);
        Assert.Contains("\"status\": \"PendenteConfirmacao\"", conteudo);
        Assert.DoesNotContain(token, conteudo);
        Assert.Contains(ProtecaoDoTokenDeConfirmacao.GerarHash(token), conteudo);
    }

    [Fact]
    public async Task ConfirmarPeloHashDoTokenAsync_QuandoTokenValido_ConfirmaEInvalidaReutilizacao()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDeDestinatarios repositorio = new RepositorioDeDestinatariosEmJson(diretorioTemporario.Caminho);
        var token = "token-secreto-de-teste";
        await repositorio.CriarOuRenovarPendenteAsync(CriarDestinatario("pessoa@exemplo.com", token));

        var primeiroResultado = await repositorio.ConfirmarPeloHashDoTokenAsync(
            ProtecaoDoTokenDeConfirmacao.GerarHash(token),
            new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero));
        var segundoResultado = await repositorio.ConfirmarPeloHashDoTokenAsync(
            ProtecaoDoTokenDeConfirmacao.GerarHash(token),
            new DateTimeOffset(2026, 10, 11, 16, 1, 0, TimeSpan.Zero));

        Assert.Equal(ResultadoDaConfirmacaoDaInscricao.Confirmada, primeiroResultado);
        Assert.Equal(ResultadoDaConfirmacaoDaInscricao.InvalidaOuExpirada, segundoResultado);
    }

    [Fact]
    public async Task CriarOuRenovarPendenteAsync_QuandoChamadoConcorrentemente_PersisteUmDestinatario()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDeDestinatarios repositorio = new RepositorioDeDestinatariosEmJson(diretorioTemporario.Caminho);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(indice =>
            repositorio.CriarOuRenovarPendenteAsync(CriarDestinatario("pessoa@exemplo.com", $"token-{indice}"))));

        Assert.All(resultados, resultado => Assert.Equal(ResultadoDaPersistenciaDaInscricao.PendenteDeConfirmacao, resultado));
        var conteudo = await File.ReadAllTextAsync(Path.Combine(diretorioTemporario.Caminho, "destinatarios.json"));
        Assert.Equal(1, conteudo.Split("\"email\":", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public async Task CancelarPeloHashDoTokenAsync_QuandoTokenValido_AlteraStatusEExcluiDosConfirmados()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDeDestinatarios repositorio = new RepositorioDeDestinatariosEmJson(diretorioTemporario.Caminho);
        const string tokenDeConfirmacao = "token-de-confirmacao";
        const string tokenDeCancelamento = "token-de-cancelamento";
        await repositorio.CriarOuRenovarPendenteAsync(CriarDestinatario("pessoa@exemplo.com", tokenDeConfirmacao));
        await repositorio.ConfirmarPeloHashDoTokenAsync(
            ProtecaoDoTokenDeConfirmacao.GerarHash(tokenDeConfirmacao),
            new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero));
        await repositorio.DefinirTokenDeCancelamentoAsync(
            "pessoa@exemplo.com",
            ProtecaoDoTokenDeConfirmacao.GerarHash(tokenDeCancelamento));

        var resultado = await repositorio.CancelarPeloHashDoTokenAsync(
            ProtecaoDoTokenDeConfirmacao.GerarHash(tokenDeCancelamento));

        Assert.Equal(ResultadoDoCancelamentoDaInscricao.Cancelada, resultado);
        Assert.Empty(await repositorio.ObterConfirmadosAsync());
        var conteudo = await File.ReadAllTextAsync(Path.Combine(diretorioTemporario.Caminho, "destinatarios.json"));
        Assert.Contains("\"status\": \"Cancelada\"", conteudo);
        Assert.DoesNotContain(tokenDeCancelamento, conteudo);
    }

    [Fact]
    public async Task CriarOuRenovarPendenteAsync_QuandoJsonInvalido_LancaFalhaExplicita()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        await File.WriteAllTextAsync(Path.Combine(diretorioTemporario.Caminho, "destinatarios.json"), "{ conteudo invalido }");
        IRepositorioDeDestinatarios repositorio = new RepositorioDeDestinatariosEmJson(diretorioTemporario.Caminho);

        var excecao = await Assert.ThrowsAsync<InvalidDataException>(
            async () => await repositorio.CriarOuRenovarPendenteAsync(CriarDestinatario("pessoa@exemplo.com", "token")));

        Assert.Equal("O arquivo de destinatários contém JSON inválido.", excecao.Message);
    }

    private static Destinatario CriarDestinatario(string email, string token)
    {
        var dataDeInscricao = new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);
        return Destinatario.CriarPendente(
            email,
            dataDeInscricao,
            ProtecaoDoTokenDeConfirmacao.GerarHash(token),
            dataDeInscricao.AddHours(24));
    }

    private sealed class DiretorioTemporario : IDisposable
    {
        public DiretorioTemporario()
        {
            Caminho = Path.Combine(Path.GetTempPath(), $"verbose-testes-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Caminho);
        }

        public string Caminho { get; }

        public void Dispose() => Directory.Delete(Caminho, recursive: true);
    }
}
