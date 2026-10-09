using FluentValidation;

namespace Servexa.Application.Assets.Commands.ChangeAssetStatus;

public sealed class ChangeAssetStatusCommandValidator : AbstractValidator<ChangeAssetStatusCommand>
{
    public ChangeAssetStatusCommandValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset ID is required.");

        RuleFor(x => x.TargetStatus)
            .IsInEnum().WithMessage("Invalid target asset status.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
