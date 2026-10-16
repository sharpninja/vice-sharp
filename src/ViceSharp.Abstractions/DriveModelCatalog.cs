namespace ViceSharp.Abstractions;

using System.Collections.Generic;

/// <summary>
/// The catalog of <see cref="DriveModel"/> values the device-setup UI exposes. Only the
/// models with a working managed true-drive implementation are offered; the order is the
/// UI presentation order.
/// </summary>
public static class DriveModelCatalog
{
    /// <summary>
    /// The drive models ViceSharp actually implements as a true-drive rig, in UI order:
    /// <see cref="DriveModel.C1541"/> (the default), then <see cref="DriveModel.C1540"/>
    /// and <see cref="DriveModel.C1541II"/>. The 1541-II's integer value is 1542.
    /// </summary>
    public static IReadOnlyList<DriveModel> Implemented { get; } = new[]
    {
        DriveModel.C1541,
        DriveModel.C1540,
        DriveModel.C1541II,
    };
}
