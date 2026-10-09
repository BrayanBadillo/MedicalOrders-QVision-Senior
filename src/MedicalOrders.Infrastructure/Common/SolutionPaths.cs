using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace MedicalOrders.Infrastructure.Common;

public class SolutionPaths
{
    public const string DefaultConnectionString = "Data Source=database/orders.db";
    private const string SolutionFileName = "OrderProcessing.sln";

    public static string RootDirectory { get; } = FindRootDirectory();

    public static string ResolveLogsDirectory(IConfiguration configuration) =>
        Resolve(configuration["FileLogging:Directory"] ?? "logs");

    public static string ResolveConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        var isFileBased = !string.IsNullOrWhiteSpace(dataSource)
                          && dataSource != ":memory:"
                          && !dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase);

        if (isFileBased)
        {
            var fullPath = Resolve(dataSource);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            builder.DataSource = fullPath;
        }

        return builder.ToString();
    }

    private static string Resolve(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(RootDirectory, path));

    private static string FindRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
                return directory.FullName;

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}

