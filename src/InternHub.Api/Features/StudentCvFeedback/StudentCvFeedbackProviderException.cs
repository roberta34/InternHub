namespace InternHub.Api.Features.StudentCvFeedback;

public enum StudentCvFeedbackFailureKind
{
    ProviderUnavailable,
    Timeout,
    InvalidEvaluation
}

public sealed class StudentCvFeedbackProviderException : Exception
{
    public StudentCvFeedbackFailureKind Kind { get; }

    public StudentCvFeedbackProviderException(
        StudentCvFeedbackFailureKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }
        
}