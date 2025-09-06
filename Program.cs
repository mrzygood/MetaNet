using System.Diagnostics;
using Spectre.Console;

namespace MetaNet;

class Program
{
    private const string SnitchChoice = "Run Snitch (detect outdated packages)";
    private const string FindTodoChoice = "Find all files with TODO comments";
    private const string ExitChoice = "Exit";
    
    static int Main(string[] args)
    {
        AnsiConsole.MarkupLine("[bold dodgerblue2]MetaNet[/] - .NET project tools center");
        while (true)
        {
            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an action:")
                    .PageSize(10)
                    .AddChoices(
                        SnitchChoice,
                        FindTodoChoice,
                        ExitChoice));

            switch (choice)
            {
                case SnitchChoice:
                    RunSnitch();
                    break;
                case FindTodoChoice:
                    RunTodoSearch();
                    break;
                case ExitChoice:
                    return 0;
            }
        }
    }

    private static void RunSnitch()
    {
        const string toolCommand = "snitch";
        const string packageId = "snitch";

        if (!IsDotnetToolInstalled(toolCommand))
        {
            bool install = AnsiConsole.Confirm($"Snitch is not installed. Install [yellow]{packageId}[/] as a global tool now?", true);
            if (install is false)
            {
                AnsiConsole.MarkupLine("[yellow]Skipped installation.[/]");
                return;
            }

            bool ok = RunProcess("dotnet", $"tool install -g {packageId}");
            if (!ok)
            {
                AnsiConsole.MarkupLine("[red]Failed to install Snitch. Please install it manually and try again.[/]");
                return;
            }

            // On Windows, after a fresh install, the PATH might not be updated for the current process.
            // We try to continue; 'dotnet snitch' should work regardless.
        }

        // Execute the tool in current repository
        AnsiConsole.Write(new Rule("snitch").RuleStyle("grey").Centered());
        bool success = RunProcessPassthrough("snitch", string.Empty);

        if (success is false)
        {
            AnsiConsole.MarkupLine("[red]Snitch finished with errors.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Snitch completed successfully.[/]");
        }
    }
    
    private static void RunTodoSearch()
    {
        AnsiConsole.Write(new Rule("TODO search in repository").RuleStyle("grey").Centered());
        // Use passthrough so git controls its own colors/formatting as if run directly in terminal
        bool success = RunProcessPassthrough("git", "grep -n -i TODO");

        if (success is false)
        {
            AnsiConsole.MarkupLine("[red]Finished with errors.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Completed successfully.[/]");
        }
    }

    private static bool IsDotnetToolInstalled(string toolAlias)
    {
        var output = CaptureProcessOutput("dotnet", "tool list -g");
        if (output is null)
        {
            return false;
        }

        // Output contains a table; we do a simple contains check for alias or package name.
        return output.IndexOf(toolAlias, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // TODO deprecated in favour of RunProcessPassthrough
    private static bool RunProcess(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = psi };

            var stdOut = new List<string>();
            var stdErr = new List<string>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdOut.Add(e.Data);
                    AnsiConsole.MarkupLineInterpolated($"[grey]{Escape(e.Data)}[/]");
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdErr.Add(e.Data);
                    AnsiConsole.MarkupLineInterpolated($"[red]{Escape(e.Data)}[/]");
                }
            };

            if (!process.Start())
            {
                return false;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error running process: {Escape(ex.Message)}[/]");
            return false;
        }
    }

    // Runs a process attached to current console without redirecting output, preserving original colors
    private static bool RunProcessPassthrough(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
            };

            // Encourage colored output where tools honor FORCE_COLOR/TERM on Windows
            if (!psi.Environment.ContainsKey("NO_COLOR"))
            {
                psi.Environment["FORCE_COLOR"] = "1";
            }
            // psi.Environment["GIT_PAGER"] = "cat"; // ensure git writes to console directly

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
            {
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error running process: {Escape(ex.Message)}[/]");
            return false;
        }
    }

    private static string? CaptureProcessOutput(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // include error for diagnostics
                AnsiConsole.MarkupLineInterpolated($"[yellow]Command '{fileName} {arguments}' exited with code {process.ExitCode}. {Escape(error)}[/]");
            }

            return output + (string.IsNullOrWhiteSpace(error) ? string.Empty : ("\n" + error));
        }
        catch
        {
            return null;
        }
    }

    private static string Escape(string input)
        => Markup.Escape(input);
}