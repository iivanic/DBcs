using System.Reflection;

namespace DBcs;

public class Utility
{
    public static string SnakeToCamel(string name)
    {
        string[] names = name.Split('.');
        name = names[names.Length - 1];
        //Snake case => CamelCase
        var parts = name.Split("_",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        var preparedName = "";
        foreach (var n in parts)
        {
            preparedName += n.Substring(0, 1).ToUpper();
            if (n.Length > 1)
                preparedName += n.Substring(1);
        }
        return preparedName;
    }
    public static string CamelToSnake(string name)
    {
        var ret = "";
        var cnt = 0;
        foreach (char c in name.ToCharArray())
        {
            if (char.IsUpper(c) && cnt > 0)
            {
                ret += $"_{char.ToLower(c)}";
            }
            else{
                ret += char.ToLower(c);
            }
            cnt++;
        }

        return ret;
    }
    public static bool IsMarkedAsNullable(PropertyInfo p)
    {
        return new NullabilityInfoContext().Create(p).WriteState is NullabilityState.Nullable;
    }
}
