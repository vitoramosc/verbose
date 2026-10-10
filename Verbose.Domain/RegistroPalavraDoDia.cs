namespace Verbose.Domain;

public sealed class RegistroPalavraDoDia
{
    public RegistroPalavraDoDia(DateOnly dataReferencia, PalavraDoDia palavraDoDia)
    {
        if (palavraDoDia is null)
        {
            throw new ArgumentNullException(nameof(palavraDoDia));
        }

        DataReferencia = dataReferencia;
        PalavraDoDia = palavraDoDia;
        StatusDeEnvio = StatusDeEnvio.PendenteEnvio;
    }

    public DateOnly DataReferencia { get; }

    public PalavraDoDia PalavraDoDia { get; }

    public StatusDeEnvio StatusDeEnvio { get; private set; }

    public void MarcarComoEnviada()
    {
        StatusDeEnvio = StatusDeEnvio.Enviada;
    }
}
