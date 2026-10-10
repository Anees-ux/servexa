using FluentValidation;

namespace Servexa.Application.Scheduling.Commands.RescheduleBooking;

public sealed class RescheduleBookingCommandValidator : AbstractValidator<RescheduleBookingCommand>
{
    public RescheduleBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required.");

        RuleFor(x => x.NewStartUtc)
            .NotEmpty()
            .WithMessage("New start time is required.");

        RuleFor(x => x.NewEndUtc)
            .NotEmpty()
            .WithMessage("New end time is required.")
            .GreaterThan(x => x.NewStartUtc)
            .WithMessage("New end time must be after new start time.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reschedule reason is required.")
            .MaximumLength(500)
            .WithMessage("Reschedule reason must not exceed 500 characters.");
    }
}
