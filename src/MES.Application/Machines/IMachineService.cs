using MES.Application.Machines.DTOs;
namespace MES.Application.Machines;
public interface IMachineService
{
    Task<IReadOnlyList<MachineDto>> GetAsync(string? search, CancellationToken cancellationToken);
}
