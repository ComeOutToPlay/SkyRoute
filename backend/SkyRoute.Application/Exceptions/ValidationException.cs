namespace SkyRoute.Application.Exceptions;

// Covers the three 400 failure modes named in docs/03-execution-plan.md Phase 3: unknown
// airport code, mismatched passenger count, invalid document number for the route type.
// FluentValidation was never adopted in this solution (not referenced in any .csproj, not
// part of the approved architecture baseline in docs/02-revision.md or
// docs/03-execution-plan.md), so this is the project's own minimal validation exception
// rather than reusing a third-party type, per the plan's "(or reuse FluentValidation's if
// adopted)" fallback. Mapped to HTTP 400 by the WebApi exception middleware (Phase 5).
public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this(error, new Dictionary<string, string[]> { [field] = [error] })
    {
    }
}
