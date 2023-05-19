namespace jumwebapi.Infrastructure.HttpClients;

using System.Text.Json;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        string firstLetter = name.Substring(0, 1);
        string rest = name.Substring(1);
        string snakeCaseName = string.Concat(
            firstLetter.ToLowerInvariant(),
            rest.ToSnakeCase());

        return snakeCaseName;
    }
}

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        string snakeCase = string.Concat(
            str.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + c.ToString()
                    : c.ToString()));

        return snakeCase.ToLowerInvariant();
    }
}
