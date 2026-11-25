using VivaBev.SharedKernel.Domain;
using VivaBev.Suppliers.Domain;
using Xunit;

namespace VivaBev.Suppliers.Tests.Domain;

public class SupplierContactTests
{
    private static Supplier WithContact()
    {
        var supplier = new Supplier("SUP-9001", "Test Supplies Ltd", "TIN-9001");
        supplier.AddContact("Kemi Ade", "kemi@test.example", "0803 000 0000", "Sales");
        return supplier;
    }

    [Fact]
    public void Making_another_contact_primary_moves_the_flag()
    {
        var supplier = WithContact();
        var bola = supplier.AddContact("Bola Ojo", "bola@test.example");

        supplier.UpdateContact(bola.Id, "Bola Ojo", "BOLA@test.example", null, "Accounts", isPrimary: true);

        Assert.Single(supplier.Contacts, c => c.IsPrimary);
        Assert.True(bola.IsPrimary);
        Assert.Equal("bola@test.example", bola.Email);
    }

    [Fact]
    public void The_primary_flag_cannot_simply_be_cleared()
    {
        var supplier = WithContact();
        var kemi = supplier.Contacts.Single();
        Assert.Throws<DomainException>(() => supplier.UpdateContact(kemi.Id, "Kemi Ade", "kemi@test.example", null, null, false));
    }

    [Fact]
    public void The_last_contact_cannot_be_removed_and_removing_the_primary_promotes_another()
    {
        var supplier = WithContact();
        var kemi = supplier.Contacts.Single();
        Assert.Throws<DomainException>(() => supplier.RemoveContact(kemi.Id));

        var bola = supplier.AddContact("Bola Ojo", "bola@test.example");
        supplier.RemoveContact(kemi.Id);

        Assert.Equal(bola, Assert.Single(supplier.Contacts));
        Assert.True(bola.IsPrimary);
    }

    [Fact]
    public void Addresses_can_be_changed_and_removed()
    {
        var supplier = WithContact();
        var address = supplier.AddAddress(AddressType.Billing, "12 Allen Avenue", "Ikeja", "Lagos");

        supplier.UpdateAddress(address.Id, AddressType.Shipping, "4 Creek Road", "Unit 2", "Apapa", "Lagos", "NG");
        Assert.Equal(AddressType.Shipping, address.AddressType);
        Assert.Equal("Unit 2", address.Line2);

        supplier.RemoveAddress(address.Id);
        Assert.Empty(supplier.Addresses);
        Assert.Throws<DomainException>(() => supplier.RemoveAddress(address.Id));
    }

    [Fact]
    public void Payment_terms_stay_within_limits()
    {
        var supplier = WithContact();
        supplier.UpdateDetails("  Test Supplies  ", 45);
        Assert.Equal("Test Supplies", supplier.TradingName);
        Assert.Throws<DomainException>(() => supplier.UpdateDetails(null, 365));
    }
}
