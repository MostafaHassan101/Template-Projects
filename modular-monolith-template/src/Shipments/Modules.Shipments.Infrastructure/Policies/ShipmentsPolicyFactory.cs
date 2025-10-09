using Microsoft.AspNetCore.Authorization;
using Modules.Common.Infrastructure.Policies;
using Modules.Shipments.Domain.Policies;

namespace Modules.Shipments.Infrastructure.Policies;

internal sealed class ShipmentsPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Shipments";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [ShipmentPolicyConsts.ReadPolicy] = policy => policy.RequireClaim(ShipmentPolicyConsts.ReadPolicy),
            [ShipmentPolicyConsts.CreatePolicy] = policy => policy.RequireClaim(ShipmentPolicyConsts.CreatePolicy),
            [ShipmentPolicyConsts.UpdatePolicy] = policy => policy.RequireClaim(ShipmentPolicyConsts.UpdatePolicy),
            [ShipmentPolicyConsts.DeletePolicy] = policy => policy.RequireClaim(ShipmentPolicyConsts.DeletePolicy)
        };
    }
}
