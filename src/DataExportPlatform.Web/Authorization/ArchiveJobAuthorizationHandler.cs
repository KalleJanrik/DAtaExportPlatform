using Microsoft.AspNetCore.Authorization;

namespace DataExportPlatform.Web.Authorization;

public class ArchiveJobAuthorizationHandler : AuthorizationHandler<ArchiveJobRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ArchiveJobRequirement requirement)
    {
        // Unrestricted access — e.g. administrators
        if (context.User.HasClaim("ArchiveAll", "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Per-job access — user must hold a claim "ArchiveJob" = "<AppId>"
        if (context.User.HasClaim("ArchiveJob", requirement.AppId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
