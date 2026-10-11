using Verbose.Application.PortasDeSaida;
using Verbose.Application.Servicos;
using Verbose.Domain;

namespace Verbose.Application.Testes;

public sealed class NotificacaoServiceTestes
{
    [Fact]
    public async Task EnviarPalavraDoDiaAsync_QuandoRegistroEstaPendente_EnviaEAtualizaStatus()
    {
        var registro = CriarRegistro();
        var repositorio = new RepositorioEmMemoria(registro);
        var emailService = new EmailServiceFalso();
        var servico = new NotificacaoService(repositorio, emailService);

        await servico.EnviarPalavraDoDiaAsync(registro.DataReferencia);

        Assert.True(emailService.FoiEnviado);
        Assert.Equal(StatusDeEnvio.Enviada, registro.StatusDeEnvio);
        Assert.True(repositorio.FoiAtualizado);
    }

    [Fact]
    public async Task EnviarPalavraDoDiaAsync_QuandoEmailFalha_MantemRegistroPendente()
    {
        var registro = CriarRegistro();
        var repositorio = new RepositorioEmMemoria(registro);
        var servico = new NotificacaoService(repositorio, new EmailServiceQueFalha());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servico.EnviarPalavraDoDiaAsync(registro.DataReferencia));

        Assert.Equal(StatusDeEnvio.PendenteEnvio, registro.StatusDeEnvio);
        Assert.False(repositorio.FoiAtualizado);
    }

    private static RegistroPalavraDoDia CriarRegistro()
    {
        return new RegistroPalavraDoDia(
            new DateOnly(2026, 10, 14),
            new PalavraDoDia("idôneo", "Adequado e confiável.", "O candidato é idôneo."));
    }

    private sealed class RepositorioEmMemoria(RegistroPalavraDoDia registro) : IRepositorioDePalavrasDoDia
    {
        public bool FoiAtualizado { get; private set; }

        public Task<RegistroPalavraDoDia?> ObterPorDataAsync(DateOnly dataReferencia, CancellationToken tokenCancelamento = default)
            => Task.FromResult<RegistroPalavraDoDia?>(registro.DataReferencia == dataReferencia ? registro : null);

        public Task<bool> PalavraJaFoiUsadaAsync(string palavra, CancellationToken tokenCancelamento = default)
            => Task.FromResult(false);

        public Task<IReadOnlyCollection<string>> ObterPalavrasUsadasAsync(CancellationToken tokenCancelamento = default)
            => Task.FromResult<IReadOnlyCollection<string>>([registro.PalavraDoDia.Palavra]);

        public Task SalvarAsync(RegistroPalavraDoDia novoRegistro, CancellationToken tokenCancelamento = default)
            => Task.CompletedTask;

        public Task AtualizarAsync(RegistroPalavraDoDia registroAtualizado, CancellationToken tokenCancelamento = default)
        {
            FoiAtualizado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class EmailServiceFalso : IEmailService
    {
        public bool FoiEnviado { get; private set; }

        public Task EnviarPalavraDoDiaAsync(RegistroPalavraDoDia registro, CancellationToken tokenCancelamento = default)
        {
            FoiEnviado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class EmailServiceQueFalha : IEmailService
    {
        public Task EnviarPalavraDoDiaAsync(RegistroPalavraDoDia registro, CancellationToken tokenCancelamento = default)
            => Task.FromException(new InvalidOperationException("Falha SMTP simulada."));
    }
}
