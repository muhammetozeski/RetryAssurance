using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using RetryAssurance.Contracts;
using RetryAssurance.Runner;

int checks=0;
void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
foreach(var status in new[]{HttpStatusCode.ServiceUnavailable,HttpStatusCode.TooManyRequests}){
    var control=new RefusingObserver(status,false);using var runner=new RetryAssuranceClient(new(new Uri("https://scenario.invalid/"),"Controlled observer fixture","1.0.0","consumer-test"));
    var field=typeof(RetryAssuranceClient).GetField("control",BindingFlags.Instance|BindingFlags.NonPublic)!;((HttpClient)field.GetValue(runner)!).Dispose();field.SetValue(runner,new HttpClient(control){BaseAddress=new Uri("https://scenario.invalid/")});
    int callbacks=0;var result=await runner.RunAsync(new string('p',43),async(_,_)=>{callbacks++;await control.Observed.Task.WaitAsync(TimeSpan.FromSeconds(5));return new("indeterminate",null,"No effect is claimed by this isolated failure-path fixture.");});
    Check(callbacks==1&&control.CloseCalls==1,status+" optional observer refusal still closes exactly one callback");
    Check(!result.DurableEffectObservedBeforeClientCompleted&&result.Evidence.Run.Result?.Verdict=="incomplete",status+" unavailable observation is not fabricated as a passing effect");
}
{
    var control=new RefusingObserver(HttpStatusCode.NotFound,true);using var runner=new RetryAssuranceClient(new(new Uri("https://scenario.invalid/"),"Revocation fixture","1.0.0","consumer-test"));var field=typeof(RetryAssuranceClient).GetField("control",BindingFlags.Instance|BindingFlags.NonPublic)!;((HttpClient)field.GetValue(runner)!).Dispose();field.SetValue(runner,new HttpClient(control){BaseAddress=new Uri("https://scenario.invalid/")});
    bool refused=false;try{await runner.RunAsync(new string('p',43),async(_,_)=>{await control.Observed.Task;return new("indeterminate");});}catch(RunnerServiceException failure){refused=failure.Status==HttpStatusCode.NotFound;}
    Check(refused&&control.CloseCalls==1,"final close authority refusal remains a real error after optional observer refusal");
}
var grant=RefusingObserver.Grant();using(var transport=new ScenarioTransport(grant,Stopwatch.StartNew(),CancellationToken.None)){
    var original=transport.InnerHandler;var capture=new CapturingHandler();transport.InnerHandler=capture;original!.Dispose();using var http=new HttpClient(transport){BaseAddress=new Uri(grant.ScenarioBaseUrl)};
    using var request=new HttpRequestMessage(HttpMethod.Post,"jobs"){Content=JsonContent.Create(new{input="explicitly-synthetic"})};request.Headers.TryAddWithoutValidation("Authorization","Bearer FAKE_PRODUCTION_AUTH_SENTINEL");request.Headers.Add("Cookie","FAKE_COOKIE_SENTINEL");request.Headers.Add("X-API-Key","FAKE_API_KEY_SENTINEL");request.Headers.Host="production.invalid";request.Headers.Add("Idempotency-Key","synthetic-operation-key");request.Content.Headers.Add("X-Body-Credential","FAKE_CONTENT_HEADER_SENTINEL");using var response=await http.SendAsync(request);
    Check(capture.Calls==1&&!capture.Headers.Contains("FAKE_")&&capture.Headers.Contains("ra_dummy_synthetic_credentials"),"outgoing allowlist strips arbitrary credential headers and replaces authorization");
    Check(capture.Key=="synthetic-operation-key"&&capture.Host is null,"synthetic operation key survives while caller Host override is removed");
    bool blocked=false;try{using var forbidden=await http.PostAsJsonAsync("https://production.invalid/jobs",new{input="synthetic"});}catch(InvalidOperationException){blocked=true;}
    Check(blocked&&capture.Calls==1,"wrong-origin request is refused before inner HTTP transport dispatch");
}

{
    const string serviceBase="https://scenario.invalid/retry-assurance/";
    var control=new RefusingObserver(HttpStatusCode.ServiceUnavailable,false,serviceBase);
    using var runner=new RetryAssuranceClient(new(new Uri(serviceBase),"Mounted path fixture","1.0.1","mount-consumer"));
    var field=typeof(RetryAssuranceClient).GetField("control",BindingFlags.Instance|BindingFlags.NonPublic)!;((HttpClient)field.GetValue(runner)!).Dispose();field.SetValue(runner,new HttpClient(control){BaseAddress=new Uri(serviceBase)});
    var result=await runner.RunAsync(new string('p',43),async(_,_)=>{await control.Observed.Task;return new("indeterminate");});
    Check(control.CloseCalls==1&&control.Targets.Count>=3&&control.Targets.All(path=>path.StartsWith("/retry-assurance/api/",StringComparison.Ordinal)),"mounted runner control requests retain the full service prefix");
    Check(result.Evidence.Run.Result?.Verdict=="incomplete","mounted control fixture still distinguishes missing remote effects");
}
{
    const string serviceBase="https://scenario.invalid/retry-assurance/";
    var control=new RefusingObserver(HttpStatusCode.ServiceUnavailable,false,serviceBase){GrantBaseOverride="https://scenario.invalid/"};
    using var runner=new RetryAssuranceClient(new(new Uri(serviceBase),"Wrong mount fixture","1.0.1","mount-consumer"));var field=typeof(RetryAssuranceClient).GetField("control",BindingFlags.Instance|BindingFlags.NonPublic)!;((HttpClient)field.GetValue(runner)!).Dispose();field.SetValue(runner,new HttpClient(control){BaseAddress=new Uri(serviceBase)});
    bool refused=false;int callbacks=0;try{await runner.RunAsync(new string('p',43),(_,_)=>{callbacks++;return Task.FromResult(new ClientObservation("indeterminate"));});}catch(InvalidOperationException){refused=true;}
    Check(refused&&callbacks==0&&control.CloseCalls==0,"a root-path grant is refused before a mounted client callback");
}
{
    var mountedGrant=RefusingObserver.Grant("https://scenario.invalid/retry-assurance/");using var transport=new ScenarioTransport(mountedGrant,Stopwatch.StartNew(),CancellationToken.None);var original=transport.InnerHandler;var capture=new CapturingHandler();transport.InnerHandler=capture;original!.Dispose();using var http=new HttpClient(transport){BaseAddress=new Uri(mountedGrant.ScenarioBaseUrl)};
    http.DefaultRequestHeaders.Add("Idempotency-Key","mounted-synthetic-operation");using var accepted=await http.PostAsJsonAsync("jobs",new{input="synthetic mounted boundary"});
    Check(capture.Calls==1,"issued prefixed synthetic jobs reach the supplied transport");
    bool refused=false;try{using var invalid=await http.PostAsJsonAsync("https://scenario.invalid/api/simulate/"+mountedGrant.RunId+"/jobs",new{input="synthetic wrong mount"});}catch(InvalidOperationException){refused=true;}
    Check(refused&&capture.Calls==1,"same-origin jobs outside the issued mount are refused before dispatch");
}
{
    bool refused=false;try{new RunnerOptions(new Uri("https://scenario.invalid/another-app/"),"Unsupported mount","1.0.1","mount-consumer").Validate();}catch(ArgumentException){refused=true;}
    Check(refused,"unsupported service prefixes cannot receive runner credentials");
}

Console.WriteLine($"{checks} targeted runner/control/transport assertions passed. These isolated control replies do not claim service integration or real remote effects.");

sealed class RefusingObserver(HttpStatusCode observerStatus,bool closeRefused,string serviceBase="https://scenario.invalid/"):HttpMessageHandler
{
    public string? GrantBaseOverride;public List<string> Targets=[];
    public readonly TaskCompletionSource Observed=new(TaskCreationOptions.RunContinuationsAsynchronously);public int CloseCalls;
    public static ExecutionGrant Grant(string serviceBase="https://scenario.invalid/"){var id="00000000-0000-4000-a000-000000000001";return new(id,new string('k',43),"00000000-0000-4000-a000-000000000002",new Uri(new Uri(serviceBase),"api/simulate/"+id+"/").AbsoluteUri,new("ra-core","1.0.0","Controlled fixture","commit-then-delay",3000,6,"No real effects in this failure-path fixture.",new("POST","jobs"),1,new string('a',64)),1,new string('b',64),new("Fixture","Idempotency-Key","client",["indeterminate"]),DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds());}
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken){Targets.Add(request.RequestUri!.AbsolutePath);if(request.RequestUri.AbsolutePath==new Uri(new Uri(serviceBase),"api/runner/runs").AbsolutePath)return Reply(HttpStatusCode.Created,Grant(GrantBaseOverride??serviceBase));if(request.Method==HttpMethod.Get){Observed.TrySetResult();return Reply(observerStatus,new ServiceFailure("Controlled optional observer refusal","fixture"));}CloseCalls++;if(closeRefused)return Reply(HttpStatusCode.NotFound,new ServiceFailure("Controlled revoked run","run_auth"));var run=new ExecutionRecord{Id=Grant().RunId,State="finalized",Result=new("incomplete",0,0,0,["No real effects in this isolated fixture."],[],new("indeterminate",null,false,false),"Failure-path unit check only.")};return Reply(HttpStatusCode.OK,new ExecutionEvidence(run,[],[]));}
    static Task<HttpResponseMessage> Reply(HttpStatusCode status,object body)=>Task.FromResult(new HttpResponseMessage(status){Content=JsonContent.Create(body)});
}
sealed class CapturingHandler:HttpMessageHandler
{
    public int Calls;public string Headers="",Key="";public string? Host;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken){Calls++;Headers=request.Headers+"\n"+request.Content!.Headers;Key=request.Headers.GetValues("Idempotency-Key").Single();Host=request.Headers.Host;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created){Content=JsonContent.Create(new{id="not-a-real-effect",synthetic=true})});}
}
