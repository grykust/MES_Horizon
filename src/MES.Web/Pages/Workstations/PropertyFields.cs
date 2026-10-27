using MES.Application.Workstations;
using MES.Domain.Properties;

namespace MES.Web.Pages.Workstations;

public sealed record PropertyFields(CatalogDto Catalog, int TypeId, IReadOnlyDictionary<int, string> Values)
{
    public static void ChangeSelection(CatalogDto catalog, int typeId, Dictionary<int, string> values,
        int propertyId, bool remove)
    {
        var rule = catalog.Rules.SingleOrDefault(x => x.TypeId == typeId && x.PropertyId == propertyId);
        if (rule is null || rule.IsRequired)
            throw new CatalogValidationException("Wybierz opcjonalną właściwość dostępną dla tego rodzaju.");
        if (remove) values.Remove(propertyId);
        else if (!values.TryAdd(propertyId, ""))
            throw new CatalogValidationException("Ta właściwość jest już w formularzu.");
    }

    // HTML sends repeated checkbox values; the application boundary uses a canonical
    // comma-separated representation, parsed and validated into typed option IDs.
    public static void MergeMultiValues(Dictionary<int, string> values, IReadOnlyList<string>? selections)
    {
        var groups = new Dictionary<int, List<int>>();
        foreach (var selection in selections ?? [])
        {
            var parts = selection.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var propertyId) || !int.TryParse(parts[1], out var optionId))
                throw new CatalogValidationException("Nieprawidłowy wybór opcji.");
            if (!groups.TryGetValue(propertyId, out var options)) groups[propertyId] = options = [];
            options.Add(optionId);
        }
        foreach (var entry in groups) values[entry.Key] = string.Join(",", entry.Value);
    }

    public static string Label(PropertyDataType type) => type switch
    {
        PropertyDataType.Text => "Tekst",
        PropertyDataType.Number => "Liczba",
        PropertyDataType.Boolean => "Tak / Nie",
        PropertyDataType.Date => "Data",
        PropertyDataType.Option => "Słownik",
        PropertyDataType.MultiOption => "Słownik — wiele wyborów",
        _ => "Nieznany"
    };
}
