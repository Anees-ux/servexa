using FluentValidation;

namespace Servexa.Application.Field.Commands.CompleteTechnicianExecution;

public sealed class CompleteTechnicianExecutionCommandValidator : AbstractValidator<CompleteTechnicianExecutionCommand>
{
    public CompleteTechnicianExecutionCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty()
            .WithMessage("Assignment ID is required.");

        RuleFor(x => x.WorkSummary)
            .NotEmpty()
            .WithMessage("Work summary is required upon completion.")
            .MaximumLength(2000)
            .WithMessage("Work summary must not exceed 2000 characters.");
    }
}
