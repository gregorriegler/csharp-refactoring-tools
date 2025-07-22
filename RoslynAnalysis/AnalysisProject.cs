using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynAnalysis;

public class AnalysisProject(string projectPath, string fileName)
{
    private readonly string projectPath = ResolveProjectPath(projectPath);

    private static string ResolveProjectPath(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }

        if (!Directory.Exists(path)) throw new FileNotFoundException($"Project file not found: '{path}'");
        var slnFiles = Directory.GetFiles(path, "*.sln");
        if (slnFiles.Length > 0)
        {
            var csprojFromSolution = ExtractFirstCsprojFromSolution(slnFiles[0]);
            if (csprojFromSolution != null)
            {
                return csprojFromSolution;
            }
        }

        var csprojFiles = Directory.GetFiles(path, "*.csproj");
        if (csprojFiles.Length > 0)
        {
            return csprojFiles[0];
        }

        throw new FileNotFoundException($"No .sln or .csproj files found in directory: '{path}'");

    }

    private static string? ExtractFirstCsprojFromSolution(string solutionPath)
    {
        try
        {
            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            var lines = File.ReadAllLines(solutionPath);

            foreach (var line in lines)
            {
                if (!line.StartsWith("Project(") || !line.Contains(".csproj")) continue;
                var parts = line.Split(',');
                if (parts.Length < 2) continue;
                var projectPathPart = parts[1].Trim();
                if (!projectPathPart.StartsWith("\"") || !projectPathPart.EndsWith("\"")) continue;
                var relativePath = projectPathPart.Substring(1, projectPathPart.Length - 2);
                var fullPath = Path.Combine(solutionDir, relativePath);

                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public async Task OpenAndApplyAnalysis(IAnalysis analysis)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);

        var result = await analysis.AnalyzeAsync(project, fileName);

        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(json);
    }
}
