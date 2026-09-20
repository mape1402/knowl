namespace KnOwl.Contracts.Artifacts;

/// <summary>
/// Represents artifact types supported by KnOwl distribution.
/// </summary>
public enum ContractArtifactType
{
    /// <summary>
    /// Event contract artifact.
    /// </summary>
    Event = 0,

    /// <summary>
    /// Command request contract artifact.
    /// </summary>
    CommandRequest = 1,

    /// <summary>
    /// Command reply contract artifact.
    /// </summary>
    CommandReply = 2,

    /// <summary>
    /// Command contract artifact containing request and optional reply schemas.
    /// </summary>
    Command = 3
}
