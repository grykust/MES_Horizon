namespace MES.Domain.Common;

/// <summary>
/// Wspólne dane encji oraz audyt pochodzący z warstwy aplikacyjnej lub bazy danych.
/// </summary>
public abstract class Entity : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

}
