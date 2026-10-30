using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Domain.Organisation;

namespace VivaBev.Identity.Infrastructure;

internal static class OrganisationSeeder
{
    public static async Task SeedAsync(IdentityModuleDbContext db, CancellationToken ct = default)
    {
        if (await db.BusinessUnits.AnyAsync(ct))
            return;

        // ---- business units ----
        var manufacturing = new BusinessUnit("MFG", "Manufacturing");
        var commercial = new BusinessUnit("COM", "Commercial");
        var corporate = new BusinessUnit("CORP", "Corporate Services");
        db.BusinessUnits.AddRange(manufacturing, commercial, corporate);

        // ---- departments and cost centres ----
        (string Code, string Name, BusinessUnit Unit, (string Code, string Name)[] CostCentres)[] departments =
        [
            ("MFG", "Manufacturing", manufacturing, [("CC-MFG-PRD", "Production"), ("CC-MFG-MNT", "Maintenance")]),
            ("QA", "Quality", manufacturing, [("CC-QA-LAB", "Quality laboratory")]),
            ("SCM", "Supply Chain", manufacturing,
                [("CC-SCM-PRC", "Procurement"), ("CC-SCM-WHS", "Warehousing"), ("CC-SCM-LOG", "Logistics")]),
            ("SAL", "Sales", commercial, [("CC-SAL-SW", "Sales South-West"), ("CC-SAL-NTH", "Sales North")]),
            ("MKT", "Marketing", commercial, [("CC-MKT-BRD", "Brand marketing"), ("CC-MKT-TRD", "Trade marketing")]),
            ("IT", "Information Technology", corporate, [("CC-IT-OPS", "IT operations"), ("CC-IT-INF", "IT infrastructure")]),
            ("HR", "Human Resources", corporate, [("CC-HR-ADM", "HR administration"), ("CC-HR-LND", "Learning and development")]),
            ("FIN", "Finance", corporate, [("CC-FIN-ACC", "Accounting"), ("CC-FIN-TRY", "Treasury")]),
        ];

        foreach (var (code, name, unit, costCentres) in departments)
        {
            var department = new Department(code, name, unit.Id);
            foreach (var (ccCode, ccName) in costCentres)
                department.AddCostCentre(ccCode, ccName);
            db.Departments.Add(department);
        }

        // ---- sites: 10 plants, each with a finished-goods warehouse ----
        (string City, string State)[] plantCities =
        [
            ("Ikeja", "Lagos"), ("Agbara", "Ogun"), ("Ibadan", "Oyo"), ("Ilorin", "Kwara"), ("Abuja", "FCT"),
            ("Kano", "Kano"), ("Kaduna", "Kaduna"), ("Onitsha", "Anambra"), ("Port Harcourt", "Rivers"),
            ("Benin City", "Edo"),
        ];

        for (var i = 0; i < plantCities.Length; i++)
        {
            var (city, state) = plantCities[i];
            db.Sites.Add(new Site($"PLT-{i + 1:00}", $"{city} Plant", SiteType.Plant, manufacturing.Id, city, state));
            db.Sites.Add(new Site($"WH-{i + 1:00}", $"{city} Finished Goods Warehouse", SiteType.Warehouse,
                manufacturing.Id, city, state));
        }

        // ---- 10 regional distribution warehouses ----
        (string City, string State)[] regionalCities =
        [
            ("Apapa", "Lagos"), ("Abeokuta", "Ogun"), ("Akure", "Ondo"), ("Osogbo", "Osun"), ("Jos", "Plateau"),
            ("Maiduguri", "Borno"), ("Sokoto", "Sokoto"), ("Enugu", "Enugu"), ("Owerri", "Imo"),
            ("Calabar", "Cross River"),
        ];

        for (var i = 0; i < regionalCities.Length; i++)
        {
            var (city, state) = regionalCities[i];
            db.Sites.Add(new Site($"WH-{i + 11:00}", $"{city} Distribution Centre", SiteType.Warehouse,
                commercial.Id, city, state));
        }

        // ---- head office ----
        db.Sites.Add(new Site("HQ-LAG", "Head Office", SiteType.Office, corporate.Id, "Victoria Island", "Lagos"));

        await db.SaveChangesAsync(ct);
    }
}
