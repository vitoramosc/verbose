using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Application.CasosDeUso;

public sealed class RegistrarPalavraDoDia(IRepositorioDePalavrasDoDia repositorio)
{
    public async Task<ResultadoDoRegistroDaPalavraDoDia> ExecutarAsync(
        DateOnly dataReferencia,
        PalavraDoDia palavraDoDia,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(palavraDoDia);

        var registroExistente = await repositorio.ObterPorDataAsync(dataReferencia, tokenCancelamento);

        if (registroExistente is not null)
        {
            return ResultadoDoRegistroDaPalavraDoDia.DataJaUtilizada;
        }

        var palavraJaFoiUsada = await repositorio.PalavraJaFoiUsadaAsync(
            palavraDoDia.Palavra,
            tokenCancelamento);

        if (palavraJaFoiUsada)
        {
            return ResultadoDoRegistroDaPalavraDoDia.PalavraJaUtilizada;
        }

        var registro = new RegistroPalavraDoDia(dataReferencia, palavraDoDia);
        await repositorio.SalvarAsync(registro, tokenCancelamento);

        return ResultadoDoRegistroDaPalavraDoDia.Registrada;
    }
}
