using Microsoft.DurableTask;


[DurableTask]
class CodeExecutionOrchestrator : TaskOrchestrator<string, bool>
{

    public override async Task<bool> RunAsync(TaskOrchestrationContext context, string codeToEvaluate)
    {


        // Step 1: Execute code in dynamic session
        var submissionResult = await context.CallRunCodeInSessionAsync(codeToEvaluate);

        // Make the result available via custom status
        context.SetCustomStatus(submissionResult);

        // Create a durable timer for the timeout
        DateTime timeoutDeadline = context.CurrentUtcDateTime.AddHours(24);

        using var timeoutCts = new CancellationTokenSource();

        // Set up the timeout task that we can cancel if approval comes before timeout
        Task timeoutTask = context.CreateTimer(timeoutDeadline, timeoutCts.Token);

        // Wait for an external event (approval/rejection)
        string approvalEventName = "HumanApproval";
        Task<bool> approvalTask = context.WaitForExternalEvent<bool>(approvalEventName);

        // Wait for either the timeout or the approval response, whichever comes first
        Task completedTask = await Task.WhenAny(approvalTask, timeoutTask);

        // Step 2: Send result for human approval (external event wait)
        bool humanApproved;
        if (completedTask == approvalTask)
        {
            humanApproved = approvalTask.Result;

            // Cancel the timeout task since we got a response
            timeoutCts.Cancel();
        }
        else
        {
            // Auto-reject if timeout
            humanApproved = false;
        }

        return humanApproved;
    }
}

[DurableTask]
class RunCodeInSession : TaskActivity<string, string>
{
    private readonly CodeExecutionActivities codeExecutionActivities;

    public RunCodeInSession(CodeExecutionActivities codeExecutionActivities)
    {
        this.codeExecutionActivities = codeExecutionActivities;
    }

    public override async Task<string> RunAsync(TaskActivityContext context, string codeToEvaluate)
    {
        // Call the activity to run the code in a session
        return await codeExecutionActivities.RunCodeInSession(codeToEvaluate);
    }
}
