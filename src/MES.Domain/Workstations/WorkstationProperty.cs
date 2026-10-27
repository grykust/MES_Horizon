using MES.Domain.Common;

namespace MES.Domain.Workstations;

/// <summary>
/// Pojedyncza wartość cechy stanowiska. Przy zapisie należy wymagać dokładnie
/// jednej wartości zgodnej z typem definicji oraz opcji należącej do tej definicji.
/// Brak wartości oznacza brak aktywnego przypisania.
/// </summary>
public sealed class WorkstationProperty : AuditableEntity
{
    public required int WorkstationId { get; set; }
    public required int PropertyId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public int? OptionId { get; set; }

    /// <summary>
    /// Wybrane opcje dla typu MultiOption. Pozostałe pola wartości pozostają puste.
    /// </summary>
    public List<int> OptionIds { get; set; } = [];
}
