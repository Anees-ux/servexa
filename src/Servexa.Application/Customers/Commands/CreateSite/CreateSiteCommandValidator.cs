using FluentValidation;

namespace Servexa.Application.Customers.Commands.CreateSite;

public sealed class CreateSiteCommandValidator : AbstractValidator<CreateSiteCommand>
{
    public CreateSiteCommandValidator()
    {
        RuleFor(x => x.SiteNumber)
            .NotEmpty().WithMessage("Site number is required.")
            .MaximumLength(50).WithMessage("Site number must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Site name is required.")
            .MaximumLength(200).WithMessage("Site name must not exceed 200 characters.");

        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("Branch ID is required.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone ID is required.")
            .MaximumLength(100).WithMessage("Time zone ID must not exceed 100 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(200).WithMessage("Address line 1 must not exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.StateProvince)
            .NotEmpty().WithMessage("State / Province is required.")
            .MaximumLength(100).WithMessage("State / Province must not exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Length(2).WithMessage("Country code must be exactly 2 characters.");
    }
}
