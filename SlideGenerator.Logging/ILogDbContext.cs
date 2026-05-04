using Microsoft.EntityFrameworkCore;
using SlideGenerator.Logging.Models;

namespace SlideGenerator.Logging;

public interface ILogDbContext
{
    DbSet<LogEntry> LogEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
