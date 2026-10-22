using MES.Application.Common.Interfaces;
using MES.Application.Machines.DTOs;
namespace MES.Application.Machines;
public sealed class MachineService(IMachineRepository repository) : IMachineService
{
    public async Task<IReadOnlyList<MachineDto>> GetAsync(string? search, CancellationToken cancellationToken)
    {
        var filter = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (filter?.Length > 100) throw new ArgumentException("Maksymalnie 100 znaków.", nameof(search));
        var machines = await repository.GetAsync(filter, cancellationToken);
        return machines.Select(m => new MachineDto(m.Id, m.Code, m.Name)).ToArray();
    }
}
