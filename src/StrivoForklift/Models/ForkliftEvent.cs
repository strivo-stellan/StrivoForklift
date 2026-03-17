namespace StrivoForklift.Models;

/// <summary>
/// Represents a forklift event record stored in the database.
/// Each record corresponds to a unique forklift/entity identifier and holds
/// the most recent (highest-timestamp) event data received from the queue.
/// </summary>
public class ForkliftEvent
{
    /// <summary>Unique identifier for the forklift or event entity (primary key).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Timestamp of the most recent event applied to this record.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Operational status from the most recent event.</summary>
    public string? Status { get; set; }

    /// <summary>UTC time when this database record was last written.</summary>
    public DateTimeOffset LastUpdated { get; set; }
}
