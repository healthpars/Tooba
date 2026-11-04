using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TobiFund.Infrastructure.Data;

/// <summary>Factory برای دستورات dotnet ef (Design Time)</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TobiFundDbContext>
{
    public TobiFundDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TobiFundDbContext>()
            .UseSqlite(TobiFundDbContext.BuildConnectionString(null))
            .Options;
        return new TobiFundDbContext(options);
    }
}
