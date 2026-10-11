using Verbose.Application.Mensagens;

namespace Verbose.Application.PortasDeSaida;

public interface IPublicadorDeSolicitacaoDePalavraDoDia
{
    Task PublicarAsync(
        SolicitacaoDeGeracaoDaPalavraDoDia solicitacao,
        CancellationToken tokenCancelamento = default);
}
