using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace RoslynAnalysis;

public class CoverageAnalysis(string[] specificFiles) : IAnalysis
{
    public static IAnalysis Create(string[] args)
    {
        return new CoverageAnalysis(args);
    }

    public async Task<object> AnalyzeAsync(Project project, string fileName)
    {
        var projectPath = project.FilePath;
        if (string.IsNullOrEmpty(projectPath))
        {
            return new { success = false, output = "No project file path available" };
        }

        var result = await RunDotnetTestWithCoverage(projectPath);

        if (!result.success)
        {
            return result;
        }

        var formattedOutput = FormatCoverageOutput(result.output, specificFiles);
        return new { success = true, output = formattedOutput };
    }

    private async Task<dynamic> RunDotnetTestWithCoverage(string projectPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{projectPath}\" --collect:\"XPlat Code Coverage\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new { success = false, output = "Failed to start dotnet test process" };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var fullOutput = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";

            return new { success = process.ExitCode == 0, output = fullOutput };
        }
        catch (Exception ex)
        {
            return new { success = false, output = $"Error running dotnet test: {ex.Message}" };
        }
    }

    private string FormatCoverageOutput(string rawOutput, string[] specificFiles)
    {
        var coverageFileMatch = Regex.Match(rawOutput, @"[^\s]*coverage\.cobertura\.xml");
        if (!coverageFileMatch.Success)
        {
            return rawOutput + "\n\nNo coverage file found in output.";
        }

        var coverageFilePath = coverageFileMatch.Value;

        try
        {
            var xmlContent = File.ReadAllText(coverageFilePath);
            var coverageData = ParseCoverageXml(xmlContent);

            if (specificFiles.Length > 0)
            {
                coverageData = FilterCoverageData(coverageData, specificFiles);
            }

            return FormatCoverageData(coverageData);
        }
        catch (Exception ex)
        {
            return $"Error parsing coverage: {ex.Message}";
        }
    }

    private Dictionary<string, FileData> ParseCoverageXml(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var coverageData = new Dictionary<string, FileData>();

        var packages = doc.Descendants("package");
        foreach (var package in packages)
        {
            var classes = package.Descendants("class");
            foreach (var classElement in classes)
            {
                var filename = classElement.Attribute("filename")?.Value;
                if (string.IsNullOrEmpty(filename))
                {
                    continue;
                }

                var className = classElement.Attribute("name")?.Value ?? "";
                var linesData = new List<LineData>();

                var linesContainer = classElement.Element("lines");
                if (linesContainer != null)
                {
                    var lines = linesContainer.Elements("line");
                    foreach (var line in lines)
                    {
                        var lineNumberStr = line.Attribute("number")?.Value;
                        var hitsStr = line.Attribute("hits")?.Value;
                        var branchStr = line.Attribute("branch")?.Value;
                        var conditionCoverageStr = line.Attribute("condition-coverage")?.Value;

                        if (int.TryParse(lineNumberStr, out var lineNumber) &&
                            int.TryParse(hitsStr, out var hits))
                        {
                            var status = DetermineCoverageStatus(hits, branchStr, conditionCoverageStr);
                            linesData.Add(new LineData
                            {
                                Number = lineNumber,
                                Status = status
                            });
                        }
                    }
                }

                coverageData[filename] = new FileData
                {
                    ClassName = className,
                    Lines = linesData
                };
            }
        }

        return coverageData;
    }

    private CoverageStatus DetermineCoverageStatus(int hits, string? branchStr, string? conditionCoverageStr)
    {
        if (hits == 0)
        {
            return CoverageStatus.NotCovered;
        }

        if (branchStr == "true" && !string.IsNullOrEmpty(conditionCoverageStr))
        {
            var match = System.Text.RegularExpressions.Regex.Match(conditionCoverageStr, @"(\d+)%\s*\((\d+)/(\d+)\)");
            if (match.Success && int.TryParse(match.Groups[2].Value, out var covered) && int.TryParse(match.Groups[3].Value, out var total))
            {
                if (covered > 0 && covered < total)
                {
                    return CoverageStatus.PartiallyCovered;
                }
            }
        }

        return CoverageStatus.FullyCovered;
    }

    private string FormatCoverageData(Dictionary<string, FileData> coverageData)
    {
        if (coverageData.Count == 0)
        {
            return "No coverage data found.";
        }

        var outputLines = new List<string>
        {
            "Coverage Legend:",
            "🔴 Not covered",
            "🟡 Partly covered",
            ""
        };

        foreach (var kvp in coverageData)
        {
            var filename = kvp.Key;
            var fileData = kvp.Value;
            var uncoveredLines = fileData.Lines.Where(line => line.Status == CoverageStatus.NotCovered).ToList();
            var partlyCoveredLines = fileData.Lines.Where(line => line.Status == CoverageStatus.PartiallyCovered).ToList();

            if (uncoveredLines.Count > 0 || partlyCoveredLines.Count > 0)
            {
                outputLines.Add($"{filename}:");

                foreach (var line in uncoveredLines)
                {
                    outputLines.Add($" 🔴 L{line.Number}");
                }

                foreach (var line in partlyCoveredLines)
                {
                    outputLines.Add($" 🟡 L{line.Number}");
                }
            }
        }

        return outputLines.Count > 4 ? string.Join("\n", outputLines) : "Coverage Legend:\n🔴 Not covered\n🟡 Partly covered\n\nAll lines are fully covered!";
    }

    private Dictionary<string, FileData> FilterCoverageData(Dictionary<string, FileData> coverageData, string[] specificFiles)
    {
        var filteredData = new Dictionary<string, FileData>();

        foreach (var kvp in coverageData)
        {
            var filename = kvp.Key;
            var fileData = kvp.Value;

            foreach (var requestedFile in specificFiles)
            {
                if (filename.EndsWith(requestedFile) || filename.Contains(requestedFile))
                {
                    filteredData[filename] = fileData;
                    break;
                }
            }
        }

        return filteredData;
    }

    private class FileData
    {
        public string ClassName { get; set; } = "";
        public List<LineData> Lines { get; set; } = [];
    }

    private class LineData
    {
        public int Number { get; set; }
        public CoverageStatus Status { get; set; }
    }

    private enum CoverageStatus
    {
        NotCovered,
        PartiallyCovered,
        FullyCovered
    }
}
