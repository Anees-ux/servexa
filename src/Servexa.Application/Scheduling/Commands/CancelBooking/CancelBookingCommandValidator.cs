using FluentValidation;

namespace Servexa.Application.Scheduling.Commands.CancelBooking;

public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required.")
            .MaximumLength(1000)
            .WithMessage("Cancellation reason must not exceed 1000 characters.");
    }
}
