using Verbose.Application.PortasDeSaida;
using Verbose.Application.Servicos;

namespace Verbose.Application.CasosDeUso;

public sealed class GerarERegistrarPalavraDoDia(
    IRepositorioDePalavrasDoDia repositorio,
    IGeradorDePalavraDoDia gerador,
    IRevisorEditorialDePalavraDoDia revisor,
    ValidadorEditorialDaPalavraDoDia validadorEditorial,
    RegistrarPalavraDoDia registrar)
{
    private const int MaximoDeTentativasEditoriais = 3;

    public async Task<ResultadoDoRegistroDaPalavraDoDia> ExecutarAsync(
        DateOnly dataReferencia,
        CancellationToken tokenCancelamento = default)
    {
        if (await repositorio.ObterPorDataAsync(dataReferencia, tokenCancelamento) is not null)
        {
            return ResultadoDoRegistroDaPalavraDoDia.DataJaUtilizada;
        }

        var palavrasParaEvitar = (await repositorio.ObterPalavrasUsadasAsync(tokenCancelamento)).ToList();

        for (var tentativa = 1; tentativa <= MaximoDeTentativasEditoriais; tentativa++)
        {
            var palavraDoDia = await gerador.GerarAsync(palavrasParaEvitar, tokenCancelamento);
            try
            {
                validadorEditorial.Validar(palavraDoDia);
            }
            catch (ArgumentException)
            {
                palavrasParaEvitar.Add(palavraDoDia.Palavra);
                continue;
            }

            var revisao = await revisor.RevisarAsync(palavraDoDia, tokenCancelamento);

            if (revisao.Aprovada)
            {
                return await registrar.ExecutarAsync(dataReferencia, palavraDoDia, tokenCancelamento);
            }

            palavrasParaEvitar.Add(palavraDoDia.Palavra);
        }

        throw new InvalidOperationException(
            $"O Ollama não gerou uma palavra aprovada após {MaximoDeTentativasEditoriais} tentativas editoriais.");
    }
}
