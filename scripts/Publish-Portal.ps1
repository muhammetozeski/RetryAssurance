param([switch]$NoRestore)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$source=Join-Path $projectRoot 'src/RetryAssurance.Portal/RetryAssurance.Portal.csproj'
$output=Join-Path $projectRoot 'publish/Portal'
$public=Join-Path $projectRoot 'public'
$arguments=@('publish',$source,'-c','Release','-o',$output,'-p:WasmEnableWebcil=true')
if($NoRestore){$arguments+='--no-restore'}
& dotnet @arguments
if($LASTEXITCODE -ne 0){throw 'The independent Blazor application did not publish.'}
foreach($item in Get-ChildItem -LiteralPath (Join-Path $output 'wwwroot')){Copy-Item -LiteralPath $item.FullName -Destination $public -Recurse -Force}
$commit=(& git -C $projectRoot rev-parse HEAD).Trim()
@{service='Retry Assurance';commit=$commit;builtAtUtc=[DateTimeOffset]::UtcNow.ToString('O');runtime='blazor-webassembly-net10';appearance=@{themeAttribute='data-ra-theme';styleMarker='data-ra-appearance=style';scriptMarker='data-ra-appearance=script';auditGlobal='RetryAssuranceAppearance.audit'}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $public 'build-info.json') -Encoding utf8
Write-Output 'Independent portal published; account/session and runner capabilities are not part of static output.'
