namespace SlideGenerator.Infrastructure.Exceptions.Job;

public class ActiveJobsExistException()
    : InvalidOperationException("Cannot perform this operation while jobs are active. Cancel all jobs first.");