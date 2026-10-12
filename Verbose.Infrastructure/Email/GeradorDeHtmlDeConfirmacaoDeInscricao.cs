using System.Net;

namespace Verbose.Infrastructure.Email;

public sealed class GeradorDeHtmlDeConfirmacaoDeInscricao
{
    private const string NomeDoRecurso =
        "Verbose.Infrastructure.Email.Recursos.confirmacao_inscricao_template.html";

    private readonly string template;

    public GeradorDeHtmlDeConfirmacaoDeInscricao()
        : this(CarregarTemplateIncorporado())
    {
    }

    public GeradorDeHtmlDeConfirmacaoDeInscricao(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("O template de confirmação deve ser informado.", nameof(template));
        }

        this.template = template;
    }

    public string Gerar(Uri linkDeConfirmacao)
    {
        ArgumentNullException.ThrowIfNull(linkDeConfirmacao);

        return template.Replace(
            "{{LINK_CONFIRMACAO}}",
            WebUtility.HtmlEncode(linkDeConfirmacao.AbsoluteUri),
            StringComparison.Ordinal);
    }

    private static string CarregarTemplateIncorporado()
    {
        var assembly = typeof(GeradorDeHtmlDeConfirmacaoDeInscricao).Assembly;
        using var fluxo = assembly.GetManifestResourceStream(NomeDoRecurso)
            ?? throw new InvalidOperationException("O template de confirmação incorporado não foi encontrado.");
        using var leitor = new StreamReader(fluxo);

        return leitor.ReadToEnd();
    }
}
