using FluentValidation;

namespace Servexa.Application.Scheduling.Commands.CreateBooking;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty()
            .WithMessage("Work order ID is required.");

        RuleFor(x => x.PlannedStartUtc)
            .NotEmpty()
            .WithMessage("Planned start time is required.");

        RuleFor(x => x.PlannedEndUtc)
            .NotEmpty()
            .WithMessage("Planned end time is required.")
            .GreaterThan(x => x.PlannedStartUtc)
            .WithMessage("Planned end time must be after planned start time.");

        RuleFor(x => x.SchedulingNotes)
            .MaximumLength(1000)
            .WithMessage("Scheduling notes must not exceed 1000 characters.");
    }
}
