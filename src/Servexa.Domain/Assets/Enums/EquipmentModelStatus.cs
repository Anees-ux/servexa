namespace Servexa.Domain.Assets.Enums;

/// <summary>
/// Lifecycle status of an EquipmentModel.
/// Stored as smallint in physical persistence.
/// </summary>
public enum EquipmentModelStatus : short
{
    Active = 1,
    Retired = 2
}
