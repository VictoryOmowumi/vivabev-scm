using Microsoft.AspNetCore.Identity;

namespace VivaBev.Identity.Domain.Users;

public sealed class AppRole : IdentityRole<Guid>
{
    private AppRole() { }

    public AppRole(string name, string description) : base(name)
    {
        Id = Guid.CreateVersion7();
        Description = description;
    }

    public string Description { get; private set; } = null!;
}
