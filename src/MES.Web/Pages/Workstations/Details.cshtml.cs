using MES.Application.Workstations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MES.Web.Pages.Workstations;

public sealed class DetailsModel(IWorkstationCatalogService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    [BindProperty(Name = "Values")] public Dictionary<int, string> Values { get; set; } = [];
    [BindProperty] public List<string>? SelectedOptions { get; set; }
    public CatalogDto Catalog { get; private set; } = null!;
    public WorkstationDto Workstation { get; private set; } = null!;
    [TempData] public string? Notice { get; set; }
    private bool Load()
    {
        Catalog = service.GetCatalog();
        Workstation = Catalog.Workstations.SingleOrDefault(x => x.Id == Id)!;
        return Workstation is not null;
    }
    public IActionResult OnGet()
    {
        if (!Load()) return NotFound();
        Values = Workstation.Values.ToDictionary();
        return Page();
    }
    public IActionResult OnPost()
    {
        if (!Load()) return NotFound();
        try
        {
            PropertyFields.MergeMultiValues(Values, SelectedOptions);
            if (!ModelState.IsValid) return Page();
            service.SaveValues(Id, Values);
            Notice = "Wartości zapisane.";
            return RedirectToPage(new { id = Id });
        }
        catch (CatalogValidationException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
        catch (CatalogNotFoundException) { return NotFound(); }
    }
    public IActionResult OnPostAddProperty(int? addPropertyId) => ChangeSelection(addPropertyId ?? 0, false);
    public IActionResult OnPostRemoveProperty(int propertyId) => ChangeSelection(propertyId, true);
    private IActionResult ChangeSelection(int propertyId, bool remove)
    {
        if (!Load()) return NotFound();
        ModelState.Clear();
        try
        {
            PropertyFields.MergeMultiValues(Values, SelectedOptions);
            PropertyFields.ChangeSelection(Catalog, Workstation.TypeId, Values, propertyId, remove);
        }
        catch (CatalogValidationException ex) { ModelState.AddModelError("", ex.Message); }
        return Page();
    }
}
