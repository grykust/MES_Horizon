using System.ComponentModel.DataAnnotations;
using MES.Application.Common.Exceptions;
using MES.Application.Machines;
using MES.Application.Machines.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace MES.Web.Pages.Machines;
public sealed class IndexModel(IMachineService service, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true), StringLength(100)]
    public string? Search { get; set; }
    public IReadOnlyList<MachineDto> Machines { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return;
        try { Machines = await service.GetAsync(Search, cancellationToken); }
        catch (DataAccessException ex)
        {
            logger.LogWarning(ex, "Lista maszyn jest niedostępna.");
            ErrorMessage = "Lista maszyn jest niedostępna. Sprawdź konfigurację połączenia i zapytania SQL.";
        }
    }
}
