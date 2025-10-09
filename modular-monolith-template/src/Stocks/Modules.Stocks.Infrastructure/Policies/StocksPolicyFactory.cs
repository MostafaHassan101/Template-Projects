using Microsoft.AspNetCore.Authorization;
using Modules.Common.Infrastructure.Policies;
using Modules.Stocks.Domain.Policies;

namespace Modules.Stocks.Infrastructure.Policies;

internal sealed class StocksPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Stocks";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [StockPolicyConsts.ReadPolicy] = policy => policy.RequireClaim(StockPolicyConsts.ReadPolicy),
            [StockPolicyConsts.CreatePolicy] = policy => policy.RequireClaim(StockPolicyConsts.CreatePolicy),
            [StockPolicyConsts.UpdatePolicy] = policy => policy.RequireClaim(StockPolicyConsts.UpdatePolicy),
            [StockPolicyConsts.DeletePolicy] = policy => policy.RequireClaim(StockPolicyConsts.DeletePolicy)
        };
    }
}
