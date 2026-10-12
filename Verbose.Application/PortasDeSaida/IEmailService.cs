using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IEmailService
{
    Task EnviarPalavraDoDiaAsync(
        RegistroPalavraDoDia registro,
        string destinatario,
        Uri linkDeCancelamento,
        CancellationToken tokenCancelamento = default);

    Task EnviarConfirmacaoDeInscricaoAsync(
        string email,
        Uri linkDeConfirmacao,
        CancellationToken tokenCancelamento = default);
}
