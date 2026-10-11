using Verbose.Application.PortasDeSaida;
using Verbose.Domain;
using Verbose.Infrastructure.Persistencia;

namespace Verbose.Infrastructure.Testes;

public sealed class RepositorioDePalavrasEmJsonTestes
{
    [Fact]
    public async Task ObterTodosAsync_QuandoArquivoNaoExiste_RetornaListaVazia()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDePalavrasDoDia repositorio =
            new RepositorioDePalavrasEmJson(diretorioTemporario.Caminho);

        var registro = await repositorio.ObterPorDataAsync(new DateOnly(2026, 10, 10));

        Assert.Null(registro);
    }

    [Fact]
    public async Task SalvarAsync_QuandoRegistroValido_PersisteERecuperaOsDados()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDePalavrasDoDia repositorio =
            new RepositorioDePalavrasEmJson(diretorioTemporario.Caminho);
        var palavraDoDia = new PalavraDoDia(
            "irascível",
            "Que se irrita com facilidade; propenso à ira.",
            "Mesmo diante de uma crítica simples, ele ficou irascível.");
        var registro = new RegistroPalavraDoDia(new DateOnly(2026, 10, 10), palavraDoDia);
        registro.MarcarComoEnviada();

        await repositorio.SalvarAsync(registro);

        var caminhoArquivo = Path.Combine(diretorioTemporario.Caminho, "used-words.json");
        var conteudoDoArquivo = await File.ReadAllTextAsync(caminhoArquivo);
        var registroRecuperado = await repositorio.ObterPorDataAsync(registro.DataReferencia);
        var palavraJaFoiUsada = await repositorio.PalavraJaFoiUsadaAsync("  IRASCÍVEL  ");

        Assert.Contains("\"statusDeEnvio\": \"Enviada\"", conteudoDoArquivo);
        Assert.NotNull(registroRecuperado);
        Assert.True(palavraJaFoiUsada);
        Assert.Equal(registro.DataReferencia, registroRecuperado.DataReferencia);
        Assert.Equal(registro.PalavraDoDia.Palavra, registroRecuperado.PalavraDoDia.Palavra);
        Assert.Equal(registro.PalavraDoDia.Definicao, registroRecuperado.PalavraDoDia.Definicao);
        Assert.Equal(registro.PalavraDoDia.Exemplo, registroRecuperado.PalavraDoDia.Exemplo);
        Assert.Equal(StatusDeEnvio.Enviada, registroRecuperado.StatusDeEnvio);
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoJsonInvalido_LancaFalhaExplicita()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        var caminhoArquivo = Path.Combine(diretorioTemporario.Caminho, "used-words.json");
        await File.WriteAllTextAsync(caminhoArquivo, "{ conteudo invalido }");
        IRepositorioDePalavrasDoDia repositorio =
            new RepositorioDePalavrasEmJson(diretorioTemporario.Caminho);

        var excecao = await Assert.ThrowsAsync<InvalidDataException>(
            async () => await repositorio.ObterPorDataAsync(new DateOnly(2026, 10, 10)));

        Assert.Equal("O arquivo de palavras usadas contém JSON inválido.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoRegistroExiste_PersisteNovoStatus()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDePalavrasDoDia repositorio =
            new RepositorioDePalavrasEmJson(diretorioTemporario.Caminho);
        var registro = new RegistroPalavraDoDia(
            new DateOnly(2026, 10, 10),
            new PalavraDoDia("irascível", "Definição válida.", "Exemplo válido."));
        await repositorio.SalvarAsync(registro);

        registro.MarcarComoEnviada();
        await repositorio.AtualizarAsync(registro);

        var registroAtualizado = await repositorio.ObterPorDataAsync(registro.DataReferencia);

        Assert.NotNull(registroAtualizado);
        Assert.Equal(StatusDeEnvio.Enviada, registroAtualizado.StatusDeEnvio);
    }

    private sealed class DiretorioTemporario : IDisposable
    {
        public DiretorioTemporario()
        {
            Caminho = Path.Combine(Path.GetTempPath(), $"verbose-testes-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Caminho);
        }

        public string Caminho { get; }

        public void Dispose()
        {
            Directory.Delete(Caminho, recursive: true);
        }
    }
}
