namespace VivaBev.SharedKernel.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
