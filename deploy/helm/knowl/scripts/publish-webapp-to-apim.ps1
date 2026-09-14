param(
  [Parameter(Mandatory = $false)] [string] $ResourceGroupName = $env:APIM_RESOURCE_GROUP,
  [Parameter(Mandatory = $false)] [string] $ApimName = $env:APIM_NAME,
  [Parameter(Mandatory = $false)] [string] $ApiId = $env:APIM_API_ID,
  [Parameter(Mandatory = $false)] [string] $ApiDisplayName = $env:APIM_API_DISPLAY_NAME,
  [Parameter(Mandatory = $false)] [string] $ApiPath = $env:APIM_API_PATH,
  [Parameter(Mandatory = $false)] [string] $ApiProtocols = $env:APIM_PROTOCOLS,
  [Parameter(Mandatory = $false)] [string] $BackendProtocol = $env:APIM_BACKEND_PROTOCOL,
  [Parameter(Mandatory = $false)] [string] $BackendEndpoint = ""
)

$ErrorActionPreference = "Stop"

function Invoke-AzCli {
  param(
    [Parameter(Mandatory = $true)]
    [string[]] $Arguments
  )

  & az @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "Azure CLI failed (exit code $LASTEXITCODE): az $($Arguments -join ' ')"
  }
}

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) {
  throw "APIM resource group is required. Set APIM_RESOURCE_GROUP."
}
if ([string]::IsNullOrWhiteSpace($ApimName)) {
  throw "APIM service name is required. Set APIM_NAME."
}

if ([string]::IsNullOrWhiteSpace($ApiId)) {
  $ApiId = "knowl-web"
}
if ([string]::IsNullOrWhiteSpace($ApiDisplayName)) {
  $ApiDisplayName = "Dmx KnOwl Web"
}
if ([string]::IsNullOrWhiteSpace($ApiPath)) {
  $ApiPath = "knowl"
}
if ([string]::IsNullOrWhiteSpace($ApiProtocols)) {
  $ApiProtocols = "https"
}
if ([string]::IsNullOrWhiteSpace($BackendProtocol)) {
  $BackendProtocol = "http"
}

if ([string]::IsNullOrWhiteSpace($BackendEndpoint)) {
  $BackendEndpoint = $env:SERVICE_EXTERNAL_ENDPOINT
}
if ([string]::IsNullOrWhiteSpace($BackendEndpoint)) {
  throw "No backend endpoint found. Expected SERVICE_EXTERNAL_ENDPOINT."
}

$backendUrl = "{0}://{1}" -f $BackendProtocol, $BackendEndpoint
Write-Host "Using backend URL: $backendUrl"
Write-Host "APIM target: $ResourceGroupName / $ApimName"
Write-Host "API: id=$ApiId, path=$ApiPath, displayName=$ApiDisplayName"

$protocolList = $ApiProtocols.Split(",") |
  ForEach-Object { $_.Trim().ToLowerInvariant() } |
  Where-Object { $_ -ne "" }

if ($protocolList.Count -eq 0) {
  throw "APIM_PROTOCOLS resolved empty value."
}

$apiCreateArgs = @(
  "apim", "api", "create",
  "--resource-group", $ResourceGroupName,
  "--service-name", $ApimName,
  "--api-id", $ApiId,
  "--display-name", $ApiDisplayName,
  "--path", $ApiPath,
  "--service-url", $backendUrl,
  "--subscription-required", "false",
  "--api-type", "http",
  "--only-show-errors",
  "--protocols"
) + $protocolList

Invoke-AzCli -Arguments $apiCreateArgs
Write-Host "API upserted."

& az apim api operation delete `
  --resource-group $ResourceGroupName `
  --service-name $ApimName `
  --api-id $ApiId `
  --operation-id "wildcard-all" `
  --only-show-errors | Out-Null

Invoke-AzCli -Arguments @(
  "apim", "api", "operation", "create",
  "--resource-group", $ResourceGroupName,
  "--service-name", $ApimName,
  "--api-id", $ApiId,
  "--operation-id", "wildcard-all",
  "--display-name", "Wildcard all routes",
  "--method", "*",
  "--url-template", "/*",
  "--only-show-errors"
)

Write-Host "Wildcard operation upserted."

$policyXml = @"
<policies>
  <inbound>
    <base />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
"@

$subscriptionId = (& az account show --query id -o tsv).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($subscriptionId)) {
  throw "Cannot resolve current Azure subscription id."
}

$policyBody = @{
  properties = @{
    format = "rawxml"
    value = $policyXml
  }
} | ConvertTo-Json -Depth 5 -Compress

$policyUrl = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.ApiManagement/service/$ApimName/apis/$ApiId/policies/policy?api-version=2022-08-01"

$accessToken = (& az account get-access-token --resource "https://management.azure.com/" --query accessToken -o tsv).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($accessToken)) {
  throw "Cannot acquire Azure access token for management.azure.com."
}

$headers = @{
  Authorization = "Bearer $accessToken"
  "Content-Type" = "application/json; charset=utf-8"
}

try {
  Invoke-RestMethod -Method Put -Uri $policyUrl -Headers $headers -Body $policyBody
}
catch {
  $responseText = $null
  if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $responseText = $reader.ReadToEnd()
  }
  if (-not [string]::IsNullOrWhiteSpace($responseText)) {
    throw "APIM policy PUT failed: $responseText"
  }
  throw
}

Write-Host "Policy applied."
Write-Host "APIM publish finished."
