using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wordslab.manager.vm;

namespace wordslab.manager.test.vm
{
    public class TestProcessUI : InstallProcessUI
    {
        public Queue<string> Answers;

        public TestProcessUI(string[] answers = null)
        {
            if(answers != null)
            {
                Answers = new Queue<string>(answers);
            }
            else
            {
                Answers = new Queue<string>();
            }
        }

        public List<string> Messages = new List<string>();

        public void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription)
        {
            Messages.Add($"Step {stepNumber}/{totalSteps}: {stepDescription}");
        }

        public void DisplayInformationLine(string text)
        {
            Messages.Add(text);
        }

        private int lastCommandId = -1;
        private List<string> commandDescriptions = new List<string>();

        public int DisplayCommandLaunch(string commandDescription)
        {
            var commandId = commandDescriptions.Count;
            lastCommandId = commandId;
            commandDescriptions.Add(commandDescription);
            Messages.Add(commandDescription);
            return commandId;
        }

        public void RunCommandsAndDisplayProgress(LongRunningCommand[] commands)
        {
            lastCommandId = -1;
            var runningTasks = new List<Task>();
            foreach (var command in commands)
            {
                command.Id = commandDescriptions.Count;
                commandDescriptions.Add(command.Description);
                var runTask = command.RunFunction(val => Messages.Add($"{command.Description} : {val}/{command.MaxValue}"));
                runningTasks.Add(runTask);
            }
            Task.WaitAll(runningTasks.ToArray());
            foreach (var command in commands)
            {
                if (command.CheckFunction != null)
                {
                    command.CheckFunction(success =>
                    {
                        string res = success ? "OK" : "ERROR";
                        Messages.Add($"{command.Description}: {res}");
                    });
                }
            }
        }

        public void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null)
        {
            string msg = "";
            if (commandId != lastCommandId)
            {
                msg += commandDescriptions[commandId];
                msg += ": ";
            }
            msg += (success ? "OK" : "ERROR");
            Messages.Add(msg);
            if (!String.IsNullOrEmpty(resultInfo))
            {
                Messages.Add(resultInfo);
            }
            if (!String.IsNullOrEmpty(errorMessage))
            {
                Messages.Add(errorMessage);
            }
        }

        public Task<bool> DisplayQuestionAsync(string question, bool defaultValue = true)
        {
            Messages.Add(question);
            var answer = defaultValue; 
            if(Answers.Count > 0)
            {
                answer = Boolean.Parse(Answers.Dequeue());
            }
            return Task.FromResult(answer);
        }

        public Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent)
        {
            Messages.Add(scriptDescription);
            Messages.Add("---");
            Messages.Add($"{scriptContent}");
            Messages.Add("---");
            Messages.Add("OK to execute this script as admin?");
            var answer = true;
            if (Answers.Count > 0)
            {
                answer = Boolean.Parse(Answers.Dequeue());
            }
            return Task.FromResult(answer);
        }

        public Task<string> DisplayInputQuestionAsync(string question, string defaultValue)
        {
            Messages.Add($"{question}");
            Messages.Add($"Default value: {defaultValue}");
            var answer = defaultValue;
            if (Answers.Count > 0)
            {
                answer = Answers.Dequeue();
            }
            return Task.FromResult(answer);
        }

        public void DisplayCommandError(string errorMessage)
        {
            Messages.Add("Unexpected ERROR:");
            Messages.Add(errorMessage);
        }
    }
}
