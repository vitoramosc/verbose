namespace Verbose.Domain;

public sealed class EnvioDaPalavraDoDia
{
    public EnvioDaPalavraDoDia(DateOnly dataReferencia, string email)
    {
        if (dataReferencia == default) throw new ArgumentException("A data de referência deve ser informada.", nameof(dataReferencia));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("O e-mail deve ser informado.", nameof(email));
        DataReferencia = dataReferencia;
        Email = email.Trim().ToLowerInvariant();
    }

    public DateOnly DataReferencia { get; }
    public string Email { get; }
    public bool FoiEnviado { get; private set; }
    public void MarcarComoEnviado() => FoiEnviado = true;
}
