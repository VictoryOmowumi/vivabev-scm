namespace VivaBev.SharedKernel.Errors;

public sealed class ForbiddenException(string message) : Exception(message);
