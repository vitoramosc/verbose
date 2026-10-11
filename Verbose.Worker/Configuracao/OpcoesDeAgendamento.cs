using System.Globalization;

namespace Verbose.Worker.Configuracao;

public sealed class OpcoesDeAgendamento
{
    public const string NomeSecao = "Agendamento";

    public string Horario { get; init; } = string.Empty;

    public string FusoHorario { get; init; } = string.Empty;

    public bool ExecutarImediatamente => string.Equals(
        Horario,
        "agora",
        StringComparison.OrdinalIgnoreCase);

    public bool TentarObterHorario(out TimeOnly horario)
    {
        return TimeOnly.TryParseExact(
            Horario,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out horario);
    }
}
