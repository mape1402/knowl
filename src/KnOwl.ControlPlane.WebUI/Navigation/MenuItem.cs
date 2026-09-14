namespace KnOwl.ControlPlane.WebUI.Navigation;

public class MenuItem
{
    public string Text { get; set; } = string.Empty;
    public string? Url { get; set; }
    /// <summary>Bootstrap Icons name, e.g. "house-fill", "shield-check".</summary>
    public string? Icon { get; set; }
    public List<MenuItem> Children { get; } = new();
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Adds a child item and returns the parent for fluent chaining.
    /// </summary>
    public MenuItem Add(string text, string? url = null, string? icon = null)
    {
        Children.Add(new MenuItem { Text = text, Url = url, Icon = icon });
        return this;
    }
}
