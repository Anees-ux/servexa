using FluentValidation;

namespace Servexa.Application.Scheduling.Commands.AssignResourceToBooking;

public sealed class AssignResourceToBookingCommandValidator : AbstractValidator<AssignResourceToBookingCommand>
{
    public AssignResourceToBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required.");

        RuleFor(x => x.ResourceId)
            .NotEmpty()
            .WithMessage("Resource ID is required.");

        RuleFor(x => x.SelectionRationale)
            .MaximumLength(500)
            .WithMessage("Selection rationale must not exceed 500 characters.");
    }
}
