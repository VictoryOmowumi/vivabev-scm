namespace VivaBev.Catalogue.Domain;

public enum ItemType
{
    RawMaterial,
    Packaging,
    FinishedGood,
    Consumable,
    Asset,
    Service
}

public enum UomDimension
{
    Count,
    Mass,
    Volume,
    Length,
    Time
}