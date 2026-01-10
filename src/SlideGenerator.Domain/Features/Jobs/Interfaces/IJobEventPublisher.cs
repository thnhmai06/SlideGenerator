using SlideGenerator.Domain.Features.Jobs.Notifications;

namespace SlideGenerator.Domain.Features.Jobs.Interfaces;

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