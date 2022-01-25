using System.Data;
using Dapper;

namespace JetHerald;
public static class DapperConverters
{
    static bool registered = false;
    public static void Register()
    {
        if (registered)
            return;
        registered = true;

        SqlMapper.AddTypeHandler(new NamespacedIdHandler());
    }

    class NamespacedIdHandler : SqlMapper.TypeHandler<NamespacedId>
    {
        public override void SetValue(IDbDataParameter parameter, NamespacedId value) => parameter.Value = value.ToString();
        public override NamespacedId Parse(object value) => new((string)value);
    }
}
