namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Represents validation results for a contract payload snapshot.
/// </summary>
public sealed class ContractSnapshotValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the snapshot is valid for promotion.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets validation errors that block promotion.
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Creates a valid validation result.
    /// </summary>
    public static ContractSnapshotValidationResult Valid() => new();

    /// <summary>
    /// Creates an invalid validation result with the provided errors.
    /// </summary>
    public static ContractSnapshotValidationResult Invalid(IEnumerable<string> errors)
    {
        ContractSnapshotValidationResult result = new();
        result.Errors.AddRange(errors);
        return result;
    }
}
