namespace Verbose.Worker;

public static class PaginaDeInscricaoConfirmada
{
    private const string NomeDoRecurso = "Verbose.Worker.Recursos.inscricao_confirmada.html";

    public static string ObterHtml()
    {
        var assembly = typeof(PaginaDeInscricaoConfirmada).Assembly;
        using var fluxo = assembly.GetManifestResourceStream(NomeDoRecurso)
            ?? throw new InvalidOperationException("A página de inscrição confirmada não foi encontrada.");
        using var leitor = new StreamReader(fluxo);

        return leitor.ReadToEnd();
    }
}
