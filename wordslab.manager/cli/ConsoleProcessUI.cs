using Spectre.Console;
using wordslab.manager.vm;

namespace wordslab.manager.cli
{
    public class ConsoleProcessUI : InstallProcessUI
    {
        public void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription)
        {
            AnsiConsole.MarkupLine($"[invert]Step {stepNumber}/{totalSteps}: {stepDescription.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine("");
        }

        private List<string> commandDescriptions = new List<string>();

        public int DisplayCommandLaunch(string commandDescription)
        {
            progressContext = null;
            var commandId = commandDescriptions.Count;
            commandDescriptions.Add(commandDescription);
            AnsiConsole.WriteLine(commandDescription);
            return commandId;
        }
        
        public void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null)
        {
            if(commandId != commandDescriptions.Count - 1)
            {
                AnsiConsole.Write(commandDescriptions[commandId]);
                AnsiConsole.Write(": ");
            }
            AnsiConsole.MarkupLine(success ? "[green]OK[/]" : "[red]ERROR[/]");
            if (!String.IsNullOrEmpty(resultInfo))
            {
                AnsiConsole.WriteLine(resultInfo);
            }
            if (!String.IsNullOrEmpty(errorMessage))
            {
                AnsiConsole.WriteLine(errorMessage);
            }
            AnsiConsole.WriteLine();
        }

        private ProgressContext progressContext;

        public int DisplayCommandLaunchWithProgress(string commandDescription, long maxValue, string unit)
        {
            AnsiConsole.Progress().Start(ctx =>
            {
                progressContext = ctx;
            });
        }

        public void DisplayCommandProgress(int commandId, long currentValue)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DisplayQuestionAsync(string question)
        {
            var answer = AnsiConsole.Confirm(question);
            AnsiConsole.WriteLine();
            return Task.FromResult(answer);
        }

        public Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent)
        {
            AnsiConsole.WriteLine(scriptDescription);
            AnsiConsole.WriteLine("---");
            AnsiConsole.MarkupLine($"[dim]{scriptContent.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine("---");
            var answer = AnsiConsole.Confirm("OK to execute this script as admin?");
            AnsiConsole.WriteLine();
            return Task.FromResult(answer);
        }


        public void DisplayCommandError(string errorMessage)
        {
            AnsiConsole.MarkupLine("[red]Unexpected ERROR:[/]");
            AnsiConsole.WriteLine(errorMessage);
            AnsiConsole.WriteLine();
        }
    }
}
