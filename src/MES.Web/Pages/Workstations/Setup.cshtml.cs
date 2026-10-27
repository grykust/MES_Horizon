using MES.Application.Workstations;
using MES.Domain.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MES.Web.Pages.Workstations;

public sealed class SetupModel(IWorkstationCatalogService service) : PageModel
{
    public CatalogDto Catalog { get; private set; } = null!;
    [BindProperty] public string? TypeName { get; set; }
    [BindProperty] public string? PropertyCode { get; set; }
    [BindProperty] public string? PropertyName { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public PropertyDataType DataType { get; set; } = PropertyDataType.Text;
    [BindProperty] public string? Unit { get; set; }
    [BindProperty] public string? Options { get; set; }
    [BindProperty] public int TypeId { get; set; }
    [BindProperty] public int PropertyId { get; set; }
    [BindProperty] public bool IsRequired { get; set; }
    [TempData] public string? Notice { get; set; }
    public void OnGet() => Catalog = service.GetCatalog();
    public IActionResult OnPostType() => Save(() => service.AddType(TypeName ?? ""), "Rodzaj dodany.");
    public IActionResult OnPostProperty() => Save(() => service.AddProperty(new(PropertyCode ?? "", PropertyName ?? "",
        Description, DataType, Unit, Options)), "Definicja właściwości dodana.");
    public IActionResult OnPostRule() => Save(() => service.AssignPropertyToType(TypeId, PropertyId, IsRequired), "Przypisanie zapisane.");
    private IActionResult Save(Action action, string success)
    {
        Catalog = service.GetCatalog();
        if (!ModelState.IsValid) return Page();
        try { action(); Notice = success; return RedirectToPage(); }
        catch (CatalogValidationException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
    }
}
