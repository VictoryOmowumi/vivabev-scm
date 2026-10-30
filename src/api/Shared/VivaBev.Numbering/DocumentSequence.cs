namespace VivaBev.Numbering;

internal sealed class DocumentSequence
{
    private DocumentSequence() { }

    public DocumentSequence(string prefix, int fiscalYear)
    {
        Prefix = prefix;
        FiscalYear = fiscalYear;
        NextValue = 1;
    }

    public string Prefix { get; private set; } = null!;
    public int FiscalYear { get; private set; }
    public int NextValue { get; private set; }
}
