using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IGeradorDePalavraDoDia
{
    Task<PalavraDoDia> GerarAsync(
        IReadOnlyCollection<string> palavrasJaUsadas,
        CancellationToken tokenCancelamento = default);
}
