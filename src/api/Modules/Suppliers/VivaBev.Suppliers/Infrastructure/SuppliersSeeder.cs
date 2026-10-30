using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Suppliers.Domain;

namespace VivaBev.Suppliers.Infrastructure;

internal static class SuppliersSeeder
{
    public static async Task SeedAsync(SuppliersDbContext db, ICatalogueQueries catalogue, TimeProvider clock,
        CancellationToken ct = default)
    {
        if (await db.Suppliers.AnyAsync(ct))
            return;

        var now = clock.GetUtcNow().UtcDateTime;

        async Task<Guid> CategoryId(string code) =>
            (await catalogue.GetCategoryByCodeAsync(code, ct))?.Id
            ?? throw new InvalidOperationException($"Category {code} is missing. Seed Catalogue first.");

        Supplier Create(string code, string name, string taxId, string contact, string email, string city,
            string state, int terms = 30)
        {
            var supplier = new Supplier(code, name, taxId, paymentTermsDays: terms);
            supplier.AddContact(contact, email, role: "Account manager");
            supplier.AddAddress(AddressType.Billing, "1 Industrial Way", city, state);
            return supplier;
        }

        var pet = Create("SUP-0001", "Lagos PET Packaging Ltd", "TIN-10000001",
            "Adaeze Okafor", "adaeze@lagospet.example", "Ikeja", "Lagos", terms: 45);
        pet.RequestCategory(await CategoryId("PKG-BTL"));
        pet.SubmitForApproval();
        pet.Approve(null, now);

        var closures = Create("SUP-0002", "Delta Closures Nigeria Ltd", "TIN-10000002",
            "Efe Oghenekaro", "efe@deltaclosures.example", "Warri", "Delta");
        closures.RequestCategory(await CategoryId("PKG-CLS"));
        closures.SubmitForApproval();
        closures.Approve(null, now);

        var sugar = Create("SUP-0003", "Northern Sugar Refiners Plc", "TIN-10000003",
            "Musa Ibrahim", "musa@northernsugar.example", "Kano", "Kano", terms: 60);
        sugar.RequestCategory(await CategoryId("RAW"));
        sugar.SubmitForApproval();
        sugar.Approve(null, now);

        var gases = Create("SUP-0004", "Atlantic Industrial Gases Ltd", "TIN-10000004",
            "Tunde Bakare", "tunde@atlanticgases.example", "Ota", "Ogun");
        gases.RequestCategory(await CategoryId("RAW"));
        gases.SubmitForApproval(); // left pending: something to approve in the demo

        var computers = Create("SUP-0005", "Techline Computers Ltd", "TIN-10000005",
            "Ngozi Eze", "ngozi@techline.example", "Victoria Island", "Lagos");
        computers.RequestCategory(await CategoryId("IT"));
        computers.SubmitForApproval();
        computers.Approve(null, now);
        computers.Suspend("Two late deliveries under review");

        var training = Create("SUP-0006", "Brightpath Training Academy", "TIN-10000006",
            "Kemi Adeyemi", "kemi@brightpath.example", "Abuja", "FCT");
        training.RequestCategory(await CategoryId("SRV-TRN")); // still a prospect

        var creative = Create("SUP-0007", "Studio Nine Creative Ltd", "TIN-10000007",
            "Chidi Nwosu", "chidi@studionine.example", "Lekki", "Lagos");
        creative.RequestCategory(await CategoryId("SRV-MKT"));
        creative.SubmitForApproval();
        creative.Approve(null, now);

        db.Suppliers.AddRange(pet, closures, sugar, gases, computers, training, creative);
        await db.SaveChangesAsync(ct);
    }
}
