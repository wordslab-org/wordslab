namespace wordslab.manager.vm
{
    public interface InstallProcessUI
    {
        void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription);

        int DisplayCommandLaunch(string commandDescription);

        int DisplayCommandsWithProgress(LongRunningCommand[] commands);

        void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null);

        Task<bool> DisplayQuestionAsync(string question);

        Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent);

        void DisplayCommandError(string errorMessage);
    }

    public delegate void DisplayCommandProgress(double currentValue);

    public class LongRunningCommand
    {
        public string   CommandDescription;
        public double   MaxValue;
        public string   Unit;

        public Action<DisplayCommandProgress> Action;
    }
}
