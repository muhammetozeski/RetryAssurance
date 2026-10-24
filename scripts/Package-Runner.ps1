param([string]$Version='1.0.1',[string]$ServiceBaseUrl='https://muhammetozeski-retry-assurance.pages.dev/',[switch]$NoRestore)
$ErrorActionPreference='Stop'
# Owner-operated release packaging. Run after the actual runner source/build acceptance, and again
# only for a new intended release or to replace an unpublished candidate. It does not execute a
# customer SDK, send a network scenario request or include user settings/logs/credentials.
if($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$'){throw 'Use a semantic release version.'}
$serviceBase=[Uri]$ServiceBaseUrl
if($serviceBase.Scheme -ne 'https' -or $serviceBase.AbsolutePath -notin @('/','/retry-assurance/') -or $serviceBase.UserInfo -or $serviceBase.Query -or $serviceBase.Fragment){throw 'Use the full HTTPS application base URL with a supported path and trailing slash.'}
$root=Split-Path -Parent $PSScriptRoot
$distribution=Join-Path $root 'publish/Distribution'
$staging=Join-Path $distribution ('Staging-'+[Guid]::NewGuid().ToString('N'))
$payload=Join-Path $staging 'RetryAssurance.Examples.NET10'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
foreach($example in @('BrokenClient','CorrectClient')){
    $publishArguments=@('publish',(Join-Path $root "examples/$example/$example.csproj"),'-c','Release','-o',(Join-Path $payload $example),"-p:Version=$Version")
    if($NoRestore){$publishArguments+='--no-restore'}
    & dotnet @publishArguments
    if($LASTEXITCODE -ne 0){throw "The $example release payload did not publish."}
}
foreach($project in @('RetryAssurance.Contracts','RetryAssurance.Runner')){
    & dotnet pack (Join-Path $root "src/$project/$project.csproj") -c Release -o $distribution "-p:Version=$Version" --no-restore
    if($LASTEXITCODE -ne 0){throw "The $project portable package did not build."}
}
$instructions=@'
Retry Assurance actual C# examples
Requires the .NET 10 runtime. These are portable DLL commands, not standalone Windows executables.

1. Open __ACCOUNT_URL__ and create/recover your actual account.
2. Create a project, declare allowed outcomes and pin ra-core 1.0.0.
3. Create a project runner capability and set it privately as RA_PROJECT_TOKEN.
   PowerShell 7: $env:RA_PROJECT_TOKEN = Read-Host -MaskInput 'Project runner capability'
4. From this directory:
   dotnet BrokenClient/BrokenClient.dll --server __SERVICE_BASE__ --commit YOUR_ACTUAL_BUILD
   dotnet CorrectClient/CorrectClient.dll --server __SERVICE_BASE__ --commit YOUR_ACTUAL_BUILD

Broken example: one callback, fresh key inside retry, two durable remote jobs, server fail, exit 1.
Correct example: one callback, stable key, one durable remote job, server pass, exit 0.
An incomplete result or action failure exits 2. These are first-party synthetic examples, not customer activity.

Use --baseline ACTUAL_RUN_ID --revision N for a separate controlled replay. Use --json PRIVATE_FILE
for allowlisted evidence. Never put recovery credentials/capabilities/production data into arguments,
public logs or a repository. The supplied transport is not an arbitrary-code sandbox and does not
sanitize arbitrary payload values. Use only synthetic input and dummy SDK credentials.

Your original failed record remains separate from a new run. Inspect it through the actual account
portal. Raw bodies/headers/capabilities are absent from exported evidence. No payment is required
for the bounded free path. Current source, API/privacy boundaries and operator setup:
https://github.com/muhammetozeski/RetryAssurance
'@
$instructions.Replace('__ACCOUNT_URL__',([Uri]::new($serviceBase,'account')).AbsoluteUri).Replace('__SERVICE_BASE__',$serviceBase.AbsoluteUri) | Set-Content -LiteralPath (Join-Path $payload 'README.txt') -Encoding utf8
$archive=Join-Path $distribution "RetryAssurance.Examples.NET10-v$Version.zip"
Compress-Archive -LiteralPath $payload -DestinationPath $archive -Force
Get-ChildItem -LiteralPath $distribution -File | Where-Object {$_.Extension -in @('.zip','.nupkg')} | ForEach-Object { $digest=Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256; Write-Output ($_.Name+' SHA256 '+$digest.Hash) }
