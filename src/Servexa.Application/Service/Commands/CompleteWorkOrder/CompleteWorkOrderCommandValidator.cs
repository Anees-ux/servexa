using FluentValidation;

namespace Servexa.Application.Service.Commands.CompleteWorkOrder;

public sealed class CompleteWorkOrderCommandValidator : AbstractValidator<CompleteWorkOrderCommand>
{
    public CompleteWorkOrderCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("WorkOrderId is required.");

        RuleFor(x => x.CommandId)
            .NotEmpty().WithMessage("CommandId is required for durable idempotency.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
    }
}
