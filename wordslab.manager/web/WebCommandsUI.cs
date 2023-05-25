namespace wordslab.manager.web
{
    public enum WebCommandsState
    {
        NotStarted,
        Running,
        Executed
    }

    public class WebCommandsUI : ICommandsUI
    {
        private Action RefreshDisplay;

        public WebCommandsUI(Action invokeAsyncStateHasChanged) 
        { 
            RefreshDisplay = invokeAsyncStateHasChanged;
        }

        public WebCommandsState State { get; private set; }

        private DateTime startTime;
        private DateTime? stopTime;

        public void Start()
        {
            startTime = DateTime.Now;
            stopTime = null;
            State = WebCommandsState.Running;
            RefreshDisplay();
        }

        public void Stop()
        {
            stopTime = DateTime.Now;
            State = WebCommandsState.Executed;
            RefreshDisplay();
        }

        public string ExecutionTime { get { 
                if (State == WebCommandsState.NotStarted)
                {
                    return string.Empty;
                } 
                else 
                {
                    var duration = stopTime.HasValue ? stopTime.Value - startTime : DateTime.Now - startTime;
                    return duration.ToString("mm\\:ss");
                }
        } }

        public List<WebUIElementInfo> Elements = new List<WebUIElementInfo>();

        public void WriteLine()
        {
            var elementInfo = WebUIElementInfo.Line(null);
            Elements.Add(elementInfo);
            RefreshDisplay();
        }

        public void WriteLine(string line)
        {
            var elementInfo = WebUIElementInfo.Line(line);
            Elements.Add(elementInfo);
            RefreshDisplay();
        }

        public void DisplayTable(TableInfo tableInfo)
        {
            var elementInfo = WebUIElementInfo.Table(tableInfo);
            Elements.Add(elementInfo);
            RefreshDisplay();
        }

        public void DisplayInstallStep(int stepNumber, int totalSteps, string stepDescription)
        {
            var elementInfo = WebUIElementInfo.InstallStep(stepNumber, totalSteps, stepDescription);
            Elements.Add(elementInfo);
            RefreshDisplay();
        }

        private List<WebUIElementInfo> commands = new List<WebUIElementInfo>();

        public int DisplayCommandLaunch(string commandDescription)
        {
            var commandId = commands.Count;
            var elementInfo = WebUIElementInfo.CommandLaunch(commandDescription);
            Elements.Add(elementInfo);
            commands.Add(elementInfo);
            RefreshDisplay();
            return commandId;
        }

        public void RunCommandsAndDisplayProgress(LongRunningCommand[] longRunningCommands)
        {
            var longRunningCommandsElements = new List<WebUIElementInfo>();
            foreach(var command in longRunningCommands)
            {
                var elementInfo = WebUIElementInfo.CommandWithProgress(command);
                longRunningCommandsElements.Add(elementInfo);
                Elements.Add(elementInfo);
            }
            RefreshDisplay();

            var runningTasks = new List<Task>();
            int elementId = 0;
            foreach (var command in longRunningCommands)
            {
                var elementInfo = longRunningCommandsElements[elementId];
                var runTask = Task.Run(() => command.RunFunction(val => { elementInfo.CommandProgress = val; RefreshDisplay(); }));
                runningTasks.Add(runTask);
                elementId++;
            }
            Task.WaitAll(runningTasks.ToArray());

            elementId = 0;
            foreach (var command in longRunningCommands)
            {

                var elementInfo = longRunningCommandsElements[elementId];
                if (command.CheckFunction != null)
                {
                    command.CheckFunction(success => { elementInfo.CommandResult = success; });
                }
                elementId++;
            }
            RefreshDisplay();
        }

        public void DisplayCommandResult(int commandId, bool success, string? resultInfo = null, string? errorMessage = null)
        {
            var elementInfo = commands[commandId];
            WebUIElementInfo.SetCommandResult(elementInfo, success, resultInfo, errorMessage);
            RefreshDisplay();
        }

        public Task<bool> DisplayQuestionAsync(string question, bool defaultValue = true)
        {
            var elementInfo = WebUIElementInfo.ChoiceQuestion(question, defaultValue);
            Elements.Add(elementInfo);
            RefreshDisplay();
            return elementInfo.SendChoiceAnswer.Task;
        }

        public Task<bool> DisplayAdminScriptQuestionAsync(string scriptDescription, string scriptContent)
        {
            var elementInfo = WebUIElementInfo.AdminScriptQuestion(scriptDescription, scriptContent);
            Elements.Add(elementInfo);
            RefreshDisplay();
            return elementInfo.SendChoiceAnswer.Task;
        }

        public Task<string> DisplayInputQuestionAsync(string question, string defaultValue)
        {
            var elementInfo = WebUIElementInfo.InputQuestion(question, defaultValue);
            Elements.Add(elementInfo);
            RefreshDisplay();
            return elementInfo.SendInputAnswer.Task;
        }

        public void DisplayCommandError(string errorMessage)
        {
            var elementInfo = WebUIElementInfo.CommandError(errorMessage);
            Elements.Add(elementInfo);
            RefreshDisplay();
        }
    }

    
    public enum WebUIElementType
    {
        Line,
        Table,
        InstallStep,
        Command,
        CommandWithProgress,
        CommandError,
        ChoiceQuestion,
        AdminScriptQuestion,
        InputQuestion
    }

    public class WebUIElementInfo
    {
        public WebUIElementInfo(WebUIElementType type)
        {
            Type = type;
        }

        public WebUIElementType Type { get; private set; }

        public string Text { get; private set; }

        public TableInfo TableInfo { get; private set; }

        public string ScriptContent { get; private set; }

        public LongRunningCommand LongRunningCommand { get; private set; }

        public double CommandProgress { get; set; } 

        public bool? CommandResult { get; set; }

        public string CommandResultInfo{ get; private set; }

        public string CommandResultError { get; private set; }

        public bool? DefaultChoiceAnswer { get; private set; }

        public TaskCompletionSource<bool> SendChoiceAnswer { get; private set; }

        public string DefaultInputAnswer { get; private set; }

        public TaskCompletionSource<string> SendInputAnswer { get; private set; }

        public static WebUIElementInfo Line(string line)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.Line);
            elementInfo.Text = line;
            return elementInfo;
        }

        public static WebUIElementInfo Table(TableInfo tableInfo)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.Table);
            elementInfo.TableInfo = tableInfo;
            return elementInfo;
        }

        public static WebUIElementInfo InstallStep(int stepNumber, int totalSteps, string stepDescription)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.InstallStep);  
            elementInfo.Text = $"Step {stepNumber}/{totalSteps}: {stepDescription}";
            return elementInfo;
        }

        public static WebUIElementInfo CommandLaunch(string commandDescription)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.Command);
            elementInfo.Text = commandDescription;
            return elementInfo;
        }

        public static void SetCommandResult(WebUIElementInfo elementInfo, bool success, string? resultInfo = null, string? errorMessage = null)
        {
            elementInfo.CommandResult = success;
            elementInfo.CommandResultInfo = resultInfo;
            elementInfo.CommandResultError = errorMessage;
        }

        public static WebUIElementInfo CommandError(string errorMessage)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.CommandError);
            elementInfo.CommandResultError = errorMessage;
            return elementInfo;
        }

        public static WebUIElementInfo CommandWithProgress(LongRunningCommand command)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.CommandWithProgress);
            elementInfo.LongRunningCommand = command;
            return elementInfo;
        }

        public static WebUIElementInfo ChoiceQuestion(string question, bool? defaultValue)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.ChoiceQuestion);
            elementInfo.Text = question;
            elementInfo.DefaultChoiceAnswer = defaultValue;
            elementInfo.SendChoiceAnswer = new TaskCompletionSource<bool>();
            return elementInfo;
        }

        public static WebUIElementInfo AdminScriptQuestion(string scriptDescription, string scriptContent)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.AdminScriptQuestion);
            elementInfo.Text = scriptDescription;
            elementInfo.ScriptContent = scriptContent;
            elementInfo.SendChoiceAnswer = new TaskCompletionSource<bool>();
            return elementInfo;
        }

        public static WebUIElementInfo InputQuestion(string question, string defaultValue)
        {
            var elementInfo = new WebUIElementInfo(WebUIElementType.InputQuestion);
            elementInfo.Text = question;
            elementInfo.DefaultInputAnswer = defaultValue;
            elementInfo.SendInputAnswer = new TaskCompletionSource<string>();
            return elementInfo;
        }
    }
}
