using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StrivoForklift.Data;
using StrivoForklift.Models;
using Xunit;

namespace StrivoForklift.Tests;

public class ForkliftQueueFunctionTests
{
    private static ForkliftDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ForkliftDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var context = new ForkliftDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Run_NewMessage_InsertsRecord()
    {
        // Arrange
        using var db = CreateInMemoryDbContext(nameof(Run_NewMessage_InsertsRecord));
        var function = new ForkliftQueueFunction(db, NullLogger<ForkliftQueueFunction>.Instance);
        var message = new QueueMessage
        {
            Id = "forklift-1",
            Timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Status = "active",
            Location = "warehouse-A"
        };

        // Act
        await function.Run(message);

        // Assert
        var stored = await db.ForkliftEvents.FindAsync("forklift-1");
        Assert.NotNull(stored);
        Assert.Equal("forklift-1", stored.Id);
        Assert.Equal(message.Timestamp, stored.Timestamp);
        Assert.Equal("active", stored.Status);
    }

    [Fact]
    public async Task Run_NewerMessage_UpdatesExistingRecord()
    {
        // Arrange
        using var db = CreateInMemoryDbContext(nameof(Run_NewerMessage_UpdatesExistingRecord));
        var olderTimestamp = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var newerTimestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        db.ForkliftEvents.Add(new ForkliftEvent
        {
            Id = "forklift-1",
            Timestamp = olderTimestamp,
            Status = "idle",
            LastUpdated = olderTimestamp
        });
        await db.SaveChangesAsync();

        var function = new ForkliftQueueFunction(db, NullLogger<ForkliftQueueFunction>.Instance);
        var newerMessage = new QueueMessage
        {
            Id = "forklift-1",
            Timestamp = newerTimestamp,
            Status = "active",
            Location = "warehouse-B"
        };

        // Act
        await function.Run(newerMessage);

        // Assert
        var stored = await db.ForkliftEvents.FindAsync("forklift-1");
        Assert.NotNull(stored);
        Assert.Equal(newerTimestamp, stored.Timestamp);
        Assert.Equal("active", stored.Status);
    }

    [Fact]
    public async Task Run_OlderMessage_DoesNotUpdateExistingRecord()
    {
        // Arrange
        using var db = CreateInMemoryDbContext(nameof(Run_OlderMessage_DoesNotUpdateExistingRecord));
        var currentTimestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var olderTimestamp = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

        db.ForkliftEvents.Add(new ForkliftEvent
        {
            Id = "forklift-1",
            Timestamp = currentTimestamp,
            Status = "active",
            LastUpdated = currentTimestamp
        });
        await db.SaveChangesAsync();

        var function = new ForkliftQueueFunction(db, NullLogger<ForkliftQueueFunction>.Instance);
        var olderMessage = new QueueMessage
        {
            Id = "forklift-1",
            Timestamp = olderTimestamp,
            Status = "idle",
            Location = "dock-2"
        };

        // Act
        await function.Run(olderMessage);

        // Assert
        var stored = await db.ForkliftEvents.FindAsync("forklift-1");
        Assert.NotNull(stored);
        Assert.Equal(currentTimestamp, stored.Timestamp);
        Assert.Equal("active", stored.Status);
    }

    [Fact]
    public async Task Run_SameTimestampMessage_DoesNotUpdateExistingRecord()
    {
        // Arrange
        using var db = CreateInMemoryDbContext(nameof(Run_SameTimestampMessage_DoesNotUpdateExistingRecord));
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        db.ForkliftEvents.Add(new ForkliftEvent
        {
            Id = "forklift-1",
            Timestamp = timestamp,
            Status = "active",
            LastUpdated = timestamp
        });
        await db.SaveChangesAsync();

        var function = new ForkliftQueueFunction(db, NullLogger<ForkliftQueueFunction>.Instance);
        var duplicateMessage = new QueueMessage
        {
            Id = "forklift-1",
            Timestamp = timestamp,
            Status = "charging",
            Location = "dock-3"
        };

        // Act
        await function.Run(duplicateMessage);

        // Assert - same timestamp should NOT trigger an update
        var stored = await db.ForkliftEvents.FindAsync("forklift-1");
        Assert.NotNull(stored);
        Assert.Equal(timestamp, stored.Timestamp);
        Assert.Equal("active", stored.Status);
    }

    [Fact]
    public async Task Run_MultipleDistinctIds_StoresEachSeparately()
    {
        // Arrange
        using var db = CreateInMemoryDbContext(nameof(Run_MultipleDistinctIds_StoresEachSeparately));
        var function = new ForkliftQueueFunction(db, NullLogger<ForkliftQueueFunction>.Instance);
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var message1 = new QueueMessage { Id = "forklift-1", Timestamp = timestamp, Status = "active", Location = "zone-A" };
        var message2 = new QueueMessage { Id = "forklift-2", Timestamp = timestamp, Status = "idle", Location = "zone-B" };

        // Act
        await function.Run(message1);
        await function.Run(message2);

        // Assert
        Assert.Equal(2, await db.ForkliftEvents.CountAsync());
        var stored1 = await db.ForkliftEvents.FindAsync("forklift-1");
        var stored2 = await db.ForkliftEvents.FindAsync("forklift-2");
        Assert.NotNull(stored1);
        Assert.NotNull(stored2);
        Assert.Equal("active", stored1.Status);
        Assert.Equal("idle", stored2.Status);
    }
}
