using Microsoft.AspNetCore.Authorization;

namespace DataExportPlatform.Web.Authorization;

/// <summary>
/// Requires the user to have access to a specific export job's archive.
/// Satisfied when the user holds a claim "ArchiveJob" matching the AppId,
/// or an "ArchiveAll" claim for unrestricted access.
/// </summary>
public class ArchiveJobRequirement : IAuthorizationRequirement
{
    public string AppId { get; }

    public ArchiveJobRequirement(string appId) => AppId = appId;
}
