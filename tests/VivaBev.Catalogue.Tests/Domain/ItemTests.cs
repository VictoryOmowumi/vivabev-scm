using VivaBev.Catalogue.Domain;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Catalogue.Tests.Domain;

public class ItemTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Codes_are_trimmed_and_upper_cased()
    {
        var item = new Item("  pk-pet50 ", "PET bottle 50cl", ItemType.Packaging, CategoryId, "ea",
            isStocked: true, isLotControlled: false);

        Assert.Equal("PK-PET50", item.Code);
        Assert.Equal("EA", item.BaseUomCode);
    }

    [Fact]
    public void A_service_cannot_be_stocked()
    {
        var error = Assert.Throws<DomainException>(() =>
            new Item("SV-TRAIN", "Training day", ItemType.Service, CategoryId, "DAY",
                isStocked: true, isLotControlled: false));

        Assert.Contains("service", error.Message);
    }

    [Fact]
    public void Lot_control_requires_a_stocked_item()
    {
        Assert.Throws<DomainException>(() =>
            new Item("CN-GLOVES", "Gloves", ItemType.Consumable, CategoryId, "PACK",
                isStocked: false, isLotControlled: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Shelf_life_must_be_positive(int days)
    {
        Assert.Throws<DomainException>(() =>
            new Item("RM-SUGAR", "Sugar", ItemType.RawMaterial, CategoryId, "KG",
                isStocked: true, isLotControlled: true, shelfLifeDays: days));
    }

    [Fact]
    public void A_new_item_is_active() =>
        Assert.True(new Item("IT-LAPTOP", "Laptop", ItemType.Asset, CategoryId, "EA",
            isStocked: true, isLotControlled: false).IsActive);
}