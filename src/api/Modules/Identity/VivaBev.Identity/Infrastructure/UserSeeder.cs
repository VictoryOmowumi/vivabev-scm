using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Domain.Users;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Identity.Infrastructure;

internal static class UserSeeder
{
    /// <summary>Development only. Never use this password anywhere real.</summary>
    public const string DemoPassword = "Demo@2026!";

    public static async Task SeedAsync(IdentityModuleDbContext db, RoleManager<AppRole> roles,
        UserManager<AppUser> users, CancellationToken ct = default)
    {
        await SyncRolesAsync(roles);

        if (await users.Users.AnyAsync(ct))
            return;

        var departments = await db.Departments.AsNoTracking().ToDictionaryAsync(d => d.Code, d => d.Id, ct);
        var sites = await db.Sites.AsNoTracking().ToDictionaryAsync(s => s.Code, s => s.Id, ct);

        (string Email, string Name, string Role, string Department, string Site, string Title)[] demo =
        [
            ("admin@vivabev.example", "Tolu Adebayo", RoleDefinitions.Administrator, "IT", "HQ-LAG", "Systems administrator"),
            ("requester@vivabev.example", "Chinedu Okeke", RoleDefinitions.Requester, "IT", "HQ-LAG", "IT analyst"),
            ("manager@vivabev.example", "Funmi Adeleke", RoleDefinitions.DepartmentManager, "IT", "HQ-LAG", "Head of IT"),
            ("buyer@vivabev.example", "Yusuf Bello", RoleDefinitions.ProcurementOfficer, "SCM", "HQ-LAG", "Procurement officer"),
            ("procurement.manager@vivabev.example", "Grace Etim", RoleDefinitions.ProcurementManager, "SCM", "HQ-LAG", "Procurement manager"),
            ("warehouse@vivabev.example", "Samuel Odu", RoleDefinitions.WarehouseOfficer, "SCM", "WH-01", "Warehouse officer"),
            ("finance@vivabev.example", "Amaka Nwachukwu", RoleDefinitions.FinanceOfficer, "FIN", "HQ-LAG", "Finance officer"),
        ];

        var created = new Dictionary<string, AppUser>();
        foreach (var person in demo)
        {
            var user = new AppUser(person.Email, person.Name, departments[person.Department], sites[person.Site], person.Title);
            Check(await users.CreateAsync(user, DemoPassword));
            Check(await users.AddToRoleAsync(user, person.Role));
            created[person.Email] = user;
        }

        var requester = created["requester@vivabev.example"];
        requester.SetManager(created["manager@vivabev.example"].Id);
        Check(await users.UpdateAsync(requester));
    }

    /// <summary>Makes each role's permission claims match RoleDefinitions exactly, every startup.</summary>
    private static async Task SyncRolesAsync(RoleManager<AppRole> roles)
    {
        foreach (var (name, description, grants) in RoleDefinitions.All)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null)
            {
                role = new AppRole(name, description);
                Check(await roles.CreateAsync(role));
            }

            var current = (await roles.GetClaimsAsync(role))
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in grants.Except(current))
                Check(await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission)));

            foreach (var permission in current.Except(grants))
                Check(await roles.RemoveClaimAsync(role, new Claim(Permissions.ClaimType, permission)));
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
