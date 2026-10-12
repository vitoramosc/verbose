using Verbose.Application.PortasDeSaida;

namespace Verbose.Application.CasosDeUso;

public sealed class ConfirmarInscricao(
    IRepositorioDeDestinatarios repositorio,
    TimeProvider relogio)
{
    public Task<ResultadoDaConfirmacaoDaInscricao> ExecutarAsync(
        string token,
        CancellationToken tokenCancelamento = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
        {
            return Task.FromResult(ResultadoDaConfirmacaoDaInscricao.InvalidaOuExpirada);
        }

        var hashDoToken = ProtecaoDoTokenDeConfirmacao.GerarHash(token);
        return repositorio.ConfirmarPeloHashDoTokenAsync(
            hashDoToken,
            relogio.GetUtcNow(),
            tokenCancelamento);
    }
}
