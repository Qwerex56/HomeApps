using System.Reflection;

namespace ServiceName.Domain;

public static class DomainAssembly
{
    public static readonly Assembly Assembly = typeof(DomainAssembly).Assembly;
}
