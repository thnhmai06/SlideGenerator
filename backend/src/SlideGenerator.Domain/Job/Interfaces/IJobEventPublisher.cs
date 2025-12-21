using SlideGenerator.Domain.Job.Notifications;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Publishes realtime job events to subscribers.
/// </summary>
public interface IJobEventPublisher
{
    /// <summary>
    ///     Publishes a job event.
    /// </summary>
    Task PublishAsync(JobEvent notification, CancellationToken cancellationToken);
}