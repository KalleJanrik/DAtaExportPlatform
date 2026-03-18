using DataExportPlatform.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace DataExportPlatform.Infrastructure.ActiveDirectory;

[SupportedOSPlatform("windows")]
public class AdGroupService : IAdGroupService
{
    private readonly string _domain;
    private readonly ILogger<AdGroupService> _logger;

    public AdGroupService(IConfiguration configuration, ILogger<AdGroupService> logger)
    {
        _domain = configuration["ActiveDirectory:Domain"] ?? Environment.UserDomainName;
        _logger = logger;
    }

    public Task<IReadOnlyList<AdGroupMember>> GetMembersAsync(string groupName, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain);
            using var group = GroupPrincipal.FindByIdentity(context, groupName);

            if (group is null)
            {
                _logger.LogWarning("AD group not found: {Group}", groupName);
                return (IReadOnlyList<AdGroupMember>)[];
            }

            var members = group
                .GetMembers(recursive: true)
                .OfType<UserPrincipal>()
                .Select(u => new AdGroupMember
                {
                    SamAccountName    = u.SamAccountName ?? string.Empty,
                    DisplayName       = u.DisplayName ?? string.Empty,
                    Email             = u.EmailAddress ?? string.Empty,
                    UserPrincipalName = u.UserPrincipalName ?? string.Empty,
                })
                .ToList();

            _logger.LogInformation("AD group {Group} resolved {Count} member(s).", groupName, members.Count);
            return (IReadOnlyList<AdGroupMember>)members;
        }, ct);
    }

    public Task<bool> IsUserInGroupAsync(string samAccountName, string groupName, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain);
            using var user  = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
            using var group = GroupPrincipal.FindByIdentity(context, groupName);

            if (user is null || group is null)
                return false;

            return user.IsMemberOf(group);
        }, ct);
    }
}
