namespace DataExportPlatform.Core.Models;

public class DataContext(
    IReadOnlyList<Employee> employees,
    IReadOnlyList<Costcenter> costcenters,
    IReadOnlyList<Accessright> accessrights)
{
    public IReadOnlyList<Employee> Employees { get; } = employees;
    public IReadOnlyList<Costcenter> Costcenters { get; } = costcenters;
    public IReadOnlyList<Accessright> Accessrights { get; } = accessrights;


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
