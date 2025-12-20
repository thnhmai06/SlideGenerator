namespace SlideGenerator.Infrastructure.Job.Exceptions;

public class ActiveJobsExist()
    : InvalidOperationException("Cannot perform this operation while jobs are active. Cancel all jobs first.");