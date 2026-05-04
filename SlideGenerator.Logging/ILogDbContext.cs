using Microsoft.EntityFrameworkCore;
using SlideGenerator.Logging.Models;

namespace SlideGenerator.Logging;

/// <summary>
///     Defines the abstraction for the database context used to persist log entries.
///     This interface allows the logging sink to interact with different database providers
///     while maintaining a consistent data model.
/// </summary>
public interface ILogDbContext
{
    /// <summary>
    ///     Gets the collection of log entries stored in the database.
    /// </summary>
    DbSet<LogEntry> LogEntries { get; }

    /// <summary>
    ///     Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
