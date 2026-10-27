using MES.Domain.Common;

namespace MES.Domain.Properties;

/// <summary>
/// Dopuszczalna opcja właściwości słownikowej; etykieta znajduje się w Name.
/// </summary>
public sealed class PropertyOption : Entity
{
    public required int PropertyId { get; set; }
    public required string Code { get; set; }
}
