namespace Verbose.Domain;

public sealed class PalavraDoDia
{
    public PalavraDoDia(string palavra, string definicao, string exemplo)
    {
        Palavra = ValidarCampo(palavra, nameof(palavra), 60);
        ValidarPalavra(Palavra);
        Definicao = ValidarCampo(definicao, nameof(definicao), 400);
        Exemplo = ValidarCampo(exemplo, nameof(exemplo), 500);
    }

    public string Palavra { get; }

    public string Definicao { get; }

    public string Exemplo { get; }

    private static string ValidarCampo(string valor, string nomeCampo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("O campo não pode estar vazio.", nomeCampo);
        }

        var valorNormalizado = valor.Trim();

        if (valorNormalizado.Length > tamanhoMaximo)
        {
            throw new ArgumentException(
                $"O campo deve ter no máximo {tamanhoMaximo} caracteres.",
                nomeCampo);
        }

        return valorNormalizado;
    }

    private static void ValidarPalavra(string palavra)
    {
        if (palavra.Any(char.IsWhiteSpace) || palavra.Any(caractere => !char.IsLetter(caractere) && caractere != '-'))
        {
            throw new ArgumentException(
                "A palavra deve ser um único lema formado apenas por letras ou hífen.",
                nameof(palavra));
        }
    }
}
