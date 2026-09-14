using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class ContractVersionTransitionPolicy : IContractVersionTransitionPolicy
{
    private static readonly IReadOnlyDictionary<ContractVersionStatus, ContractVersionStatus[]> Transitions =
        new Dictionary<ContractVersionStatus, ContractVersionStatus[]>
        {
            [ContractVersionStatus.Draft] = [ContractVersionStatus.InReview],
            [ContractVersionStatus.InReview] = [ContractVersionStatus.Draft, ContractVersionStatus.Approved],
            [ContractVersionStatus.Approved] = [ContractVersionStatus.InReview, ContractVersionStatus.Deployed],
            [ContractVersionStatus.Deployed] = [ContractVersionStatus.Deprecated],
            [ContractVersionStatus.Deprecated] = [ContractVersionStatus.Archived],
            [ContractVersionStatus.Archived] = []
        };

    /// <inheritdoc />
    public bool CanTransition(ContractVersionStatus from, ContractVersionStatus to)
    {
        return Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ContractVersionStatus> GetAllowedTargets(ContractVersionStatus from)
    {
        return Transitions.TryGetValue(from, out var allowed) ? allowed : [];
    }

    /// <inheritdoc />
    public void EnsureCanTransition(ContractVersionStatus from, ContractVersionStatus to)
    {
        if (CanTransition(from, to))
        {
            return;
        }

        var allowed = string.Join(", ", GetAllowedTargets(from));
        throw new InvalidOperationException($"Transition from '{from}' to '{to}' is not allowed. Allowed targets: [{allowed}].");
    }
}
