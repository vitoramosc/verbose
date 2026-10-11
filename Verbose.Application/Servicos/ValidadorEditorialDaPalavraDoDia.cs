using System.Text.RegularExpressions;
using Verbose.Domain;

namespace Verbose.Application.Servicos;

public sealed class ValidadorEditorialDaPalavraDoDia
{
    public void Validar(PalavraDoDia palavraDoDia)
    {
        ArgumentNullException.ThrowIfNull(palavraDoDia);

        var padrao = $"(?<!\\p{{L}}){Regex.Escape(palavraDoDia.Palavra)}(?!\\p{{L}})";

        if (!Regex.IsMatch(
                palavraDoDia.Exemplo,
                padrao,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            throw new ArgumentException(
                "O exemplo deve conter exatamente a palavra escolhida.",
                nameof(palavraDoDia));
        }
    }
}
