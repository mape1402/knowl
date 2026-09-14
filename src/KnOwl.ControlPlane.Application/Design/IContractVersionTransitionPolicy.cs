using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Defines allowed lifecycle transitions for deployable contract versions.
/// </summary>
public interface IContractVersionTransitionPolicy
{
    /// <summary>
    /// Determines whether a lifecycle transition is allowed.
    /// </summary>
    bool CanTransition(ContractVersionStatus from, ContractVersionStatus to);

    /// <summary>
    /// Gets the allowed target states from a lifecycle state.
    /// </summary>
    IReadOnlyCollection<ContractVersionStatus> GetAllowedTargets(ContractVersionStatus from);

    /// <summary>
    /// Throws when a lifecycle transition is not allowed.
    /// </summary>
    void EnsureCanTransition(ContractVersionStatus from, ContractVersionStatus to);
}
