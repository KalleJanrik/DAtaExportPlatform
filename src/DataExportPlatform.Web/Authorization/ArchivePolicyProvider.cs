using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DataExportPlatform.Web.Authorization;

/// <summary>
/// Generates authorization policies on demand for the "Archive.{AppId}" prefix.
/// All other policy names fall through to the default provider.
/// </summary>
public class ArchivePolicyProvider : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "Archive.";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public ArchivePolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var appId = policyName[PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ArchiveJobRequirement(appId))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
