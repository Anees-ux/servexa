using FluentValidation;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.TransitionWorkOrderStatus;

public sealed class TransitionWorkOrderStatusCommandValidator : AbstractValidator<TransitionWorkOrderStatusCommand>
{
    public TransitionWorkOrderStatusCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("Work order ID is required.");

        RuleFor(x => x.TargetStatus)
            .IsInEnum().WithMessage("Invalid target operational status.");

        RuleFor(x => x.PauseReasonCode)
            .NotEmpty().WithMessage("Pause reason code is required when pausing a work order.")
            .When(x => x.TargetStatus == WorkOrderOperationalStatus.Paused);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required when cancelling a work order.")
            .When(x => x.TargetStatus == WorkOrderOperationalStatus.Cancelled);
    }
}
