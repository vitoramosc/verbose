using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IRepositorioDeDestinatarios
{
    Task<IReadOnlyCollection<Destinatario>> ObterConfirmadosAsync(
        CancellationToken tokenCancelamento = default);

    Task<ResultadoDaPersistenciaDaInscricao> CriarOuRenovarPendenteAsync(
        Destinatario destinatario,
        CancellationToken tokenCancelamento = default);

    Task<ResultadoDaConfirmacaoDaInscricao> ConfirmarPeloHashDoTokenAsync(
        string hashDoToken,
        DateTimeOffset instanteAtual,
        CancellationToken tokenCancelamento = default);

    Task DefinirTokenDeCancelamentoAsync(
        string email,
        string hashDoToken,
        CancellationToken tokenCancelamento = default);

    Task<ResultadoDoCancelamentoDaInscricao> CancelarPeloHashDoTokenAsync(
        string hashDoToken,
        CancellationToken tokenCancelamento = default);
}
