using System.Reflection;

namespace JetHerald.Authorization;
public static class FlightcheckHelpers
{
    public static IEnumerable<string> GetUsedPermissions(Type rootType)
    {
        var res = new HashSet<string>();
        var asm = Assembly.GetAssembly(rootType);
        var types = asm.GetTypes();
        var methods = types.SelectMany(t => t.GetMethods());

        foreach (var t in types)
        {
            if (t.GetCustomAttribute<PermissionAttribute>() is PermissionAttribute perm)
                res.Add(perm.Policy);
        }

        foreach (var t in methods)
        {
            if (t.GetCustomAttribute<PermissionAttribute>() is PermissionAttribute perm)
                res.Add(perm.Policy);
        }

        return res.OrderBy(p => p);
    }
}

