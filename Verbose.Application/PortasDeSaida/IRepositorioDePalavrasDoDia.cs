using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IRepositorioDePalavrasDoDia
{
    Task<RegistroPalavraDoDia?> ObterPorDataAsync(
        DateOnly dataReferencia,
        CancellationToken tokenCancelamento = default);

    Task<bool> PalavraJaFoiUsadaAsync(
        string palavra,
        CancellationToken tokenCancelamento = default);

    Task SalvarAsync(
        RegistroPalavraDoDia registro,
        CancellationToken tokenCancelamento = default);
}
