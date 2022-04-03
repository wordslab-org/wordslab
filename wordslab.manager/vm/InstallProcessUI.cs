namespace wordslab.manager.vm
{
    public interface InstallProcessUI
    {
        void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription);

        int DisplayCommandLaunch(string commandDescription);

        int DisplayCommandLaunchWithProgress(string commandDescription, long maxValue, string unit);

        void DisplayCommandProgress(int commandId, long currentValue);

        void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null);

        Task<bool> DisplayQuestionAsync(string question);

        Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent);

        void DisplayCommandError(string errorMessage);
    }
}
