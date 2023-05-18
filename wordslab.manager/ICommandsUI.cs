using Spectre.Console.Cli;

namespace wordslab.manager
{
    public abstract class CommandWithUI<TSettings> : Command<TSettings> where TSettings : CommandSettings
    {
        private ICommandsUI ui;

        public CommandWithUI(ICommandsUI ui)
        {
            this.ui = ui;
        }

        public ICommandsUI UI { get { return ui; } }
    }

    public interface ICommandsUI
    {
        // Raw

        void WriteLine();

        void WriteLine(string line);

        void DisplayTable(TableInfo tableInfo);

        // Install process

        void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription);

        int DisplayCommandLaunch(string commandDescription);

        void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null);

        void DisplayCommandError(string errorMessage);

        void RunCommandsAndDisplayProgress(LongRunningCommand[] commands);

        Task<bool> DisplayQuestionAsync(string question, bool defaultValue = true);

        Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent);

        Task<string> DisplayInputQuestionAsync(string question, string defaultValue);
    }

    public class TableInfo
    {
        public List<string> Columns = new List<string>();
        public List<string[]> Rows = new List<string[]>();

        public void AddColumn(string column) { Columns.Add(column); }    

        public void AddRow(params string[] row) { Rows.Add(row); }   
    } 

    public delegate Task RunAndDisplayProgress(Action<double> displayProgress);

    public delegate void CheckAndDisplayResult(Action<bool> displayResult);

    public class LongRunningCommand
    {
        public LongRunningCommand(string commandDescription, double maxValue, string unit, RunAndDisplayProgress runFunction, CheckAndDisplayResult checkFunction)
        {
            Description = commandDescription;
            MaxValue = maxValue;
            Unit = unit;
            RunFunction = runFunction;
            CheckFunction = checkFunction;
        }

        public int Id;

        public string Description { get; init; }
        public double MaxValue { get; init; }
        public string Unit { get; init; }

        public RunAndDisplayProgress RunFunction { get; init; }

        public CheckAndDisplayResult CheckFunction { get; init; }
    }
}
