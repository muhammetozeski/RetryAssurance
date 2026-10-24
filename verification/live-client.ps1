param([Parameter(Mandatory)][string]$EvidenceDirectory,[string]$Origin='https://muhammetozeski-retry-assurance.pages.dev')
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$projectRoot=Split-Path -Parent $PSScriptRoot
$previousCapability=$env:RA_PROJECT_TOKEN
$accountCreated=$false
$headers=@{Origin=$Origin}
function Post-AccountJson([string]$Path,[object]$Value) { Invoke-RestMethod -Method Post -Uri "$Origin/api/$Path" -Headers $headers -WebSession $ownerSession -ContentType 'application/json' -Body ($Value | ConvertTo-Json -Depth 8 -Compress) }
function Assert-Observed([bool]$Condition,[string]$Label){if(!$Condition){throw $Label};Write-Output "PASS $Label"}
try {
    New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
    $account=Invoke-RestMethod -Method Post -Uri "$Origin/api/accounts" -Headers $headers -SessionVariable ownerSession -ContentType 'application/json' -Body (@{alias='Synthetic .NET acceptance '+[Guid]::NewGuid().ToString('N').Substring(0,8)} | ConvertTo-Json -Compress)
    $accountCreated=$true
    $project=Post-AccountJson 'projects' @{name='Synthetic actual .NET operation verification';packVersion='1.0.0';contract=@{name='One logical synthetic create';keyHeader='Idempotency-Key';keyOwner='client';allowedTerminals=@('success','indeterminate')}}
    $capability=Post-AccountJson "projects/$($project.projectId)/capabilities" @{label='Temporary real C# acceptance runner'}
    $env:RA_PROJECT_TOKEN=$capability.token
    $commit=(& git -C $projectRoot rev-parse HEAD).Trim()
    foreach($example in @('BrokenClient','CorrectClient')){
        $target=Join-Path $EvidenceDirectory $example
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        Get-ChildItem -LiteralPath (Join-Path $projectRoot "examples/$example/bin/Release/net10.0") -File | Copy-Item -Destination $target
    }
    $brokenFile=Join-Path $EvidenceDirectory 'broken.json'
    & dotnet (Join-Path $EvidenceDirectory 'BrokenClient/BrokenClient.dll') --server $Origin --commit $commit --json $brokenFile | Tee-Object -FilePath (Join-Path $EvidenceDirectory 'broken-output.txt')
    $brokenExit=$LASTEXITCODE
    $broken=Get-Content -LiteralPath $brokenFile -Raw | ConvertFrom-Json
    Assert-Observed ($brokenExit -eq 1 -and $broken.evidence.run.result.verdict -eq 'fail' -and $broken.evidence.resources.Count -eq 2) 'real broken C# operation produced two server resources and FAIL'
    Assert-Observed (@($broken.transport | Where-Object {$_.state -eq 'send-canceled' -and !$_.callerCanceled}).Count -ge 1) 'broken client used actual non-caller HttpClient cancellation'
    $correctFile=Join-Path $EvidenceDirectory 'correct.json'
    & dotnet (Join-Path $EvidenceDirectory 'CorrectClient/CorrectClient.dll') --server $Origin --commit $commit --baseline $broken.evidence.run.id --revision 1 --json $correctFile | Tee-Object -FilePath (Join-Path $EvidenceDirectory 'correct-output.txt')
    $correctExit=$LASTEXITCODE
    $correct=Get-Content -LiteralPath $correctFile -Raw | ConvertFrom-Json
    Assert-Observed ($correctExit -eq 0 -and $correct.evidence.run.result.verdict -eq 'pass' -and $correct.evidence.resources.Count -eq 1) 'real correct C# operation produced one server resource and PASS'
    Assert-Observed ($correct.evidence.run.result.runnerReported.returnedResourceObserved -eq $true) 'successful typed client result references a server-created resource'
    Assert-Observed (@($correct.transport | Where-Object {$_.state -eq 'send-canceled' -and !$_.callerCanceled}).Count -ge 1) 'correct client used actual non-caller HttpClient cancellation'
    $comparison=Invoke-RestMethod -Uri "$Origin/api/runs/$($correct.evidence.run.id)/compare/$($broken.evidence.run.id)" -Headers $headers -WebSession $ownerSession
    Assert-Observed ($comparison.comparable -and $comparison.current.run.result.verdict -eq 'pass' -and $comparison.baseline.run.result.verdict -eq 'fail') 'real owner API compares the two pinned actual C# executions'
    $leaked=$false
    foreach($file in Get-ChildItem -LiteralPath $EvidenceDirectory -Recurse -File | Where-Object {$_.Extension -in @('.json','.txt','.log')}){if((Get-Content -LiteralPath $file.FullName -Raw).Contains($capability.token)){$leaked=$true}}
    Assert-Observed (!$leaked) 'project capability absent from exported evidence and local metadata logs'
    Write-Output ('Live C# acceptance completed; sanitized evidence directory: '+$EvidenceDirectory)
} finally {
    if($null -eq $previousCapability){Remove-Item Env:RA_PROJECT_TOKEN -ErrorAction SilentlyContinue}else{$env:RA_PROJECT_TOKEN=$previousCapability}
    if($accountCreated){$deleted=Post-AccountJson 'account/delete' @{confirmation='DELETE MY ACCOUNT'};if(!$deleted.ok){throw 'The synthetic verification account was not deleted.'};Write-Output 'Synthetic verification account and project/run records deleted through the actual API.'}
}
