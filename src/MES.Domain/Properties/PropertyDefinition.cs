using MES.Domain.Common;

namespace MES.Domain.Properties;

/// <summary>
/// Definicja cechy, np. siła zwarcia z typem Number i jednostką kN.
/// </summary>
public sealed class PropertyDefinition : Entity
{
    public required string Code { get; set; }
    public required PropertyDataType DataType { get; set; }
    public string? Unit { get; set; }
    public ICollection<PropertyOption> Options { get; } = new List<PropertyOption>();
}
