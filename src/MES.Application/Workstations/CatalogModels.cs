using MES.Domain.Properties;
using MES.Domain.Workstations;

namespace MES.Application.Workstations;

// Mutable state is confined to the store callback. Services return detached DTOs.
public sealed class CatalogState
{
    public List<Workstation> Workstations { get; } = [];
    public List<WorkstationType> Types { get; } = [];
    public List<PropertyDefinition> Definitions { get; } = [];
}

public interface IWorkstationCatalogStore
{
    T Execute<T>(Func<CatalogState, T> action);
}

public interface ICurrentOperator
{
    int Id { get; }
}

public sealed class CatalogValidationException(string message) : Exception(message);
public sealed class CatalogNotFoundException(string message) : Exception(message);

public sealed record LookupDto(int Id, string Name);
public sealed record PropertyRuleDto(int TypeId, int PropertyId, bool IsRequired);
public sealed record PropertyDto(int Id, string Code, string Name, string? Description,
    PropertyDataType DataType, string? Unit, IReadOnlyList<LookupDto> Options);
public sealed record WorkstationDto(int Id, string Code, string Name, string? Description,
    int TypeId, string TypeName,
    IReadOnlyDictionary<int, string> Values, DateTimeOffset CreatedAt, int CreatedByOperatorId,
    DateTimeOffset? ModifiedAt, int? ModifiedByOperatorId);
public sealed record CatalogDto(IReadOnlyList<LookupDto> Types,
    IReadOnlyList<PropertyDto> Properties, IReadOnlyList<PropertyRuleDto> Rules,
    IReadOnlyList<WorkstationDto> Workstations);

public sealed record CreatePropertyRequest(string Code, string Name, string? Description,
    PropertyDataType DataType, string? Unit, string? Options);
public sealed record CreateWorkstationRequest(string Code, string Name, string? Description,
    int TypeId, IReadOnlyDictionary<int, string> Values);

public interface IWorkstationCatalogService
{
    CatalogDto GetCatalog();
    int AddType(string name);
    int AddProperty(CreatePropertyRequest request);
    void AssignPropertyToType(int typeId, int propertyId, bool isRequired);
    int AddWorkstation(CreateWorkstationRequest request);
    void SaveValues(int workstationId, IReadOnlyDictionary<int, string> values);
}
