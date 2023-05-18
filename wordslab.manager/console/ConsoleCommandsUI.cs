using Spectre.Console;

namespace wordslab.manager.console
{
    public class ConsoleCommandsUI : ICommandsUI
    {
        public void WriteLine()
        {
            AnsiConsole.WriteLine();
        }

        public void WriteLine(string line)
        {
            AnsiConsole.WriteLine(line);
        }

        public void DisplayTable(TableInfo tableInfo)
        {
            var table = new Table();
            foreach(var column in tableInfo.Columns) { table.AddColumn(column); }
            foreach(var row  in tableInfo.Rows) { table.AddRow(row); }
            AnsiConsole.Write(table);
        }

        public void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription)
        {
            AnsiConsole.MarkupLine($"[invert]Step {stepNumber}/{totalSteps}: {stepDescription.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine("");
        }

        private int lastCommandId = -1;
        private List<string> commandDescriptions = new List<string>();

        public int DisplayCommandLaunch(string commandDescription)
        {
            var commandId = commandDescriptions.Count;
            lastCommandId = commandId;
            commandDescriptions.Add(commandDescription);
            AnsiConsole.WriteLine(commandDescription);
            return commandId;
        }

        public void RunCommandsAndDisplayProgress(LongRunningCommand[] commands)
        {
            lastCommandId = -1;
            AnsiConsole.Progress().Start(ctx =>
            {
                var runningTasks = new List<Task>();
                foreach (var command in commands)
                {
                    command.Id = commandDescriptions.Count;
                    commandDescriptions.Add(command.Description);
                    var uiTask = ctx.AddTask(command.Description, maxValue: command.MaxValue);
                    var runTask = command.RunFunction(val => uiTask.Value = val);
                    runningTasks.Add(runTask);
                }
                Task.WaitAll(runningTasks.ToArray());
            });
            AnsiConsole.WriteLine();
            foreach (var command in commands)
            {
                if (command.CheckFunction != null)
                {
                    command.CheckFunction(success =>
                    {
                        AnsiConsole.Write(command.Description);
                        AnsiConsole.Write(": ");
                        AnsiConsole.MarkupLine(success ? "[green]OK[/]" : "[red]ERROR[/]");
                    });
                }
            }
            AnsiConsole.WriteLine();
        }

        public void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null)
        {
            if (commandId != lastCommandId)
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

        public Task<bool> DisplayQuestionAsync(string question, bool defaultValue = true)
        {
            var answer = AnsiConsole.Confirm(question, defaultValue);
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

        public Task<string> DisplayInputQuestionAsync(string question, string defaultValue)
        {
            var answer = AnsiConsole.Ask(question, defaultValue);
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
