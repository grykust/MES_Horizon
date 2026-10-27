using MES.Application.Workstations;

namespace MES.Web.Prototype;

// Used only by the explicitly enabled prototype. Replace with authenticated operator resolution for SQL integration.
public sealed class DemoOperator : ICurrentOperator
{
    public int Id => 1;
}
