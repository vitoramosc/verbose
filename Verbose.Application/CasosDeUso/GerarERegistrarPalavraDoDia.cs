using Verbose.Application.PortasDeSaida;

namespace Verbose.Application.CasosDeUso;

public sealed class GerarERegistrarPalavraDoDia(
    IRepositorioDePalavrasDoDia repositorio,
    IGeradorDePalavraDoDia gerador,
    RegistrarPalavraDoDia registrar)
{
    public async Task<ResultadoDoRegistroDaPalavraDoDia> ExecutarAsync(
        DateOnly dataReferencia,
        CancellationToken tokenCancelamento = default)
    {
        if (await repositorio.ObterPorDataAsync(dataReferencia, tokenCancelamento) is not null)
        {
            return ResultadoDoRegistroDaPalavraDoDia.DataJaUtilizada;
        }

        var palavrasJaUsadas = await repositorio.ObterPalavrasUsadasAsync(tokenCancelamento);
        var palavraDoDia = await gerador.GerarAsync(palavrasJaUsadas, tokenCancelamento);

        return await registrar.ExecutarAsync(dataReferencia, palavraDoDia, tokenCancelamento);
    }
}
