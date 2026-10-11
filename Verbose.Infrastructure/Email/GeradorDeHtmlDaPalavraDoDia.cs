using System.Globalization;
using System.Net;
using System.Reflection;
using Verbose.Domain;

namespace Verbose.Infrastructure.Email;

public sealed class GeradorDeHtmlDaPalavraDoDia
{
    private const string NomeDoRecurso =
        "Verbose.Infrastructure.Email.Recursos.palavra_do_dia_template.html";

    private readonly string template;

    public GeradorDeHtmlDaPalavraDoDia()
        : this(CarregarTemplateIncorporado())
    {
    }

    public GeradorDeHtmlDaPalavraDoDia(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("O template de e-mail deve ser informado.", nameof(template));
        }

        this.template = template;
    }

    public string Gerar(RegistroPalavraDoDia registro)
    {
        ArgumentNullException.ThrowIfNull(registro);

        return template
            .Replace("{{PALAVRA}}", Codificar(registro.PalavraDoDia.Palavra), StringComparison.Ordinal)
            .Replace("{{DEFINICAO}}", Codificar(registro.PalavraDoDia.Definicao), StringComparison.Ordinal)
            .Replace("{{EXEMPLO}}", Codificar(registro.PalavraDoDia.Exemplo), StringComparison.Ordinal)
            .Replace(
                "{{data}}",
                Codificar(registro.DataReferencia.ToString("d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("pt-BR"))),
                StringComparison.Ordinal);
    }

    private static string CarregarTemplateIncorporado()
    {
        var assembly = typeof(GeradorDeHtmlDaPalavraDoDia).Assembly;
        using var fluxo = assembly.GetManifestResourceStream(NomeDoRecurso)
            ?? throw new InvalidOperationException("O template de e-mail incorporado não foi encontrado.");
        using var leitor = new StreamReader(fluxo);

        return leitor.ReadToEnd();
    }

    private static string Codificar(string valor) => WebUtility.HtmlEncode(valor);
}
