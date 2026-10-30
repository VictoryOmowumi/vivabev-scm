namespace VivaBev.SharedKernel.Errors;

public sealed class NotFoundException(string message) : Exception(message);