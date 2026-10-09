namespace Servexa.Domain.Assets.Enums;

/// <summary>
/// Tracking policy for an EquipmentModel.
/// Stored as smallint in physical persistence.
/// </summary>
public enum EquipmentTrackingPolicy : short
{
    Serialized = 1,
    NonSerialized = 2
}
