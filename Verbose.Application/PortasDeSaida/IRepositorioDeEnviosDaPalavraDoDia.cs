using Verbose.Domain;

namespace Verbose.Application.PortasDeSaida;

public interface IRepositorioDeEnviosDaPalavraDoDia
{
    Task<EnvioDaPalavraDoDia> ObterOuCriarAsync(DateOnly dataReferencia, string email, CancellationToken tokenCancelamento = default);
    Task AtualizarAsync(EnvioDaPalavraDoDia envio, CancellationToken tokenCancelamento = default);
}
