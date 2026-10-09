using FluentValidation;

namespace Servexa.Application.Customers.Commands.CreateAccount;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(50).WithMessage("Account number must not exceed 50 characters.");

        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(200).WithMessage("Legal name must not exceed 200 characters.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be exactly 3 characters.");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).When(x => x.PaymentTermsDays.HasValue)
            .WithMessage("Payment terms days must be non-negative.");
    }
}
