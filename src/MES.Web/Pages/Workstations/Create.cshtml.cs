using System.ComponentModel.DataAnnotations;
using MES.Application.Workstations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MES.Web.Pages.Workstations;

public sealed class CreateModel(IWorkstationCatalogService service) : PageModel
{
    [BindProperty, Required, StringLength(50)] public string Code { get; set; } = "";
    [BindProperty, Required, StringLength(200)] public string Name { get; set; } = "";
    [BindProperty, StringLength(2000)] public string? Description { get; set; }
    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)] public int TypeId { get; set; }
    [BindProperty] public List<string>? SelectedOptions { get; set; }
    [BindProperty(Name = "Values")] public Dictionary<int, string> Values { get; set; } = [];
    public CatalogDto Catalog { get; private set; } = null!;
    public void OnGet() => Catalog = service.GetCatalog();
    public IActionResult OnPostAddProperty(int? addPropertyId) => ChangeSelection(addPropertyId ?? 0, false);
    public IActionResult OnPostRemoveProperty(int propertyId) => ChangeSelection(propertyId, true);
    private IActionResult ChangeSelection(int propertyId, bool remove)
    {
        Catalog = service.GetCatalog();
        // Intermediate actions preserve the draft without requiring its fields to be complete.
        ModelState.Clear();
        try
        {
            PropertyFields.MergeMultiValues(Values, SelectedOptions);
            PropertyFields.ChangeSelection(Catalog, TypeId, Values, propertyId, remove);
        }
        catch (CatalogValidationException ex) { ModelState.AddModelError("", ex.Message); }
        return Page();
    }
    public IActionResult OnPost()
    {
        Catalog = service.GetCatalog();
        try
        {
            PropertyFields.MergeMultiValues(Values, SelectedOptions);
            if (!ModelState.IsValid) return Page();
            var id = service.AddWorkstation(new(Code, Name, Description, TypeId, Values));
            return RedirectToPage("Details", new { id });
        }
        catch (CatalogValidationException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
    }
}
