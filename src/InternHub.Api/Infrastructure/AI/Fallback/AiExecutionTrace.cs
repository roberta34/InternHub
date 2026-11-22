namespace InternHub.Api.Infrastructure.AI.Fallback;

public sealed class AiExecutionTrace
{
    public string Provider { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
}
