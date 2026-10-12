namespace Verbose.Worker;

public static class PaginaDeInscricaoCancelada
{
    private const string NomeDoRecurso = "Verbose.Worker.Recursos.inscricao_cancelada.html";

    public static string ObterHtml()
    {
        var assembly = typeof(PaginaDeInscricaoCancelada).Assembly;
        using var fluxo = assembly.GetManifestResourceStream(NomeDoRecurso)
            ?? throw new InvalidOperationException("A página de inscrição cancelada não foi encontrada.");
        using var leitor = new StreamReader(fluxo);

        return leitor.ReadToEnd();
    }
}
