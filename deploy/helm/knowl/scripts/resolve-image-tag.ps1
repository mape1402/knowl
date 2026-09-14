$root = $env:SYSTEM_DEFAULTWORKINGDIRECTORY
if ([string]::IsNullOrWhiteSpace($root)) {
  throw "SYSTEM_DEFAULTWORKINGDIRECTORY is not set."
}
Write-Host "Searching metadata under: $root"

$metaFile = Get-ChildItem -Path $root -Filter build-metadata.txt -Recurse -File -ErrorAction SilentlyContinue |
  Select-Object -First 1 -ExpandProperty FullName

if (-not $metaFile) {
  Write-Host "No build-metadata.txt found. Directory snapshot:"
  Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
    Select-Object -First 200 -ExpandProperty FullName
  throw "build-metadata.txt not found under $root"
}

Write-Host "Using metadata file: $metaFile"

$meta = @{}
Get-Content $metaFile | ForEach-Object {
  if ($_ -match '^([^=]+)=(.*)$') {
    $meta[$matches[1]] = $matches[2]
  }
}

if (-not $meta.ContainsKey('imageTag') -or [string]::IsNullOrWhiteSpace($meta['imageTag'])) {
  throw "imageTag not found in build-metadata.txt"
}

Write-Host "Resolved IMAGE_TAG=$($meta['imageTag'])"
Write-Host "##vso[task.setvariable variable=IMAGE_TAG]$($meta['imageTag'])"
