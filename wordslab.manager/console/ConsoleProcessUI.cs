using Spectre.Console;
using wordslab.manager.vm;

namespace wordslab.manager.console
{
    public class ConsoleProcessUI : InstallProcessUI
    {
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

        public Task<string> DisplayInputQuestion(string question, string defaultValue)
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

    public static class AsyncUtil
    {
        private static readonly TaskFactory _taskFactory = new
            TaskFactory(CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);

        /// <summary>
        /// Executes an async Task method which has a void return value synchronously
        /// USAGE: AsyncUtil.RunSync(() => AsyncMethod());
        /// </summary>
        /// <param name="task">Task method to execute</param>
        public static void RunSync(Func<Task> task)
            => _taskFactory
                .StartNew(task)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        /// <summary>
        /// Executes an async Task<T> method which has a T return type synchronously
        /// USAGE: T result = AsyncUtil.RunSync(() => AsyncMethod<T>());
        /// </summary>
        /// <typeparam name="TResult">Return Type</typeparam>
        /// <param name="task">Task<T> method to execute</param>
        /// <returns></returns>
        public static TResult RunSync<TResult>(Func<Task<TResult>> task)
            => _taskFactory
                .StartNew(task)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
    }
}
