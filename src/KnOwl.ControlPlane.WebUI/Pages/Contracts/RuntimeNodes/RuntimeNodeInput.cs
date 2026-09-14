using KnOwl.Contracts.Distribution;
using System.ComponentModel.DataAnnotations;
using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.RuntimeNodes;

public sealed class RuntimeNodeInput
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Node name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Node code")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Environment")]
    public Guid? EnvironmentId { get; set; }

    [Display(Name = "Distribution mode")]
    public DistributionMode DistributionMode { get; set; } = DistributionMode.Pull;

    [MaxLength(500)]
    [Display(Name = "Runtime base URL")]
    public string EndpointBaseUri { get; set; } = string.Empty;

    [Display(Name = "Status")]
    public RuntimeNodeStatus Status { get; set; } = RuntimeNodeStatus.Active;

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    public RuntimeNode ToEntity(RuntimeEnvironment? environment)
    {
        return new RuntimeNode
        {
            Name = Name.Trim(),
            Code = Code.Trim(),
            EnvironmentId = environment?.Id,
            EnvironmentName = environment?.Name ?? string.Empty,
            DistributionMode = DistributionMode,
            EndpointBaseUri = EndpointBaseUri?.Trim() ?? string.Empty,
            EndpointApiPath = string.Empty,
            AuthenticationMode = RuntimeAuthenticationMode.None,
            ClientId = string.Empty,
            SecretReference = string.Empty,
            ApiKeyReference = string.Empty,
            Status = Status,
            IsEnabled = IsEnabled,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            RegisteredAtUtc = DateTime.UtcNow
        };
    }

    public void ApplyTo(RuntimeNode node, RuntimeEnvironment? environment)
    {
        node.Name = Name.Trim();
        node.Code = Code.Trim();
        node.EnvironmentId = environment?.Id;
        node.EnvironmentName = environment?.Name ?? string.Empty;
        node.DistributionMode = DistributionMode;
        node.EndpointBaseUri = EndpointBaseUri?.Trim() ?? string.Empty;
        node.EndpointApiPath = string.Empty;
        node.AuthenticationMode = RuntimeAuthenticationMode.None;
        node.ClientId = string.Empty;
        node.SecretReference = string.Empty;
        node.ApiKeyReference = string.Empty;
        node.Status = Status;
        node.IsEnabled = IsEnabled;
        node.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
    }

    public static RuntimeNodeInput FromEntity(RuntimeNode node)
    {
        return new RuntimeNodeInput
        {
            Id = node.Id,
            Name = node.Name,
            Code = node.Code,
            EnvironmentId = node.EnvironmentId,
            DistributionMode = node.DistributionMode,
            EndpointBaseUri = node.EndpointBaseUri,
            Status = node.Status,
            IsEnabled = node.IsEnabled,
            Description = node.Description
        };
    }
}

