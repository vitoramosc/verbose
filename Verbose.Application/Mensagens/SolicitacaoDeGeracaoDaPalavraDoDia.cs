namespace Verbose.Application.Mensagens;

public sealed class SolicitacaoDeGeracaoDaPalavraDoDia
{
    public SolicitacaoDeGeracaoDaPalavraDoDia(DateOnly dataReferencia)
    {
        if (dataReferencia == default)
        {
            throw new ArgumentException("A data de referência deve ser informada.", nameof(dataReferencia));
        }

        DataReferencia = dataReferencia;
    }

    public DateOnly DataReferencia { get; }
}
