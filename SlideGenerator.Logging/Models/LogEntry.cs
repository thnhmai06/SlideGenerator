using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlideGenerator.Logging.Models;

public class LogEntry
{
    [Key]
    public int Id { get; init; }
    
    public DateTimeOffset Timestamp { get; init; }
    
    public string Level { get; init; } = null!;
    
    public string Message { get; init; } = null!;
    
    [Column(TypeName = "jsonb")]
    public ExceptionIdentifier? Error { get; init; }
    
    public string TaskId { get; init; } = null!;
    
    public string Path { get; init; } = null!;
}
