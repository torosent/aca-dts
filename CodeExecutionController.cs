using Microsoft.AspNetCore.Mvc;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

[Route("code")]
[ApiController]
public partial class CodeEvaluationController(
    DurableTaskClient durableTaskClient,
    ILogger<CodeEvaluationController> logger) : ControllerBase
{
    readonly DurableTaskClient durableTaskClient = durableTaskClient;

    /// <summary>
    /// This method is called to start the code execution process.
    /// It takes the code to be executed as input and starts a new orchestration instance.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    [HttpPost("execute")]
    public async Task<ActionResult> RunCodeExecution([FromBody] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Code cannot be null or empty.");
        }

        // Start the orchestration to evaluate the code
        string instanceId = Guid.NewGuid().ToString();

        await this.durableTaskClient.ScheduleNewCodeExecutionOrchestratorInstanceAsync(code, new StartOrchestrationOptions
        {
            InstanceId = instanceId
        });

        return this.Ok(new
        {
            RequestId = $"{instanceId}",
        });
    }

    /// <summary>
    /// Approve or reject the code execution request.
    /// This method is called by the human approver to either approve or reject the code execution.
    /// </summary>
    /// <param name="approve"></param>
    /// <param name="requestId"></param>
    /// <returns></returns>
    [HttpPost("review")]
    public async Task<ActionResult> ApproveCodeExecution([FromQuery] bool approve, [FromQuery] string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return BadRequest("Request ID cannot be null or empty.");

        // Raise an event to the orchestration instance for human approval
        await durableTaskClient.RaiseEventAsync(requestId, "HumanApproval", approve);

        return Ok(new { message = $"Code execution {(approve ? "approved" : "rejected")}." });
    }
}