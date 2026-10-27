using MES.Domain.Common;

namespace MES.Domain.Workstations;

/// <summary>
/// Słownik rodzajów stanowisk, np. wtryskarka, stanowisko montażowe,
/// lakiernia lub stanowisko kontroli. Nazwa, opis i audyt pochodzą z Entity.
/// </summary>
public sealed class WorkstationType : Entity
{
    public ICollection<WorkstationTypeProperty> Properties { get; } = new List<WorkstationTypeProperty>();
}
