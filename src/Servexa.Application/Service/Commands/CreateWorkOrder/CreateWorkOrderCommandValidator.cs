using FluentValidation;

namespace Servexa.Application.Service.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.ServiceAccountId)
            .NotEmpty().WithMessage("Service account ID is required.");

        RuleFor(x => x.PrimarySiteId)
            .NotEmpty().WithMessage("Primary site ID is required.");

        RuleFor(x => x.WorkTypeCode)
            .NotEmpty().WithMessage("Work type code is required.")
            .MaximumLength(50).WithMessage("Work type code cannot exceed 50 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid work order priority.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Summary is required.")
            .MaximumLength(300).WithMessage("Summary cannot exceed 300 characters.");

        RuleFor(x => x.WorkOrderNumber)
            .MaximumLength(50).WithMessage("Work order number cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.WorkOrderNumber));
    }
}
