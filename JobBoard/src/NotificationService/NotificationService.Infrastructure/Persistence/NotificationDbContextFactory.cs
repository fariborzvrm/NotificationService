using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NotificationMigrations;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new NotificationDbContext(options);
    }
}
