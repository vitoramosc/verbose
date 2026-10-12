using Verbose.Application.PortasDeSaida;

namespace Verbose.Application.CasosDeUso;

public sealed class CancelarInscricao(IRepositorioDeDestinatarios repositorio)
{
    public Task<ResultadoDoCancelamentoDaInscricao> ExecutarAsync(
        string token,
        CancellationToken tokenCancelamento = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(ResultadoDoCancelamentoDaInscricao.Invalida);
        }

        return repositorio.CancelarPeloHashDoTokenAsync(
            ProtecaoDoTokenDeConfirmacao.GerarHash(token),
            tokenCancelamento);
    }
}
