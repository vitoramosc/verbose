using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Ollama;

public sealed class RevisorEditorialDePalavraDoDiaNoOllama(
    HttpClient clienteHttp,
    OpcoesDoOllama opcoes,
    ILogger<RevisorEditorialDePalavraDoDiaNoOllama> logger)
    : IRevisorEditorialDePalavraDoDia
{
    public async Task<ResultadoDaRevisaoEditorial> RevisarAsync(
        PalavraDoDia palavraDoDia,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(palavraDoDia);

        var requisicao = new
        {
            model = opcoes.Modelo,
            stream = false,
            think = false,
            format = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "approved", "reason" },
                properties = new
                {
                    approved = new { type = "boolean" },
                    reason = new { type = "string" }
                }
            },
            options = new { temperature = 0.0 },
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = CriarInstrucao(palavraDoDia)
                }
            }
        };

        using var respostaHttp = await clienteHttp.PostAsJsonAsync(
            "api/chat",
            requisicao,
            tokenCancelamento);
        respostaHttp.EnsureSuccessStatusCode();

        var resposta = await respostaHttp.Content.ReadFromJsonAsync<RespostaDoChat>(
            cancellationToken: tokenCancelamento)
            ?? throw new InvalidDataException("O Ollama retornou uma revisão editorial vazia.");

        if (string.IsNullOrWhiteSpace(resposta.Message?.Content))
        {
            throw new InvalidDataException("O Ollama não retornou conteúdo para a revisão editorial.");
        }

        var conteudo = JsonSerializer.Deserialize<ConteudoRevisado>(
            resposta.Message.Content,
            JsonSerializerOptions.Web)
            ?? throw new InvalidDataException("O Ollama retornou JSON sem revisão editorial.");

        if (string.IsNullOrWhiteSpace(conteudo.Reason))
        {
            throw new InvalidDataException("O Ollama não informou o motivo da revisão editorial.");
        }

        var resultado = new ResultadoDaRevisaoEditorial(conteudo.Approved, conteudo.Reason.Trim());

        logger.LogInformation(
            "Revisão editorial para {Palavra}: {Aprovada}. Motivo: {Motivo}",
            palavraDoDia.Palavra,
            resultado.Aprovada ? "aprovada" : "reprovada",
            resultado.Motivo);

        return resultado;
    }

    private static string CriarInstrucao(PalavraDoDia palavraDoDia)
    {
        return $$"""
            Atue como revisor editorial rigoroso de vocabulário em português brasileiro.
            Avalie a candidata abaixo para uma palavra diária.

            Palavra: {{palavraDoDia.Palavra}}
            Definição: {{palavraDoDia.Definicao}}
            Exemplo: {{palavraDoDia.Exemplo}}

            Aprove somente se a palavra for relativamente incomum, útil e atual; se a definição estiver lexicalmente correta, sem erros ortográficos; e se o exemplo for natural e usar exatamente a palavra informada.
            Reprove palavras muito comuns, termos arcaicos, definição imprecisa, erro ortográfico ou exemplo inadequado.
            Responda somente JSON válido, sem markdown ou texto adicional, usando exatamente os campos booleano "approved" e texto curto "reason".
            """;
    }

    private sealed class RespostaDoChat
    {
        public MensagemDoChat? Message { get; init; }
    }

    private sealed class MensagemDoChat
    {
        public string? Content { get; init; }
    }

    private sealed class ConteudoRevisado
    {
        public bool Approved { get; init; }

        public required string Reason { get; init; }
    }
}
