using System.Collections.Concurrent;
using System.Net.Http.Headers;
using RetryAssurance.Contracts;
namespace RetryAssurance.Runner;

/// <summary>A supplied-transport boundary, not an arbitrary-code sandbox; every accepted SDK send stays in one issued namespace.</summary>
public sealed class ScenarioTransport : DelegatingHandler
{
    const bool IsLogEnabled=true;
    readonly Uri scenarioBase;
    readonly string runKey,logicalId;
    readonly Stopwatch clock;
    readonly CancellationToken callerToken;
    readonly ConcurrentQueue<TransportObservation> observations=[];
    /// <summary>Creates a destination guard using a real HTTP handler with automatic redirects disabled.</summary>
    public ScenarioTransport(ExecutionGrant grant,Stopwatch clock,CancellationToken callerToken):base(new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false}){scenarioBase=new(grant.ScenarioBaseUrl);runKey=grant.RunKey;logicalId=grant.LogicalOperationId;this.clock=clock;this.callerToken=callerToken;}
    /// <summary>Gets a detached allowlisted view without HTTP headers, body content or capability values.</summary>
    public IReadOnlyList<TransportObservation> Observations=>observations.ToArray();
    /// <summary>Checks the exact destination before inner HTTP execution and tags each actual send with a distinct attempt ID.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var target=request.RequestUri??throw new InvalidOperationException("The SDK request has no destination.");
        if(!target.IsAbsoluteUri||target.Scheme!=Uri.UriSchemeHttps||target.Scheme!=scenarioBase.Scheme||!string.Equals(target.IdnHost,scenarioBase.IdnHost,StringComparison.OrdinalIgnoreCase)||target.Port!=scenarioBase.Port||target.AbsolutePath!=new Uri(scenarioBase,"jobs").AbsolutePath||target.Query.Length!=0||target.Fragment.Length!=0||target.UserInfo.Length!=0||request.Method!=HttpMethod.Post)throw new InvalidOperationException("The supplied SDK transport refused a request outside the issued synthetic POST/jobs destination. No production request was sent by this transport.");
        var operationKey=request.Headers.TryGetValues("Idempotency-Key",out var keys)?keys.SingleOrDefault():null;
        request.Headers.Clear();request.Headers.Host=null;
        if(operationKey is not null)request.Headers.TryAddWithoutValidation("Idempotency-Key",operationKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if(request.Content is not null){request.Content.Headers.Clear();request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json"){CharSet="utf-8"};}
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer","ra_dummy_synthetic_credentials");request.Headers.Add("X-RA-Run-Key",runKey);request.Headers.Add("X-RA-Logical-Operation",logicalId);request.Headers.Add("X-RA-Synthetic","true");var attempt=Guid.NewGuid().ToString();request.Headers.Add("X-RA-Attempt",attempt);
        observations.Enqueue(new(attempt,"send-started",clock.Elapsed.TotalMilliseconds));Log($"Scenario send started; attempt={attempt}; method=POST; synthetic=true",isRun:IsLogEnabled);
        try{var response=await base.SendAsync(request,cancellationToken);observations.Enqueue(new(attempt,"headers-received",clock.Elapsed.TotalMilliseconds));Log($"Scenario headers received; attempt={attempt}; status={(int)response.StatusCode}",isRun:IsLogEnabled);return response;}
        catch(OperationCanceledException error){observations.Enqueue(new(attempt,"send-canceled",clock.Elapsed.TotalMilliseconds,error.GetType().Name,callerToken.IsCancellationRequested));Log($"Scenario send canceled; attempt={attempt}; callerCanceled={callerToken.IsCancellationRequested}; exception={error.GetType().Name}",isRun:IsLogEnabled);throw;}
        catch(HttpRequestException error){observations.Enqueue(new(attempt,"transport-failed",clock.Elapsed.TotalMilliseconds,error.GetType().Name));Log($"Scenario transport failed; attempt={attempt}; exception={error.GetType().Name}",isRun:IsLogEnabled);throw;}
    }
}
