using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IRevisorEditorialDePalavraDoDia
{
    Task<ResultadoDaRevisaoEditorial> RevisarAsync(
        PalavraDoDia palavraDoDia,
        CancellationToken tokenCancelamento = default);
}
