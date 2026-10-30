namespace VivaBev.SharedKernel.Abstractions;

public interface IDocumentNumberGenerator
{
    /// <summary>Next number for a document type in the current fiscal year, e.g. REQ-2027-00001.</summary>
    Task<string> NextAsync(string prefix, CancellationToken ct = default);
}
