using FluentValidation;

namespace Servexa.Application.Assets.Commands.CreateEquipmentModel;

public sealed class CreateEquipmentModelCommandValidator : AbstractValidator<CreateEquipmentModelCommand>
{
    public CreateEquipmentModelCommandValidator()
    {
        RuleFor(x => x.ManufacturerName)
            .NotEmpty().WithMessage("Manufacturer name is required.")
            .MaximumLength(150).WithMessage("Manufacturer name cannot exceed 150 characters.");

        RuleFor(x => x.ModelCode)
            .NotEmpty().WithMessage("Model code is required.")
            .MaximumLength(100).WithMessage("Model code cannot exceed 100 characters.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");

        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code cannot exceed 50 characters.");

        RuleFor(x => x.TrackingPolicy)
            .IsInEnum().WithMessage("Invalid equipment tracking policy.");
    }
}
