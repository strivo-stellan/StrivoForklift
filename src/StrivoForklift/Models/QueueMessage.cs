using System.Text.Json.Serialization;

namespace StrivoForklift.Models;

/// <summary>
/// Represents a forklift event message received from the Azure Storage Queue.
/// </summary>
public class QueueMessage
{
    /// <summary>Unique identifier for the forklift or event entity.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Timestamp of the event, used to determine message recency.</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Operational status of the forklift (e.g. "active", "idle", "charging").</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Current location of the forklift (e.g. "warehouse-A", "dock-3").</summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }
}
