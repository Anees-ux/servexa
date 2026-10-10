using FluentValidation;

namespace Servexa.Application.Scheduling.Commands.UnassignResourceFromBooking;

public sealed class UnassignResourceFromBookingCommandValidator : AbstractValidator<UnassignResourceFromBookingCommand>
{
    public UnassignResourceFromBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required.");

        RuleFor(x => x.AssignmentId)
            .NotEmpty()
            .WithMessage("Assignment ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Unassign reason is required.")
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters.");
    }
}
