using System.Text;

namespace WayFinder.DevTools.Cli.Interactive;

public static class InteractiveCommandParser
{
    public static IReadOnlyList<string> Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var arguments = new List<string>();
        var current = new StringBuilder();

        char? quote = null;
        var tokenStarted = false;

        foreach (var character in input)
        {
            if (quote is not null)
            {
                if (character == quote)
                {
                    quote = null;
                }
                else
                {
                    current.Append(character);
                }

                continue;
            }

            if (character is '"' or '\'')
            {
                quote = character;
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (tokenStarted)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                    tokenStarted = false;
                }

                continue;
            }

            current.Append(character);
            tokenStarted = true;
        }

        if (quote is not null)
        {
            throw new FormatException(
                "Unterminated quoted argument."
            );
        }

        if (tokenStarted)
        {
            arguments.Add(current.ToString());
        }

        return arguments;
    }
}
