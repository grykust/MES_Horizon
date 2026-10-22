using MES.Domain.Machines;
namespace MES.Application.Common.Interfaces;
public interface IMachineRepository
{
    Task<IReadOnlyList<Machine>> GetAsync(string? search, CancellationToken cancellationToken);
}
