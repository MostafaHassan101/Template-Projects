using System.Reflection;

namespace Modules.Stocks.Domain;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
