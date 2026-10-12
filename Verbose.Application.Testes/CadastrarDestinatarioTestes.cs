using Verbose.Application.CasosDeUso;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Application.Testes;

public sealed class CadastrarDestinatarioTestes
{
    [Fact]
    public async Task ExecutarAsync_QuandoEmailInedito_EnviaConfirmacaoEPersisteHash()
    {
        var repositorio = new RepositorioDeDestinatariosEmMemoria();
        var emailService = new EmailServiceFalso();
        var casoDeUso = CriarCasoDeUso(repositorio, emailService);

        var resultado = await casoDeUso.ExecutarAsync("  Pessoa@Exemplo.com ");

        Assert.Equal(ResultadoDoCadastroDeDestinatario.ConfirmacaoEnviada, resultado);
        var destinatario = Assert.Single(repositorio.Destinatarios);
        Assert.Equal("pessoa@exemplo.com", destinatario.Email);
        Assert.Equal(StatusDaInscricao.PendenteConfirmacao, destinatario.Status);
        Assert.NotNull(destinatario.HashDoTokenDeConfirmacao);
        Assert.Equal("pessoa@exemplo.com", emailService.EmailDestinatario);
        Assert.StartsWith("http://localhost:5078/api/inscricoes/confirmar?token=", emailService.LinkDeConfirmacao!.AbsoluteUri);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoEmailPendenteJaExiste_RenovaTokenEReenviaConfirmacao()
    {
        var repositorio = new RepositorioDeDestinatariosEmMemoria();
        var emailService = new EmailServiceFalso();
        var casoDeUso = CriarCasoDeUso(repositorio, emailService);
        await casoDeUso.ExecutarAsync("pessoa@exemplo.com");
        var hashAnterior = Assert.Single(repositorio.Destinatarios).HashDoTokenDeConfirmacao;

        var resultado = await casoDeUso.ExecutarAsync("PESSOA@EXEMPLO.COM");

        Assert.Equal(ResultadoDoCadastroDeDestinatario.ConfirmacaoEnviada, resultado);
        Assert.Single(repositorio.Destinatarios);
        Assert.NotEqual(hashAnterior, Assert.Single(repositorio.Destinatarios).HashDoTokenDeConfirmacao);
        Assert.Equal(2, emailService.QuantidadeDeConfirmacoesEnviadas);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoEmailInvalido_LancaErroExplicito()
    {
        var casoDeUso = CriarCasoDeUso(new RepositorioDeDestinatariosEmMemoria(), new EmailServiceFalso());

        var excecao = await Assert.ThrowsAsync<ArgumentException>(
            async () => await casoDeUso.ExecutarAsync("nao-e-um-email"));

        Assert.Equal("email", excecao.ParamName);
    }

    private static CadastrarDestinatario CriarCasoDeUso(
        IRepositorioDeDestinatarios repositorio,
        IEmailService emailService)
    {
        return new CadastrarDestinatario(
            repositorio,
            emailService,
            new RelogioFixo(new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero)),
            new Uri("http://localhost:5078/"),
            TimeSpan.FromHours(24));
    }

    private sealed class RepositorioDeDestinatariosEmMemoria : IRepositorioDeDestinatarios
    {
        private readonly List<Destinatario> destinatarios = [];

        public IReadOnlyCollection<Destinatario> Destinatarios => destinatarios;

        public Task<IReadOnlyCollection<Destinatario>> ObterConfirmadosAsync(
            CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult<IReadOnlyCollection<Destinatario>>(
                destinatarios.Where(item => item.Status == StatusDaInscricao.Confirmada).ToArray());
        }

        public Task<ResultadoDaPersistenciaDaInscricao> CriarOuRenovarPendenteAsync(
            Destinatario candidato,
            CancellationToken tokenCancelamento = default)
        {
            var existente = destinatarios.SingleOrDefault(item => item.Email == candidato.Email);

            if (existente?.Status == StatusDaInscricao.Confirmada)
            {
                return Task.FromResult(ResultadoDaPersistenciaDaInscricao.JaConfirmada);
            }

            if (existente is null)
            {
                destinatarios.Add(candidato);
            }
            else
            {
                existente.RenovarConfirmacao(
                    candidato.HashDoTokenDeConfirmacao!,
                    candidato.DataDeExpiracaoDaConfirmacao!.Value,
                    candidato.DataDeInscricao);
            }

            return Task.FromResult(ResultadoDaPersistenciaDaInscricao.PendenteDeConfirmacao);
        }

        public Task<ResultadoDaConfirmacaoDaInscricao> ConfirmarPeloHashDoTokenAsync(
            string hashDoToken,
            DateTimeOffset instanteAtual,
            CancellationToken tokenCancelamento = default)
        {
            var destinatario = destinatarios.SingleOrDefault(item =>
                item.ConfirmarSeTokenCorresponder(hashDoToken, instanteAtual));

            return Task.FromResult(destinatario is null
                ? ResultadoDaConfirmacaoDaInscricao.InvalidaOuExpirada
                : ResultadoDaConfirmacaoDaInscricao.Confirmada);
        }

        public Task DefinirTokenDeCancelamentoAsync(
            string email,
            string hashDoToken,
            CancellationToken tokenCancelamento = default) => Task.CompletedTask;

        public Task<ResultadoDoCancelamentoDaInscricao> CancelarPeloHashDoTokenAsync(
            string hashDoToken,
            CancellationToken tokenCancelamento = default)
        {
            return Task.FromResult(ResultadoDoCancelamentoDaInscricao.Invalida);
        }
    }

    private sealed class EmailServiceFalso : IEmailService
    {
        public int QuantidadeDeConfirmacoesEnviadas { get; private set; }

        public string? EmailDestinatario { get; private set; }

        public Uri? LinkDeConfirmacao { get; private set; }

        public Task EnviarPalavraDoDiaAsync(
            RegistroPalavraDoDia registro,
            string destinatario,
            Uri linkDeCancelamento,
            CancellationToken tokenCancelamento = default) => Task.CompletedTask;

        public Task EnviarConfirmacaoDeInscricaoAsync(
            string email,
            Uri linkDeConfirmacao,
            CancellationToken tokenCancelamento = default)
        {
            QuantidadeDeConfirmacoesEnviadas++;
            EmailDestinatario = email;
            LinkDeConfirmacao = linkDeConfirmacao;
            return Task.CompletedTask;
        }
    }

    private sealed class RelogioFixo(DateTimeOffset instante) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instante;
    }
}
