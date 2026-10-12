using Microsoft.AspNetCore.RateLimiting;
using Verbose.Application.CasosDeUso;

namespace Verbose.Worker;

public static class EndpointsDeInscricao
{
    public static IEndpointRouteBuilder MapearEndpointsDeInscricao(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/inscricoes", async (
            SolicitacaoDeInscricao solicitacao,
            CadastrarDestinatario cadastrarDestinatario,
            CancellationToken tokenCancelamento) =>
        {
            if (string.IsNullOrWhiteSpace(solicitacao.Email))
            {
                return Results.BadRequest(new { mensagem = "Informe um e-mail válido." });
            }

            try
            {
                await cadastrarDestinatario.ExecutarAsync(solicitacao.Email, tokenCancelamento);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new { mensagem = "Informe um e-mail válido." });
            }

            return Results.Accepted(value: new
            {
                mensagem = "Recebemos sua solicitação de inscrição."
            });
        })
        .RequireRateLimiting("inscricoes");

        endpoints.MapGet("/api/inscricoes/confirmar", async (
            string? token,
            ConfirmarInscricao confirmarInscricao,
            CancellationToken tokenCancelamento) =>
        {
            var resultado = await confirmarInscricao.ExecutarAsync(token ?? string.Empty, tokenCancelamento);

            return resultado == Verbose.Application.PortasDeSaida.ResultadoDaConfirmacaoDaInscricao.Confirmada
                ? Results.Content(
                    PaginaDeInscricaoConfirmada.ObterHtml(),
                    "text/html; charset=utf-8")
                : Results.BadRequest(new { mensagem = "Este link de confirmação é inválido ou expirou." });
        })
        .RequireRateLimiting("confirmacoes");

        endpoints.MapGet("/api/inscricoes/cancelar", async (
            string? token,
            CancelarInscricao cancelarInscricao,
            CancellationToken tokenCancelamento) =>
        {
            var resultado = await cancelarInscricao.ExecutarAsync(token ?? string.Empty, tokenCancelamento);

            return resultado == Verbose.Application.PortasDeSaida.ResultadoDoCancelamentoDaInscricao.Cancelada
                ? Results.Content(
                    PaginaDeInscricaoCancelada.ObterHtml(),
                    "text/html; charset=utf-8")
                : Results.BadRequest(new { mensagem = "Este link de cancelamento é inválido ou já foi utilizado." });
        })
        .RequireRateLimiting("confirmacoes");

        return endpoints;
    }

    private sealed class SolicitacaoDeInscricao
    {
        public string? Email { get; init; }
    }
}
