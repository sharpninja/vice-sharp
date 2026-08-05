namespace ViceSharp.RomM;

/// <summary>
/// TR-ROMM-JSON-001. Wire DTO for a RomM collection (GET /api/collections responses). RomM.Client does
/// not type this endpoint, so the adapter owns the DTO and deserializes it via the source-generated
/// <see cref="RomMJsonContext"/> (SnakeCaseLower).
/// </summary>
internal sealed class RomMCollectionDto
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int RomCount { get; set; }

    public List<int>? RomIds { get; set; }

    public bool IsSmart { get; set; }

    public bool IsVirtual { get; set; }

    public bool IsPublic { get; set; }

    public bool IsFavorite { get; set; }
}

/// <summary>
/// TR-ROMM-JSON-001. Request body for POST/DELETE /api/collections/{id}/roms (application/json).
/// </summary>
internal sealed class RomMCollectionRomsPayload
{
    public List<int> RomIds { get; set; } = new();
}
