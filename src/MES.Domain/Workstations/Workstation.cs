using MES.Domain.Common;

namespace MES.Domain.Workstations;

/// <summary>
/// Stanowisko produkcyjne identyfikowane kodem biznesowym.
/// </summary>
public sealed class Workstation : Entity
{
    /// <summary>
    /// Kod stanowiska, np. MON-01. Unikalność wymaga sprawdzenia podczas zapisu.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Identyfikator jednego rodzaju stanowiska ze słownika WorkstationType.
    /// Ten sam rodzaj może być przypisany do wielu stanowisk.
    /// </summary>
    public required int WorkstationTypeId { get; set; }

    /// <summary>
    /// Dodatkowe właściwości, wypełniane jawnie przez repozytorium.
    /// </summary>
    public ICollection<WorkstationProperty> Properties { get; } = new List<WorkstationProperty>();

}
