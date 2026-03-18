using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Principal;

namespace DataExportPlatform.Web.Authorization;

/// <summary>
/// Maps Windows AD group membership to ArchiveJob / ArchiveAll claims so that
/// ArchivePolicyProvider can enforce per-job access without any code changes
/// when groups or jobs are added.
///
/// Configuration (appsettings.json):
///   "Authorization": {
///     "ArchiveAdminGroup": "DOMAIN\\SG-DataExport-Admins",
///     "ArchiveJobGroups": {
///       "AppA": "DOMAIN\\SG-DataExport-AppA",
///       "AppB": "DOMAIN\\SG-DataExport-AppB",
///       "AppC": "DOMAIN\\SG-DataExport-AppC",
///       "AppD": "DOMAIN\\SG-DataExport-AppD"
///     }
///   }
///
/// Group membership rules:
///   - Member of ArchiveAdminGroup  → ArchiveAll claim  → access to every job
///   - Member of a job-specific group → ArchiveJob claim → access to that job only
///   - A user can be in multiple job groups and will receive one claim per group
/// </summary>
public class AdGroupClaimsTransformation : IClaimsTransformation
{
    private readonly string? _adminGroup;
    // Key: AppId (e.g. "AppA"), Value: AD group name (e.g. "DOMAIN\SG-DataExport-AppA")
    private readonly Dictionary<string, string> _jobGroups;

    public AdGroupClaimsTransformation(IConfiguration configuration)
    {
        _adminGroup = configuration["Authorization:ArchiveAdminGroup"];

        _jobGroups = configuration
            .GetSection("Authorization:ArchiveJobGroups")
            .GetChildren()
            .ToDictionary(s => s.Key, s => s.Value ?? string.Empty);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only transform authenticated Windows users
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(principal);

        if (principal.Identity is not WindowsIdentity { IsAuthenticated: true })
            return Task.FromResult(principal);

        // Guard against double-transformation on the same principal
        if (principal.HasClaim(c => c.Type is "ArchiveJob" or "ArchiveAll"))
            return Task.FromResult(principal);

        var identity = new ClaimsIdentity();

        if (_adminGroup is not null && principal.IsInRole(_adminGroup))
        {
            // Admin group membership grants unrestricted archive access
            identity.AddClaim(new Claim("ArchiveAll", "true"));
        }
        else
        {
            // Add one ArchiveJob claim per job group the user belongs to
            foreach (var (appId, group) in _jobGroups)
            {
                if (principal.IsInRole(group))
                    identity.AddClaim(new Claim("ArchiveJob", appId));
            }
        }

        if (identity.Claims.Any())
            principal.AddIdentity(identity);

        return Task.FromResult(principal);
    }
}
