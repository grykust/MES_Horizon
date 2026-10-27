namespace MES.Domain.Common;

/// <summary>
/// Metadane utworzenia, ostatniej modyfikacji i miękkiego usunięcia.
/// Nie zastępują dziennika wszystkich zmian wartości.
/// </summary>
public abstract class AuditableEntity
{
    public required int CreatedByOperatorId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public int? ModifiedByOperatorId { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
    public int? SoftDeletedByOperatorId { get; set; }
    public bool IsDeleted => SoftDeletedAt.HasValue;
}
