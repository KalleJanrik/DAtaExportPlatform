namespace DataExportPlatform.Core.Interfaces;

public interface IAdGroupService
{
    /// <summary>Returns all direct and nested members of the given AD group.</summary>
    Task<IReadOnlyList<AdGroupMember>> GetMembersAsync(string groupName, CancellationToken ct = default);

    /// <summary>Returns true if the user (by SAM account name) is a member of the group.</summary>
    Task<bool> IsUserInGroupAsync(string samAccountName, string groupName, CancellationToken ct = default);
}

public class AdGroupMember
{
    public string SamAccountName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserPrincipalName { get; init; } = string.Empty;
}
