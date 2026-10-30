using Microsoft.AspNetCore.Identity;
using VivaBev.SharedKernel.Domain;

namespace VivaBev.Identity.Domain.Users;

public sealed class AppUser : IdentityUser<Guid>
{
    private AppUser() { }

    public AppUser(string email, string displayName, Guid departmentId, Guid siteId, string? jobTitle = null)
    {
        Id = Guid.CreateVersion7();
        Email = Guard.Required(email, nameof(email), 200).ToLowerInvariant();
        UserName = Email;
        EmailConfirmed = true;
        DisplayName = Guard.Required(displayName, nameof(displayName), 100);
        DepartmentId = departmentId;
        SiteId = siteId;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle.Trim();
    }

    public string DisplayName { get; private set; } = null!;
    public Guid DepartmentId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public string? JobTitle { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void SetManager(Guid? managerId)
    {
        if (managerId == Id)
            throw new DomainException("A user cannot be their own manager.");
        ManagerId = managerId;
    }

    public void MoveTo(Guid departmentId, Guid siteId)
    {
        DepartmentId = departmentId;
        SiteId = siteId;
    }

    public void Deactivate() => IsActive = false;
}
