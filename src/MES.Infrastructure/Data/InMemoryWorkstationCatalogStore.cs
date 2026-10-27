using MES.Application.Workstations;

namespace MES.Infrastructure.Data;

/// <summary>Process-local prototype store. One lock protects validation and mutation together.</summary>
public sealed class InMemoryWorkstationCatalogStore : IWorkstationCatalogStore
{
    private readonly object sync = new();
    private readonly CatalogState state = new();

    public T Execute<T>(Func<CatalogState, T> action)
    {
        lock (sync) return action(state);
    }
}
