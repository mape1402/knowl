namespace KnOwl.ControlPlane.WebUI.Navigation;

public interface IMenuService
{
    /// <summary>
    /// Top-level menu items. Can be modified per-request (e.g. from a PageModel)
    /// to apply filtering or role-based visibility.
    /// </summary>
    IList<MenuItem> Items { get; }
}
