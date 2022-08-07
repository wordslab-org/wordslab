namespace wordslab.manager.vm
{
    public interface InstallProcessUI
    {
        void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription);

        int DisplayCommandLaunch(string commandDescription);

        void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null);

        void DisplayCommandError(string errorMessage);

        void RunCommandsAndDisplayProgress(LongRunningCommand[] commands);

        Task<bool> DisplayQuestionAsync(string question);

        Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent);
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

        public string   Description { get; init; }
        public double   MaxValue { get; init; }
        public string   Unit { get; init; }

        public RunAndDisplayProgress RunFunction { get; init; }

        public CheckAndDisplayResult CheckFunction { get; init; }
    }
}
