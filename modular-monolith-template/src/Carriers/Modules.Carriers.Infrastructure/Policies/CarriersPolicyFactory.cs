using Microsoft.AspNetCore.Authorization;
using Modules.Carriers.Domain.Policies;
using Modules.Common.Infrastructure.Policies;

namespace Modules.Carriers.Infrastructure.Policies;

internal sealed class CarriersPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Carriers";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [CarrierPolicyConsts.ReadPolicy] = policy => policy.RequireClaim(CarrierPolicyConsts.ReadPolicy),
            [CarrierPolicyConsts.CreatePolicy] = policy => policy.RequireClaim(CarrierPolicyConsts.CreatePolicy),
            [CarrierPolicyConsts.UpdatePolicy] = policy => policy.RequireClaim(CarrierPolicyConsts.UpdatePolicy),
            [CarrierPolicyConsts.DeletePolicy] = policy => policy.RequireClaim(CarrierPolicyConsts.DeletePolicy)
        };
    }
}
