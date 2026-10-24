using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RetryAssurance.Contracts;
namespace RetryAssurance.Runner;

/// <summary>Runs one real callback exactly once; only explicit idempotent control requests may recover from transport failure.</summary>
public sealed class RetryAssuranceClient : IDisposable
{
    const bool IsLogEnabled=true;
    readonly HttpClient control;
    readonly RunnerOptions options;
    /// <summary>Creates a fixed-origin control client; the secret is never accepted in a URL or written to settings.</summary>
    public RetryAssuranceClient(RunnerOptions options){options.Validate();this.options=options;control=new(new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false}){BaseAddress=options.ServiceOrigin,Timeout=TimeSpan.FromSeconds(20)};}
    /// <summary>Executes one actual client operation and obtains the server-owned durable verdict; customer code stays here.</summary>
    public async Task<RunnerResult> RunAsync(string projectCapability,Func<OperationContext,CancellationToken,Task<ClientObservation>> operation,CancellationToken cancellationToken=default)
    {
        if(projectCapability.Length!=43||projectCapability.Any(character=>!char.IsAsciiLetterOrDigit(character)&&character is not '_' and not '-'))throw new ArgumentException("Supply the project capability through RA_PROJECT_TOKEN or a private caller value.");
        var requestId=Guid.NewGuid().ToString();var grant=await ControlAsync<ExecutionGrant>("api/runner/runs",new{requestId,client=options.Client,packageVersion=options.PackageVersion,commitId=options.CommitId,replayOf=options.ReplayOf,contractRevision=options.ContractRevision},projectCapability,null,cancellationToken);
        ValidateGrant(grant);var clock=Stopwatch.StartNew();using var transport=new ScenarioTransport(grant,clock,cancellationToken);using var client=new HttpClient(transport){BaseAddress=new Uri(grant.ScenarioBaseUrl),Timeout=options.AttemptTimeout??TimeSpan.FromMilliseconds(1400)};var context=new OperationContext(client,grant);var observedAt=-1d;
        using var observerCancellation=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var observer=ObserveEffectAsync(grant,clock,value=>Interlocked.CompareExchange(ref observedAt,value,-1),observerCancellation.Token);
        ClientObservation observation;double completedAt;
        try{Log($"Client operation started; client={options.Client}; package={options.PackageVersion}; logicalOperation={grant.LogicalOperationId}; callbackInvocations=1",isRun:IsLogEnabled);observation=await operation(context,cancellationToken);}
        catch(OperationCanceledException error){observation=new("indeterminate",null,$"Actual operation ended with {error.GetType().Name}; callerCanceled={cancellationToken.IsCancellationRequested}.");}
        catch(HttpRequestException error){observation=new("indeterminate",null,$"Actual operation ended with {error.GetType().Name}; remote outcome requires the server journal.");}
        catch(Exception error){observation=new("failure",null,$"Adapter operation ended with {error.GetType().Name}. No exception content or customer payload was uploaded.");}
        finally{completedAt=clock.Elapsed.TotalMilliseconds;await observerCancellation.CancelAsync();try{await observer;}catch(OperationCanceledException){Log("Local evidence observer stopped after the client operation ended.",isRun:IsLogEnabled);}}
        if(observation.Observation.Length>1000)observation=observation with{Observation=observation.Observation[..1000]};
        using var closing=new CancellationTokenSource(TimeSpan.FromSeconds(20));var evidence=await ControlAsync<ExecutionEvidence>("api/runner/runs/"+grant.RunId+"/close",new{terminal=observation.Terminal,returnedResourceId=observation.ReturnedResourceId,observation=observation.Observation},null,grant.RunKey,closing.Token);
        clock.Stop();Log($"Server verdict received; run={grant.RunId}; verdict={evidence.Run.Result?.Verdict}; resources={evidence.Resources.Count}; requests={evidence.Requests.Count}; elapsedMs={clock.Elapsed.TotalMilliseconds:F0}",isRun:IsLogEnabled);return new(evidence,transport.Observations,observedAt>=0&&observedAt<completedAt);
    }
    /// <summary>Observes a committed journal while the real client operation is pending, using this process's monotonic order only.</summary>
    async Task ObserveEffectAsync(ExecutionGrant grant,Stopwatch clock,Action<double> record,CancellationToken cancellationToken)
    {
        for(var index=0;index<20;index++){
            cancellationToken.ThrowIfCancellationRequested();try{var evidence=await ControlAsync<ExecutionEvidence>("api/runner/runs/"+grant.RunId+"/evidence",null,null,grant.RunKey,cancellationToken);if(evidence.Resources.Count>0){record(clock.Elapsed.TotalMilliseconds);return;}}
            catch(Exception error) when(error is HttpRequestException or RunnerServiceException){Log($"The optional local journal observation was unavailable; type={error.GetType().Name}; the final server verdict remains authoritative for effects.",isRun:IsLogEnabled);return;}
            await Task.Delay(100,cancellationToken);
        }
    }
    /// <summary>Verifies a server grant's declared path before giving it to a customer adapter.</summary>
    void ValidateGrant(ExecutionGrant grant){var target=new Uri(grant.ScenarioBaseUrl);if(target.Scheme!=options.ServiceOrigin.Scheme||target.IdnHost!=options.ServiceOrigin.IdnHost||target.Port!=options.ServiceOrigin.Port||target.AbsolutePath!="/api/simulate/"+grant.RunId+"/"||target.Query.Length!=0||target.UserInfo.Length!=0||grant.RunKey.Length!=43||grant.Pack.Scenario!="commit-then-delay"||grant.Pack.Operation.Method!="POST"||grant.Pack.Operation.Path!="jobs")throw new InvalidOperationException("The service grant does not match the expected fixed-origin synthetic scenario contract.");}
    /// <summary>Retries at most once only when a stable mint/close identity makes a transport retry idempotent.</summary>
    async Task<T> ControlAsync<T>(string path,object? body,string? parentKey,string? runKey,CancellationToken cancellationToken)
    {
        for(var attempt=0; ;attempt++)try{using var request=new HttpRequestMessage(body is null?HttpMethod.Get:HttpMethod.Post,path);if(parentKey is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",parentKey);if(runKey is not null)request.Headers.Add("X-RA-Run-Key",runKey);if(body is not null)request.Content=JsonContent.Create(body);Log($"Control request started; action={(body is null?"read":"idempotent-write")}; bodyPresent={body is not null}; parentCapabilityPresent={parentKey is not null}; runCapabilityPresent={runKey is not null}",isRun:IsLogEnabled);using var response=await control.SendAsync(request,HttpCompletionOption.ResponseContentRead,cancellationToken);if(!response.IsSuccessStatusCode){var failure=await response.Content.ReadFromJsonAsync<ServiceFailure>(cancellationToken);throw new RunnerServiceException(response.StatusCode,failure?.Error??"The service rejected this action.",failure?.Code??"service_failure");}return await response.Content.ReadFromJsonAsync<T>(cancellationToken)??throw new InvalidOperationException("The service response was empty.");}
        catch(HttpRequestException) when(attempt==0&&!cancellationToken.IsCancellationRequested){Log("Recovering one control transport failure with the same idempotent request identity.",isRun:IsLogEnabled);}
    }
    /// <summary>Releases the fixed-origin control transport after the caller finishes its run batch.</summary>
    public void Dispose()=>control.Dispose();
}
/// <summary>A bounded service refusal retaining status/code without including headers or submitted secrets.</summary>
public sealed class RunnerServiceException(HttpStatusCode status,string message,string code):Exception(message)
{
    public HttpStatusCode Status {get;}=status;
    public string Code {get;}=code;
}
