using MES.Domain.Common;

namespace MES.Domain.Workstations;

/// <summary>
/// Dostępność i wymagalność właściwości dla określonego rodzaju stanowiska.
/// </summary>
public sealed class WorkstationTypeProperty : AuditableEntity
{
    public required int WorkstationTypeId { get; set; }
    public required int PropertyId { get; set; }
    public bool IsRequired { get; set; }
}
