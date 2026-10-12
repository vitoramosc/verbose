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
        var repositorioDeEnvios = new RepositorioDeEnviosEmMemoria();
        var servico = CriarServico(repositorio, repositorioDeEnvios, emailService);

        await servico.EnviarPalavraDoDiaAsync(registro.DataReferencia);

        Assert.True(emailService.FoiEnviado);
        Assert.Equal("pessoa@exemplo.com", emailService.Destinatario);
        Assert.Equal(StatusDeEnvio.Enviada, registro.StatusDeEnvio);
        Assert.True(repositorio.FoiAtualizado);
        Assert.True(Assert.Single(repositorioDeEnvios.Envios).FoiEnviado);
    }

    [Fact]
    public async Task EnviarPalavraDoDiaAsync_QuandoEmailFalha_MantemRegistroPendente()
    {
        var registro = CriarRegistro();
        var repositorio = new RepositorioEmMemoria(registro);
        var servico = CriarServico(
            repositorio,
            new RepositorioDeEnviosEmMemoria(),
            new EmailServiceQueFalha());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servico.EnviarPalavraDoDiaAsync(registro.DataReferencia));

        Assert.Equal(StatusDeEnvio.PendenteEnvio, registro.StatusDeEnvio);
        Assert.False(repositorio.FoiAtualizado);
    }

    [Fact]
    public async Task EnviarPalavraDoDiaAsync_QuandoDestinatarioJaRecebeu_NaoReenvia()
    {
        var registro = CriarRegistro();
        var repositorioDeEnvios = new RepositorioDeEnviosEmMemoria();
        var envio = await repositorioDeEnvios.ObterOuCriarAsync(registro.DataReferencia, "pessoa@exemplo.com");
        envio.MarcarComoEnviado();
        await repositorioDeEnvios.AtualizarAsync(envio);
        var emailService = new EmailServiceFalso();
        var servico = CriarServico(new RepositorioEmMemoria(registro), repositorioDeEnvios, emailService);

        await servico.EnviarPalavraDoDiaAsync(registro.DataReferencia);

        Assert.False(emailService.FoiEnviado);
        Assert.Equal(StatusDeEnvio.Enviada, registro.StatusDeEnvio);
    }

    private static NotificacaoService CriarServico(
        IRepositorioDePalavrasDoDia repositorioDePalavras,
        IRepositorioDeEnviosDaPalavraDoDia repositorioDeEnvios,
        IEmailService emailService)
    {
        return new NotificacaoService(
            repositorioDePalavras,
            new RepositorioDeDestinatariosFalso(),
            repositorioDeEnvios,
            emailService,
            new Uri("https://verbose.test/"));
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

        public string? Destinatario { get; private set; }

        public Task EnviarPalavraDoDiaAsync(
            RegistroPalavraDoDia registro,
            string destinatario,
            Uri linkDeCancelamento,
            CancellationToken tokenCancelamento = default)
        {
            FoiEnviado = true;
            Destinatario = destinatario;
            return Task.CompletedTask;
        }

        public Task EnviarConfirmacaoDeInscricaoAsync(string email, Uri linkDeConfirmacao, CancellationToken tokenCancelamento = default)
            => Task.CompletedTask;
    }

    private sealed class EmailServiceQueFalha : IEmailService
    {
        public Task EnviarPalavraDoDiaAsync(
            RegistroPalavraDoDia registro,
            string destinatario,
            Uri linkDeCancelamento,
            CancellationToken tokenCancelamento = default)
            => Task.FromException(new InvalidOperationException("Falha SMTP simulada."));

        public Task EnviarConfirmacaoDeInscricaoAsync(string email, Uri linkDeConfirmacao, CancellationToken tokenCancelamento = default)
            => Task.FromException(new InvalidOperationException("Falha SMTP simulada."));
    }

    private sealed class RepositorioDeDestinatariosFalso : IRepositorioDeDestinatarios
    {
        private static readonly Destinatario[] Destinatarios =
        [
            Destinatario.Reidratar(
                "pessoa@exemplo.com",
                new DateTimeOffset(2026, 10, 14, 8, 0, 0, TimeSpan.Zero),
                StatusDaInscricao.Confirmada,
                hashDoTokenDeConfirmacao: null,
                dataDeExpiracaoDaConfirmacao: null,
                hashDoTokenDeCancelamento: null)
        ];

        public Task<IReadOnlyCollection<Destinatario>> ObterConfirmadosAsync(
            CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult<IReadOnlyCollection<Destinatario>>(Destinatarios);
        }

        public Task<ResultadoDaPersistenciaDaInscricao> CriarOuRenovarPendenteAsync(
            Destinatario destinatario,
            CancellationToken tokenCancelamento = default) => throw new NotSupportedException();

        public Task<ResultadoDaConfirmacaoDaInscricao> ConfirmarPeloHashDoTokenAsync(
            string hashDoToken,
            DateTimeOffset instanteAtual,
            CancellationToken tokenCancelamento = default) => throw new NotSupportedException();

        public Task DefinirTokenDeCancelamentoAsync(
            string email,
            string hashDoToken,
            CancellationToken tokenCancelamento = default)
        {
            var destinatario = Destinatarios.Single(item => item.Email == email);
            destinatario.DefinirTokenDeCancelamento(hashDoToken);
            return Task.CompletedTask;
        }

        public Task<ResultadoDoCancelamentoDaInscricao> CancelarPeloHashDoTokenAsync(
            string hashDoToken,
            CancellationToken tokenCancelamento = default) => throw new NotSupportedException();
    }

    private sealed class RepositorioDeEnviosEmMemoria : IRepositorioDeEnviosDaPalavraDoDia
    {
        private readonly List<EnvioDaPalavraDoDia> envios = [];

        public IReadOnlyCollection<EnvioDaPalavraDoDia> Envios => envios;

        public Task<EnvioDaPalavraDoDia> ObterOuCriarAsync(
            DateOnly dataReferencia,
            string email,
            CancellationToken tokenCancelamento = default)
        {
            var envio = envios.SingleOrDefault(item =>
                item.DataReferencia == dataReferencia &&
                item.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (envio is null)
            {
                envio = new EnvioDaPalavraDoDia(dataReferencia, email);
                envios.Add(envio);
            }

            return Task.FromResult(envio);
        }

        public Task AtualizarAsync(
            EnvioDaPalavraDoDia envio,
            CancellationToken tokenCancelamento = default) => Task.CompletedTask;
    }
}
