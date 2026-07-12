using Spectre.Console.Cli;

namespace Fallout.Migrate;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<MigrateCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("fallout-migrate");
            config.AddExample("--dry-run");
            config.AddExample("path/to/repo");
        });

        return app.Run(args);
    }
}
