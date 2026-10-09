using FluentValidation;

namespace Servexa.Application.Assets.Commands.CreateAsset;

public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetCommandValidator()
    {
        RuleFor(x => x.EquipmentModelId)
            .NotEmpty().WithMessage("Equipment model ID is required.");

        RuleFor(x => x.AssetNumber)
            .MaximumLength(50).WithMessage("Asset number cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.AssetNumber));

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number cannot exceed 100 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid asset status.");
    }
}
