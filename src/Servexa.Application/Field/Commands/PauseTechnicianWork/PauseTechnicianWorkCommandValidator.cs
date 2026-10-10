using FluentValidation;

namespace Servexa.Application.Field.Commands.PauseTechnicianWork;

public sealed class PauseTechnicianWorkCommandValidator : AbstractValidator<PauseTechnicianWorkCommand>
{
    public PauseTechnicianWorkCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty()
            .WithMessage("Assignment ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Pause reason is required.")
            .MaximumLength(500)
            .WithMessage("Pause reason must not exceed 500 characters.");
    }
}
