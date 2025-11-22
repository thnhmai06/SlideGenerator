namespace presentation.Models.Exceptions.Services;

public class NotEnoughArgumentException(params string[] args)
    : ArgumentException($"Not enough arguments provided. Need provide these argument(s): {string.Join(", ", args)}");