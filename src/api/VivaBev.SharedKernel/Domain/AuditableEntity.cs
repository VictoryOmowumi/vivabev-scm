namespace VivaBev.SharedKernel.Domain;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
}

public abstract class AggregateRoot : AuditableEntity
{
    public byte[] RowVersion { get; private set; } = [];
}
