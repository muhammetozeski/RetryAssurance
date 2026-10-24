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
Console.WriteLine($"{checks} targeted runner/control/transport assertions passed. These isolated control replies do not claim service integration or real remote effects.");

sealed class RefusingObserver(HttpStatusCode observerStatus,bool closeRefused):HttpMessageHandler
{
    public readonly TaskCompletionSource Observed=new(TaskCreationOptions.RunContinuationsAsynchronously);public int CloseCalls;
    public static ExecutionGrant Grant(){var id="00000000-0000-4000-a000-000000000001";return new(id,new string('k',43),"00000000-0000-4000-a000-000000000002","https://scenario.invalid/api/simulate/"+id+"/",new("ra-core","1.0.0","Controlled fixture","commit-then-delay",3000,6,"No real effects in this failure-path fixture.",new("POST","jobs"),1,new string('a',64)),1,new string('b',64),new("Fixture","Idempotency-Key","client",["indeterminate"]),DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds());}
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken){if(request.RequestUri!.AbsolutePath=="/api/runner/runs")return Reply(HttpStatusCode.Created,Grant());if(request.Method==HttpMethod.Get){Observed.TrySetResult();return Reply(observerStatus,new ServiceFailure("Controlled optional observer refusal","fixture"));}CloseCalls++;if(closeRefused)return Reply(HttpStatusCode.NotFound,new ServiceFailure("Controlled revoked run","run_auth"));var run=new ExecutionRecord{Id=Grant().RunId,State="finalized",Result=new("incomplete",0,0,0,["No real effects in this isolated fixture."],[],new("indeterminate",null,false,false),"Failure-path unit check only.")};return Reply(HttpStatusCode.OK,new ExecutionEvidence(run,[],[]));}
    static Task<HttpResponseMessage> Reply(HttpStatusCode status,object body)=>Task.FromResult(new HttpResponseMessage(status){Content=JsonContent.Create(body)});
}
sealed class CapturingHandler:HttpMessageHandler
{
    public int Calls;public string Headers="",Key="";public string? Host;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken){Calls++;Headers=request.Headers+"\n"+request.Content!.Headers;Key=request.Headers.GetValues("Idempotency-Key").Single();Host=request.Headers.Host;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created){Content=JsonContent.Create(new{id="not-a-real-effect",synthetic=true})});}
}
