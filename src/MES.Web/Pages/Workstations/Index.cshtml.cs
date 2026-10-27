using MES.Application.Workstations;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MES.Web.Pages.Workstations;

public sealed class IndexModel(IWorkstationCatalogService service) : PageModel
{
    public CatalogDto Catalog { get; private set; } = null!;
    public void OnGet() => Catalog = service.GetCatalog();
}
