using FluentValidation;
using Servexa.Domain.Field.Enums;

namespace Servexa.Application.Field.Commands.UpdateWorkTaskStatus;

public sealed class UpdateWorkTaskStatusCommandValidator : AbstractValidator<UpdateWorkTaskStatusCommand>
{
    public UpdateWorkTaskStatusCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("WorkOrderId is required.");

        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("TaskId is required.");

        RuleFor(x => x.NewStatus)
            .Must(s => Enum.IsDefined(typeof(WorkTaskStatus), s))
            .WithMessage("NewStatus must be a valid work task status.");

        RuleFor(x => x.SkipReason)
            .MaximumLength(500).WithMessage("SkipReason cannot exceed 500 characters.");
    }
}
