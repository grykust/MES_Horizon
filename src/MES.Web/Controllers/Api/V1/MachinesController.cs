using System.ComponentModel.DataAnnotations;
using MES.Application.Machines;
using MES.Application.Machines.DTOs;
using Microsoft.AspNetCore.Mvc;
namespace MES.Web.Controllers.Api.V1;
[ApiController]
[Route("api/v1/machines")]
public sealed class MachinesController(IMachineService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MachineDto>>(200)]
    [ProducesResponseType<ProblemDetails>(503)]
    public async Task<ActionResult<IReadOnlyList<MachineDto>>> Get(
        [FromQuery, StringLength(100)] string? search, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(search, cancellationToken));
}
