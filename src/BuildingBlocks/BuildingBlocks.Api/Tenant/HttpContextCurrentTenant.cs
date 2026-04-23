using AthleteOS.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AthleteOS.BuildingBlocks.Api.Tenant;

internal sealed class HttpContextCurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    private const string TenantIdClaimType = "tenant_id";

    public Guid TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(TenantIdClaimType);
            return claim is not null && Guid.TryParse(claim.Value, out var id)
                ? id
                : throw new InvalidOperationException("Tenant ID claim is missing or invalid.");
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
