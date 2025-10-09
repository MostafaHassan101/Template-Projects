using System.Reflection;

namespace Modules.Stocks.PublicApi;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
