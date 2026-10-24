param([string]$ServiceOrigin='https://muhammetozeski-retry-assurance.pages.dev',[string]$ProjectName='muhammetozeski-retry-assurance',[string]$AccountId=$env:CLOUDFLARE_ACCOUNT_ID)
$ErrorActionPreference='Stop'
# Owner-operated setup, not an application background task. Run once before enabling real checkout,
# and rerun only after an intentional merchant, price, endpoint or secret change. It creates/reuses
# the fixed monthly plan and notification endpoint, then updates all three production secrets together.
# It never creates a customer subscription or charge. Credentials must come from private process variables.
foreach($name in @('STRIPE_SECRET_KEY','CLOUDFLARE_API_TOKEN','CLOUDFLARE_ACCOUNT_ID')){if([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))){throw "Set $name privately in the process environment before owner setup. Do not use command arguments or a tracked file."}}
$origin=[Uri]$ServiceOrigin
if($origin.Scheme -ne 'https' -or $origin.AbsolutePath -ne '/' -or $origin.UserInfo -or $origin.Query -or $origin.Fragment){throw 'Use the actual HTTPS application origin, without credentials or a path.'}
function Stripe-OwnerRequest([string]$Path,[object]$Fields=$null,[string]$Identity=''){
    $requestHeaders=@{Authorization='Bearer '+$env:STRIPE_SECRET_KEY}
    if($Identity){$requestHeaders['Idempotency-Key']=$Identity}
    try{if($null -eq $Fields){return Invoke-RestMethod -Uri ('https://api.stripe.com/v1/'+$Path) -Headers $requestHeaders}else{return Invoke-RestMethod -Method Post -Uri ('https://api.stripe.com/v1/'+$Path) -Headers $requestHeaders -ContentType 'application/x-www-form-urlencoded' -Body $Fields}}
    catch{throw 'The fixed-origin Stripe owner request failed. Inspect the merchant dashboard privately; no response or credential value was logged.'}
}
$prices=Stripe-OwnerRequest 'prices?lookup_keys%5B%5D=retry_assurance_team_monthly_v1&active=true'
$price=@($prices.data | Where-Object {$_.unit_amount -eq 9900 -and $_.currency -eq 'usd' -and $_.recurring.interval -eq 'month'}) | Select-Object -First 1
if(!$price){$product=Stripe-OwnerRequest 'products' @{name='Retry Assurance Team maintenance';'metadata[app]'='retry-assurance'} 'retry-assurance-product-v1';$price=Stripe-OwnerRequest 'prices' @{product=$product.id;unit_amount='9900';currency='usd';'recurring[interval]'='month';lookup_key='retry_assurance_team_monthly_v1'} 'retry-assurance-monthly-price-v1'}
$target=$ServiceOrigin.TrimEnd('/')+'/api/billing/webhook'
$endpoints=Stripe-OwnerRequest 'webhook_endpoints?limit=100'
$endpoint=@($endpoints.data | Where-Object {$_.url -eq $target -and $_.status -eq 'enabled'}) | Select-Object -First 1
$webhookSecret=$env:STRIPE_WEBHOOK_SECRET
$events=@{'enabled_events[0]'='checkout.session.completed';'enabled_events[1]'='customer.subscription.created';'enabled_events[2]'='customer.subscription.updated';'enabled_events[3]'='customer.subscription.deleted'}
if(!$endpoint){$fields=@{url=$target;'metadata[app]'='retry-assurance'};foreach($item in $events.GetEnumerator()){$fields[$item.Key]=$item.Value};$endpoint=Stripe-OwnerRequest 'webhook_endpoints' $fields ('retry-assurance-notifications-'+[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($target))).Substring(0,16));$webhookSecret=$endpoint.secret}
else{if(!$webhookSecret){throw 'An endpoint already exists. Supply its previously saved STRIPE_WEBHOOK_SECRET privately; existing endpoint secrets cannot be retrieved. No checkout configuration was enabled.'};$updatedEndpoint=Stripe-OwnerRequest ('webhook_endpoints/'+$endpoint.id) $events}
if(!$webhookSecret.StartsWith('whsec_')){throw 'The webhook signing secret was not available. No production checkout configuration was enabled.'}
$configuration=@{deployment_configs=@{production=@{env_vars=@{STRIPE_SECRET_KEY=@{type='secret_text';value=$env:STRIPE_SECRET_KEY};STRIPE_PRICE_ID=@{type='secret_text';value=$price.id};STRIPE_WEBHOOK_SECRET=@{type='secret_text';value=$webhookSecret}}}}} | ConvertTo-Json -Depth 8
try{$configured=Invoke-RestMethod -Method Patch -Uri "https://api.cloudflare.com/client/v4/accounts/$AccountId/pages/projects/$ProjectName" -Headers @{Authorization='Bearer '+$env:CLOUDFLARE_API_TOKEN} -ContentType 'application/json' -Body $configuration}catch{throw 'The production secret update failed. No credentials or redacted existing settings were replayed into configuration.'}
if(!$configured.success){throw 'The hosting provider did not acknowledge the production secret update.'}
Write-Output ('Owner setup acknowledged; mode: '+$(if($env:STRIPE_SECRET_KEY.StartsWith('sk_live_')){'live'}else{'test'}))
Write-Output ('Monthly USD99 price verified: '+$price.id)
Write-Output ('Notification endpoint configured: '+$target)
Write-Output 'Production-only key, price and signing secret were submitted together. No customer charge or subscription was created. Save merchant secrets privately and deploy/recheck actual billing readiness.'
