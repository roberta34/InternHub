namespace InternHub.Tests.AI.Evaluation.Running;

/// <summary>Thrown when the evaluation setup is incomplete; never recorded as a provider outcome.</summary>
public sealed class EvaluationSetupException(string message) : Exception(message);
