using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Domain;

namespace VivaBev.Catalogue.Infrastructure;

internal static class CatalogueSeeder
{
    public static async Task SeedAsync(CatalogueDbContext db, CancellationToken ct = default)
    {
        if (await db.UnitsOfMeasure.AnyAsync(ct))
            return; // already seeded

        db.UnitsOfMeasure.AddRange(
            new UnitOfMeasure("EA", "Each", UomDimension.Count),
            new UnitOfMeasure("PACK", "Pack", UomDimension.Count),
            new UnitOfMeasure("CASE", "Case", UomDimension.Count),
            new UnitOfMeasure("KG", "Kilogram", UomDimension.Mass),
            new UnitOfMeasure("G", "Gram", UomDimension.Mass),
            new UnitOfMeasure("L", "Litre", UomDimension.Volume),
            new UnitOfMeasure("ML", "Millilitre", UomDimension.Volume),
            new UnitOfMeasure("M", "Metre", UomDimension.Length),
            new UnitOfMeasure("HR", "Hour", UomDimension.Time),
            new UnitOfMeasure("DAY", "Day", UomDimension.Time));

        db.UomConversions.AddRange(
            new UomConversion("KG", "G", 1000m),
            new UomConversion("L", "ML", 1000m));

        var raw = new Category("RAW", "Raw materials");
        var packaging = new Category("PKG", "Packaging");
        var bottles = new Category("PKG-BTL", "Bottles", packaging.Id);
        var closures = new Category("PKG-CLS", "Caps and closures", packaging.Id);
        var finished = new Category("FG", "Finished goods");
        var it = new Category("IT", "IT equipment and software");
        var training = new Category("SRV-TRN", "Training services");
        var marketing = new Category("SRV-MKT", "Marketing services");
        db.Categories.AddRange(raw, packaging, bottles, closures, finished, it, training, marketing);

        var pet50 = new Item("PK-PET50", "PET bottle 50cl", ItemType.Packaging, bottles.Id, "EA",
            isStocked: true, isLotControlled: false);

        db.Items.AddRange(
            new Item("RM-SUGAR", "Granulated sugar", ItemType.RawMaterial, raw.Id, "KG",
                isStocked: true, isLotControlled: true, shelfLifeDays: 365),
            new Item("RM-CO2", "Food-grade CO2", ItemType.RawMaterial, raw.Id, "KG",
                isStocked: true, isLotControlled: true, shelfLifeDays: 730),
            pet50,
            new Item("PK-CAP28", "28mm plastic closure", ItemType.Packaging, closures.Id, "EA",
                isStocked: true, isLotControlled: false),
            new Item("FG-ORANGE50", "VivaBev Orange 50cl", ItemType.FinishedGood, finished.Id, "EA",
                isStocked: true, isLotControlled: true, shelfLifeDays: 270),
            new Item("IT-LAPTOP", "Business laptop", ItemType.Asset, it.Id, "EA",
                isStocked: true, isLotControlled: false),
            new Item("SV-TRAIN-DAY", "Instructor-led training day", ItemType.Service, training.Id, "DAY",
                isStocked: false, isLotControlled: false),
            new Item("SV-CAMPAIGN", "Campaign creative services", ItemType.Service, marketing.Id, "EA",
                isStocked: false, isLotControlled: false));

        db.UomConversions.Add(new UomConversion("CASE", "EA", 24m, pet50.Id));

        await db.SaveChangesAsync(ct);
    }
}