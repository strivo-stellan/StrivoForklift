using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StrivoForklift.Data;
using StrivoForklift.Models;

namespace StrivoForklift;

/// <summary>
/// Azure Function triggered by messages on the "consumethis" Azure Storage Queue.
/// Each message is deserialized as a <see cref="QueueMessage"/> and upserted into the
/// database: a new record is inserted if none exists for that ID, or the record is
/// updated only when the incoming message has a more recent timestamp.
/// </summary>
public class ForkliftQueueFunction
{
    private readonly ForkliftDbContext _dbContext;
    private readonly ILogger<ForkliftQueueFunction> _logger;

    public ForkliftQueueFunction(ForkliftDbContext dbContext, ILogger<ForkliftQueueFunction> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [Function(nameof(ForkliftQueueFunction))]
    public async Task Run(
        [QueueTrigger("consumethis", Connection = "StorageConnectionString")] QueueMessage message)
    {
        _logger.LogInformation(
            "Processing queue message for Id: {Id}, Timestamp: {Timestamp}",
            message.Id, message.Timestamp);

        var existing = await _dbContext.ForkliftEvents.FindAsync(message.Id);

        if (existing is null)
        {
            _dbContext.ForkliftEvents.Add(new ForkliftEvent
            {
                Id = message.Id,
                Timestamp = message.Timestamp,
                Status = message.Status,
                LastUpdated = DateTimeOffset.UtcNow
            });

            _logger.LogInformation("Inserted new ForkliftEvent with Id: {Id}", message.Id);
            await _dbContext.SaveChangesAsync();
        }
        else if (message.Timestamp > existing.Timestamp)
        {
            existing.Timestamp = message.Timestamp;
            existing.Status = message.Status;
            existing.LastUpdated = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Updated ForkliftEvent with Id: {Id} to Timestamp: {Timestamp}",
                message.Id, message.Timestamp);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation(
                "Skipped outdated message for Id: {Id}. Existing Timestamp: {Existing}, Incoming: {Incoming}",
                message.Id, existing.Timestamp, message.Timestamp);
        }
    }
}
