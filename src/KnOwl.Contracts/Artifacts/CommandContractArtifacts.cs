namespace KnOwl.Contracts.Artifacts;

/// <summary>
/// Represents the deployed artifacts that describe a command request and optional reply.
/// </summary>
public sealed record CommandContractArtifacts<TArtifact>(
    string CommandKey,
    string Version,
    TArtifact RequestArtifact,
    TArtifact? ReplyArtifact);
