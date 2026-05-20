using System.IO;

namespace ThumbsPreloader.Core;

public sealed class Options
{
    public bool BadArguments { get; }
    public bool NoArguments { get; }
    public bool IncludeNestedDirectories { get; }
    public bool SilentMode { get; }
    public string? Path { get; }

    public Options(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            NoArguments = true;
            return;
        }

        if (arguments.Length > 2)
        {
            BadArguments = true;
            return;
        }

        var optionsProvided = arguments.Length == 2;
        var rawOptions = optionsProvided ? arguments[0] : string.Empty;
        Path = arguments[optionsProvided ? 1 : 0];

        if (!Directory.Exists(Path))
        {
            BadArguments = true;
            return;
        }

        IncludeNestedDirectories = rawOptions.Contains('r');
        SilentMode = rawOptions.Contains('s');
    }
}
