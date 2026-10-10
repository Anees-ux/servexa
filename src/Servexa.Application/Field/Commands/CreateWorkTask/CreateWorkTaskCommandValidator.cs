using FluentValidation;
using Servexa.Domain.Field.Enums;

namespace Servexa.Application.Field.Commands.CreateWorkTask;

public sealed class CreateWorkTaskCommandValidator : AbstractValidator<CreateWorkTaskCommand>
{
    public CreateWorkTaskCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("WorkOrderId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.Sequence)
            .GreaterThanOrEqualTo(0).WithMessage("Sequence cannot be negative.");

        RuleFor(x => x.TaskType)
            .Must(t => Enum.IsDefined(typeof(WorkTaskType), t))
            .WithMessage("TaskType must be a valid task type code.");

        RuleFor(x => x.Gate)
            .Must(g => Enum.IsDefined(typeof(WorkTaskGate), g))
            .WithMessage("Gate must be a valid gate code.");
    }
}
