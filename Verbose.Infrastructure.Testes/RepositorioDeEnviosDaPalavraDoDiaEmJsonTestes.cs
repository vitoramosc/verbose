using Verbose.Application.PortasDeSaida;
using Verbose.Infrastructure.Persistencia;

namespace Verbose.Infrastructure.Testes;

public sealed class RepositorioDeEnviosDaPalavraDoDiaEmJsonTestes
{
    [Fact]
    public async Task ObterOuCriarAsync_QuandoEnvioExiste_RetornaStatusPersistido()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        IRepositorioDeEnviosDaPalavraDoDia repositorio =
            new RepositorioDeEnviosDaPalavraDoDiaEmJson(diretorioTemporario.Caminho);
        var dataReferencia = new DateOnly(2026, 10, 16);

        var envio = await repositorio.ObterOuCriarAsync(dataReferencia, "pessoa@exemplo.com");
        envio.MarcarComoEnviado();
        await repositorio.AtualizarAsync(envio);

        var envioRelido = await repositorio.ObterOuCriarAsync(dataReferencia, "PESSOA@EXEMPLO.COM");

        Assert.True(envioRelido.FoiEnviado);
        Assert.Equal("pessoa@exemplo.com", envioRelido.Email);
    }

    [Fact]
    public async Task ObterOuCriarAsync_QuandoJsonInvalido_LancaFalhaExplicita()
    {
        using var diretorioTemporario = new DiretorioTemporario();
        await File.WriteAllTextAsync(
            Path.Combine(diretorioTemporario.Caminho, "envios-palavras.json"),
            "{ conteudo invalido }");
        IRepositorioDeEnviosDaPalavraDoDia repositorio =
            new RepositorioDeEnviosDaPalavraDoDiaEmJson(diretorioTemporario.Caminho);

        var excecao = await Assert.ThrowsAsync<InvalidDataException>(
            () => repositorio.ObterOuCriarAsync(new DateOnly(2026, 10, 16), "pessoa@exemplo.com"));

        Assert.Equal("O arquivo de envios contém JSON inválido.", excecao.Message);
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
