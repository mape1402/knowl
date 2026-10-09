namespace KnOwl.WolfAuth;

/// <summary>
/// Marker service indicating that a KnOwl host opted into WolfAuth authentication.
/// </summary>
public interface IKnOwlWolfAuthRegistration
{
}

internal sealed class KnOwlWolfAuthRegistration : IKnOwlWolfAuthRegistration;
