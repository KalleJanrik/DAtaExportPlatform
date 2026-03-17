namespace DataExportPlatform.Core.Models;

public class DataContext
{
    public IReadOnlyList<Employee> Employees { get; }
    public IReadOnlyList<Costcenter> Costcenters { get; }
    public IReadOnlyList<Accessright> Accessrights { get; }

    public DataContext(
        IReadOnlyList<Employee> employees,
        IReadOnlyList<Costcenter> costcenters,
        IReadOnlyList<Accessright> accessrights)
    {
        Employees = employees;
        Costcenters = costcenters;
        Accessrights = accessrights;
    }

    /// <summary>
    /// Mutates ResponsibleEmail on matching Costcenters and returns the same instance.
    /// </summary>
    public DataContext ApplyOverrides(IEnumerable<CostcenterResponsibleOverride> overrides)
    {
        var overrideMap = overrides.ToDictionary(o => o.CostcenterId, o => o.ResponsibleUserEmail);

        foreach (var cc in Costcenters)
        {
            if (overrideMap.TryGetValue(cc.Id, out var email))
            {
                cc.ResponsibleEmail = email;
            }
        }

        return this;
    }
}
