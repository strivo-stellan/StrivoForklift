using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StrivoForklift.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("SqlConnection")
            ?? throw new InvalidOperationException(
                "A 'SqlConnection' connection string must be provided in configuration.");

        services.AddDbContext<ForkliftDbContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

// Ensure the database schema exists before the host starts processing.
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ForkliftDbContext>();
    dbContext.Database.EnsureCreated();
}

host.Run();
