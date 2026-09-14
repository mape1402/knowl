$jsonRaw = $env:KUBECTL_OUTPUT

if ([string]::IsNullOrWhiteSpace($jsonRaw)) {
  throw "KUBECTL_OUTPUT is empty. Map env var KUBECTL_OUTPUT = $(GetSvcJson.KubectlOutput) in the PowerShell task."
}

$jsonRaw = $jsonRaw.Trim()
if (-not ($jsonRaw.StartsWith("{") -or $jsonRaw.StartsWith("["))) {
  throw "KUBECTL_OUTPUT is not valid JSON. Starts with: '$($jsonRaw.Substring(0, [Math]::Min(80, $jsonRaw.Length)))'"
}

$svc = $jsonRaw | ConvertFrom-Json

$externalIp = $null
$externalHostname = $null
$internalIp = $null
$port = $null

if ($svc.status.loadBalancer.ingress -and $svc.status.loadBalancer.ingress.Count -gt 0) {
  $externalIp = $svc.status.loadBalancer.ingress[0].ip
  $externalHostname = $svc.status.loadBalancer.ingress[0].hostname
}

if ($svc.spec.ports -and $svc.spec.ports.Count -gt 0) {
  $port = $svc.spec.ports[0].port
}

if (-not [string]::IsNullOrWhiteSpace($svc.spec.clusterIP) -and $svc.spec.clusterIP -ne "None") {
  $internalIp = $svc.spec.clusterIP
}

if (-not $port) {
  throw "Service port not found in spec.ports[0].port."
}

if ([string]::IsNullOrWhiteSpace($externalIp) -and [string]::IsNullOrWhiteSpace($externalHostname) -and [string]::IsNullOrWhiteSpace($internalIp)) {
  throw "No external endpoint (LoadBalancer) or internal endpoint (clusterIP) found in service."
}

$externalEndpoint = $null
if (-not [string]::IsNullOrWhiteSpace($externalIp)) {
  $externalEndpoint = "$externalIp`:$port"
} elseif (-not [string]::IsNullOrWhiteSpace($externalHostname)) {
  $externalEndpoint = "$externalHostname`:$port"
}

$internalEndpoint = if (-not [string]::IsNullOrWhiteSpace($internalIp)) { "$internalIp`:$port" } else { $null }
$endpoint = if (-not [string]::IsNullOrWhiteSpace($externalEndpoint)) { $externalEndpoint } else { $internalEndpoint }

Write-Host "SERVICE_EXTERNAL_IP=$externalIp"
Write-Host "SERVICE_EXTERNAL_HOSTNAME=$externalHostname"
Write-Host "SERVICE_EXTERNAL_ENDPOINT=$externalEndpoint"
Write-Host "SERVICE_INTERNAL_IP=$internalIp"
Write-Host "SERVICE_INTERNAL_ENDPOINT=$internalEndpoint"
Write-Host "SERVICE_PORT=$port"
Write-Host "SERVICE_ENDPOINT=$endpoint"

Write-Host "##vso[task.setvariable variable=SERVICE_EXTERNAL_IP]$externalIp"
Write-Host "##vso[task.setvariable variable=SERVICE_EXTERNAL_HOSTNAME]$externalHostname"
Write-Host "##vso[task.setvariable variable=SERVICE_EXTERNAL_ENDPOINT]$externalEndpoint"
Write-Host "##vso[task.setvariable variable=SERVICE_INTERNAL_IP]$internalIp"
Write-Host "##vso[task.setvariable variable=SERVICE_INTERNAL_ENDPOINT]$internalEndpoint"
Write-Host "##vso[task.setvariable variable=SERVICE_PORT]$port"
Write-Host "##vso[task.setvariable variable=SERVICE_ENDPOINT]$endpoint"
