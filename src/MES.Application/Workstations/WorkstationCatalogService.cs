using System.Globalization;
using MES.Domain.Properties;
using MES.Domain.Workstations;

namespace MES.Application.Workstations;

public sealed class WorkstationCatalogService(IWorkstationCatalogStore store, ICurrentOperator currentOperator)
    : IWorkstationCatalogService
{
    private static string Required(string? value, string label, int max = 200)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > max)
            throw new CatalogValidationException($"{label}: podaj od 1 do {max} znaków.");
        return text;
    }

    private static string? Optional(string? value, string label, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Required(value, label, max);
    }

    private int OperatorId => currentOperator.Id > 0 ? currentOperator.Id
        : throw new CatalogValidationException("Nie ustalono operatora.");

    public CatalogDto GetCatalog() => store.Execute(state => new CatalogDto(
        state.Types.Where(x => !x.IsDeleted).Select(x => new LookupDto(x.Id, x.Name)).ToArray(),
        state.Definitions.Where(x => !x.IsDeleted).Select(x => new PropertyDto(x.Id, x.Code, x.Name,
            x.Description, x.DataType, x.Unit, x.Options.Where(o => !o.IsDeleted)
                .Select(o => new LookupDto(o.Id, o.Name)).ToArray())).ToArray(),
        state.Types.Where(x => !x.IsDeleted).SelectMany(x => x.Properties).Where(x => !x.IsDeleted)
            .Select(x => new PropertyRuleDto(x.WorkstationTypeId, x.PropertyId, x.IsRequired)).ToArray(),
        state.Workstations.Where(x => !x.IsDeleted).Select(x => new WorkstationDto(x.Id, x.Code, x.Name,
            x.Description, x.WorkstationTypeId,
            state.Types.Single(t => t.Id == x.WorkstationTypeId).Name,
            x.Properties.Where(p => !p.IsDeleted).ToDictionary(p => p.PropertyId, Format),
            x.CreatedAt, x.CreatedByOperatorId, x.ModifiedAt, x.ModifiedByOperatorId)).ToArray()));

    public int AddType(string name) => store.Execute(state =>
    {
        var label = Required(name, "Nazwa rodzaju");
        if (state.Types.Any(x => !x.IsDeleted && x.Name.Equals(label, StringComparison.OrdinalIgnoreCase)))
            throw new CatalogValidationException("Taki rodzaj już istnieje.");
        var item = new WorkstationType { Id = state.Types.Count + 1, Name = label,
            CreatedAt = DateTimeOffset.UtcNow, CreatedByOperatorId = OperatorId };
        state.Types.Add(item);
        return item.Id;
    });

    public int AddProperty(CreatePropertyRequest request) => store.Execute(state =>
    {
        var code = Required(request.Code, "Kod", 50).ToUpperInvariant();
        var name = Required(request.Name, "Nazwa");
        if (!Enum.IsDefined(request.DataType)) throw new CatalogValidationException("Nieznany typ danych.");
        if (state.Definitions.Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new CatalogValidationException("Właściwość o takim kodzie już istnieje.");
        var unit = Optional(request.Unit, "Jednostka", 30);
        if (unit is not null && request.DataType != PropertyDataType.Number)
            throw new CatalogValidationException("Jednostkę można podać tylko dla liczby.");
        var options = (request.Options ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (request.DataType is PropertyDataType.Option or PropertyDataType.MultiOption)
        {
            if (options.Length == 0 || options.Length > 100) throw new CatalogValidationException("Podaj od 1 do 100 opcji, każdą w osobnej linii.");
            if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Length)
                throw new CatalogValidationException("Opcje nie mogą się powtarzać.");
            foreach (var option in options) Required(option, "Opcja");
        }
        else if (options.Length > 0) throw new CatalogValidationException("Opcje można podać tylko dla typu Słownik.");
        var now = DateTimeOffset.UtcNow;
        var actor = OperatorId;
        var item = new PropertyDefinition { Id = state.Definitions.Count + 1, Code = code, Name = name,
            Description = Optional(request.Description, "Opis", 2000), DataType = request.DataType, Unit = unit,
            CreatedAt = now, CreatedByOperatorId = actor };
        var nextOption = state.Definitions.SelectMany(x => x.Options).Select(x => x.Id).DefaultIfEmpty().Max() + 1;
        foreach (var option in options)
        {
            var id = nextOption++;
            item.Options.Add(new PropertyOption { Id = id, PropertyId = item.Id, Name = option,
                Code = $"OPTION_{id}", CreatedAt = now, CreatedByOperatorId = actor });
        }
        state.Definitions.Add(item);
        return item.Id;
    });

    public void AssignPropertyToType(int typeId, int propertyId, bool isRequired) => store.Execute(state =>
    {
        var type = state.Types.SingleOrDefault(x => x.Id == typeId && !x.IsDeleted)
            ?? throw new CatalogValidationException("Wybierz istniejący rodzaj stanowiska.");
        if (!state.Definitions.Any(x => x.Id == propertyId && !x.IsDeleted))
            throw new CatalogValidationException("Wybierz istniejącą właściwość.");
        if (isRequired && state.Workstations.Any(w => !w.IsDeleted && w.WorkstationTypeId == typeId
            && !w.Properties.Any(p => p.PropertyId == propertyId && !p.IsDeleted)))
            throw new CatalogValidationException("Najpierw przypisz cechę jako opcjonalną i uzupełnij ją we wszystkich stanowiskach tego rodzaju.");
        var now = DateTimeOffset.UtcNow;
        var actor = OperatorId;
        var rule = type.Properties.SingleOrDefault(x => x.PropertyId == propertyId && !x.IsDeleted);
        if (rule is null) type.Properties.Add(new WorkstationTypeProperty { WorkstationTypeId = typeId,
            PropertyId = propertyId, IsRequired = isRequired, CreatedAt = now, CreatedByOperatorId = actor });
        else { rule.IsRequired = isRequired; rule.ModifiedAt = now; rule.ModifiedByOperatorId = actor; }
        return true;
    });

    public int AddWorkstation(CreateWorkstationRequest request) => store.Execute(state =>
    {
        var code = Required(request.Code, "Kod stanowiska", 50).ToUpperInvariant();
        var name = Required(request.Name, "Nazwa stanowiska");
        if (state.Workstations.Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new CatalogValidationException("Stanowisko o takim kodzie już istnieje.");
        var type = state.Types.SingleOrDefault(x => x.Id == request.TypeId && !x.IsDeleted)
            ?? throw new CatalogValidationException("Wybierz rodzaj stanowiska.");
        var item = new Workstation { Id = state.Workstations.Count + 1, Code = code, Name = name,
            Description = Optional(request.Description, "Opis", 2000), WorkstationTypeId = type.Id,
            CreatedAt = DateTimeOffset.UtcNow, CreatedByOperatorId = OperatorId };
        var values = ParseValues(state, type, item.Id, request.Values);
        foreach (var value in values) item.Properties.Add(value);
        state.Workstations.Add(item);
        return item.Id;
    });

    public void SaveValues(int workstationId, IReadOnlyDictionary<int, string> values) => store.Execute(state =>
    {
        var item = state.Workstations.SingleOrDefault(x => x.Id == workstationId && !x.IsDeleted)
            ?? throw new CatalogNotFoundException("Stanowisko nie istnieje.");
        var type = state.Types.Single(x => x.Id == item.WorkstationTypeId);
        var parsed = ParseValues(state, type, item.Id, values);
        var now = DateTimeOffset.UtcNow;
        var actor = OperatorId;
        foreach (var old in item.Properties.Where(x => !x.IsDeleted))
        {
            var replacement = parsed.SingleOrDefault(x => x.PropertyId == old.PropertyId);
            if (replacement is null) { old.SoftDeletedAt = now; old.SoftDeletedByOperatorId = actor; }
            else
            {
                replacement.CreatedAt = old.CreatedAt;
                replacement.CreatedByOperatorId = old.CreatedByOperatorId;
                replacement.ModifiedAt = now;
                replacement.ModifiedByOperatorId = actor;
            }
        }
        // Retain removed assignments with their soft-delete audit; replace active values atomically under the store lock.
        var removed = item.Properties.Where(x => x.IsDeleted).ToArray();
        item.Properties.Clear();
        foreach (var value in removed.Concat(parsed)) item.Properties.Add(value);
        item.ModifiedAt = now;
        item.ModifiedByOperatorId = actor;
        return true;
    });

    private List<WorkstationProperty> ParseValues(CatalogState state, WorkstationType type, int workstationId,
        IReadOnlyDictionary<int, string> values)
    {
        var rules = type.Properties.Where(x => !x.IsDeleted).ToArray();
        if (values.Keys.Any(id => !rules.Any(r => r.PropertyId == id)))
            throw new CatalogValidationException("Właściwość nie jest dostępna dla tego rodzaju stanowiska.");
        var result = new List<WorkstationProperty>();
        foreach (var rule in rules)
        {
            var definition = state.Definitions.Single(x => x.Id == rule.PropertyId && !x.IsDeleted);
            var raw = values.GetValueOrDefault(rule.PropertyId)?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                if (rule.IsRequired || values.ContainsKey(rule.PropertyId))
                    throw new CatalogValidationException($"Uzupełnij: {definition.Name}. Opcjonalną cechę możesz usunąć z formularza.");
                continue;
            }
            var value = new WorkstationProperty { WorkstationId = workstationId, PropertyId = definition.Id,
                CreatedAt = DateTimeOffset.UtcNow, CreatedByOperatorId = OperatorId };
            switch (definition.DataType)
            {
                case PropertyDataType.Text: value.TextValue = Required(raw, definition.Name, 2000); break;
                case PropertyDataType.Number:
                    if (!decimal.TryParse(raw.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out var number)) throw new CatalogValidationException($"{definition.Name}: podaj liczbę.");
                    value.NumberValue = number; break;
                case PropertyDataType.Boolean:
                    if (!bool.TryParse(raw, out var flag)) throw new CatalogValidationException($"{definition.Name}: wybierz Tak lub Nie.");
                    value.BooleanValue = flag; break;
                case PropertyDataType.Date:
                    if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        throw new CatalogValidationException($"{definition.Name}: podaj prawidłową datę.");
                    value.DateValue = date; break;
                case PropertyDataType.Option:
                    if (!int.TryParse(raw, out var id) || !definition.Options.Any(o => o.Id == id && !o.IsDeleted))
                        throw new CatalogValidationException($"{definition.Name}: wybierz opcję z tej właściwości.");
                    value.OptionId = id; break;
                case PropertyDataType.MultiOption:
                    var parts = raw.Split(',');
                    if (parts.Length > 100) throw new CatalogValidationException($"{definition.Name}: maksymalnie 100 opcji.");
                    foreach (var part in parts)
                    {
                        if (!int.TryParse(part, out var optionId) || !definition.Options.Any(o => o.Id == optionId && !o.IsDeleted))
                            throw new CatalogValidationException($"{definition.Name}: wybierz opcje z tej właściwości.");
                        if (value.OptionIds.Contains(optionId))
                            throw new CatalogValidationException($"{definition.Name}: opcje nie mogą się powtarzać.");
                        value.OptionIds.Add(optionId);
                    }
                    value.OptionIds.Sort();
                    break;
                default: throw new CatalogValidationException("Nieobsługiwany typ danych.");
            }
            result.Add(value);
        }
        return result;
    }

    private static string Format(WorkstationProperty value) => value.TextValue
        ?? value.NumberValue?.ToString(CultureInfo.InvariantCulture)
        ?? value.BooleanValue?.ToString().ToLowerInvariant()
        ?? value.DateValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        ?? value.OptionId?.ToString(CultureInfo.InvariantCulture)
        ?? string.Join(",", value.OptionIds);
}
