using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IEmailService
{
    Task EnviarPalavraDoDiaAsync(
        RegistroPalavraDoDia registro,
        CancellationToken tokenCancelamento = default);
}
