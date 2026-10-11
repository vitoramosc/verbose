using System.Net.Http.Json;
using System.Text.Json;
using Verbose.Application.PortasDeSaida;
using Verbose.Domain;

namespace Verbose.Infrastructure.Ollama;

public sealed class GeradorDePalavraDoDiaNoOllama(HttpClient clienteHttp, OpcoesDoOllama opcoes)
    : IGeradorDePalavraDoDia
{
    public async Task<PalavraDoDia> GerarAsync(
        IReadOnlyCollection<string> palavrasJaUsadas,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(palavrasJaUsadas);

        var requisicao = new
        {
            model = opcoes.Modelo,
            stream = false,
            think = false,
            format = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "word", "definition", "example" },
                properties = new
                {
                    word = new { type = "string" },
                    definition = new { type = "string" },
                    example = new { type = "string" }
                }
            },
            options = new { temperature = 0.2 },
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = CriarInstrucao(palavrasJaUsadas)
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
            ?? throw new InvalidDataException("O Ollama retornou uma resposta vazia.");

        if (string.IsNullOrWhiteSpace(resposta.Message?.Content))
        {
            throw new InvalidDataException("O Ollama não retornou conteúdo para a palavra do dia.");
        }

        var conteudo = JsonSerializer.Deserialize<ConteudoGerado>(
            resposta.Message.Content,
            JsonSerializerOptions.Web)
            ?? throw new InvalidDataException("O Ollama retornou JSON sem conteúdo.");

        return new PalavraDoDia(conteudo.Word, conteudo.Definition, conteudo.Example);
    }

    private static string CriarInstrucao(IReadOnlyCollection<string> palavrasJaUsadas)
    {
        var palavras = palavrasJaUsadas.Count == 0
            ? "Nenhuma palavra foi usada ainda."
            : string.Join(", ", palavrasJaUsadas);

        return $$"""
            Escolha um único lema do português brasileiro relativamente incomum, útil e atual.
            Prefira substantivos ou adjetivos de uso formal contemporâneo, como o nível de "irascível", "lacônico" ou "idôneo".
            Evite palavras muito frequentes no cotidiano, verbos genéricos, formas flexionadas, nomes próprios, estrangeirismos, termos ofensivos, sexualizados, ambíguos e palavras excessivamente arcaicas.
            Avalie internamente mais de uma candidata e escolha a menos comum que ainda seja útil no português brasileiro atual.
            Forneça definição lexical precisa para o lema escolhido e um exemplo natural que use exatamente essa palavra.
            Não escolha nenhuma palavra já utilizada nesta lista: {{palavras}}
            Responda somente com JSON válido, sem markdown ou texto adicional, usando exatamente os campos "word", "definition" e "example".
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

    private sealed class ConteudoGerado
    {
        public required string Word { get; init; }

        public required string Definition { get; init; }

        public required string Example { get; init; }
    }
}
