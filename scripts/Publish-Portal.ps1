param(
    [switch]$NoRestore,
    [string]$BasePath='/',
    [string]$Destination='',
    [string]$ServiceBaseUrl='https://muhammetozeski-retry-assurance.pages.dev/'
)
$ErrorActionPreference='Stop'
if($BasePath -notin @('/','/retry-assurance/','/retry-assurance')){throw 'Use / or /retry-assurance/ as the application base path.'}
$mount=if($BasePath -eq '/'){''}else{'/retry-assurance'}
$baseHref=$mount+'/'
$serviceBase=[Uri]$ServiceBaseUrl
if($serviceBase.Scheme -ne 'https' -or $serviceBase.AbsolutePath -ne $baseHref -or $serviceBase.UserInfo -or $serviceBase.Query -or $serviceBase.Fragment){throw 'ServiceBaseUrl must be the HTTPS application base URL with its matching path and trailing slash.'}
$projectRoot=Split-Path -Parent $PSScriptRoot
$source=Join-Path $projectRoot 'src/RetryAssurance.Portal/RetryAssurance.Portal.csproj'
$output=Join-Path $projectRoot ('publish/Portal'+$(if($mount){'-Mounted'}else{''}))
$public=if($Destination){[IO.Path]::GetFullPath($Destination)}else{Join-Path $projectRoot 'public'}
New-Item -ItemType Directory -Path $public -Force | Out-Null
$arguments=@('publish',$source,'-c','Release','-o',$output,'-p:WasmEnableWebcil=true')
if($NoRestore){$arguments+='--no-restore'}
& dotnet @arguments
if($LASTEXITCODE -ne 0){throw 'The Blazor application did not publish.'}
foreach($item in Get-ChildItem -LiteralPath (Join-Path $output 'wwwroot')){Copy-Item -LiteralPath $item.FullName -Destination $public -Recurse -Force}
$indexPath=Join-Path $public 'index.html'
$index=[IO.File]::ReadAllText($indexPath).Replace('<base href="/">',('<base href="'+$baseHref+'">'))
[IO.File]::WriteAllText($indexPath,$index)
# Compression siblings contain the root-base bootstrap from dotnet publish. Refresh them after the mount edit.
$indexBytes=[Text.Encoding]::UTF8.GetBytes($index)
foreach($encoding in @('gzip','brotli')){
    $compressed=New-Object IO.MemoryStream
    $stream=if($encoding -eq 'gzip'){[IO.Compression.GZipStream]::new($compressed,[IO.Compression.CompressionLevel]::SmallestSize,$true)}else{[IO.Compression.BrotliStream]::new($compressed,[IO.Compression.CompressionLevel]::SmallestSize,$true)}
    $stream.Write($indexBytes,0,$indexBytes.Length);$stream.Dispose()
    [IO.File]::WriteAllBytes($indexPath+$(if($encoding -eq 'gzip'){'.gz'}else{'.br'}),$compressed.ToArray());$compressed.Dispose()
}
$routes=@('','projects','projects/new','projects/:id','runs/:id','compare/:current/:baseline','packs','runner','account','account/billing','privacy')
$redirects=[Collections.Generic.List[string]]::new()
if($mount){$redirects.Add($mount+' '+$baseHref+' 308')}
foreach($route in $routes){$path=$baseHref+$route;$redirects.Add($path+' '+$baseHref+'index.html 200');if($route){$redirects.Add($path+'/ '+$baseHref+'index.html 200')}}
$redirects | Set-Content -LiteralPath (Join-Path $public '_redirects') -Encoding utf8
@"
$baseHref*
  Referrer-Policy: no-referrer
  X-Content-Type-Options: nosniff
  X-Frame-Options: DENY
  Permissions-Policy: camera=(), microphone=(), geolocation=()
  Content-Security-Policy: default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'
"@ | Set-Content -LiteralPath (Join-Path $public '_headers') -Encoding utf8
@{version=1;include=@($mount+'/api/*');exclude=@()} | ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $public '_routes.json') -Encoding utf8
$packArguments=@{Version='1.0.1';ServiceBaseUrl=$serviceBase.AbsoluteUri;NoRestore=$NoRestore}
& (Join-Path $PSScriptRoot 'Package-Runner.ps1') @packArguments
if($LASTEXITCODE -ne 0){throw 'The portable examples did not package.'}
$downloads=Join-Path $public 'downloads';New-Item -ItemType Directory -Path $downloads -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'publish/Distribution/RetryAssurance.Examples.NET10-v1.0.1.zip') -Destination (Join-Path $downloads 'RetryAssurance.Examples.NET10.zip') -Force
$commit=(& git -C $projectRoot rev-parse HEAD).Trim()
@{service='Retry Assurance';commit=$commit;builtAtUtc=[DateTimeOffset]::UtcNow.ToString('O');baseHref=$baseHref;apiBase=$mount+'/api';serviceBaseUrl=$serviceBase.AbsoluteUri;examplesVersion='1.0.1';runtime='blazor-webassembly-net10';appearance=@{themeAttribute='data-ra-theme';styleMarker='data-ra-appearance=style';scriptMarker='data-ra-appearance=script';auditGlobal='RetryAssuranceAppearance.audit'}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $public 'build-info.json') -Encoding utf8
Write-Output ('Portal published for '+$serviceBase.AbsoluteUri+'; root hosting must merge the emitted routing/header metadata.')
