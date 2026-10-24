using System.Net;
using System.Net.Http.Json;
using RetryAssurance.Contracts;
namespace RetryAssurance.Portal.Services;

/// <summary>Same-origin account transport with explicit failures and metadata-only logging.</summary>
public sealed class PortalApi(HttpClient http,ILogger<PortalApi> logger)
{
    const bool IsLogEnabled=true;
    /// <summary>Reads an actual service response without automatic retries or synthetic fallback data.</summary>
    public Task<T> GetAsync<T>(string path)=>SendAsync<T>(HttpMethod.Get,path,null);
    /// <summary>Submits one explicit browser action; uncertain writes are not silently repeated.</summary>
    public Task<T> PostAsync<T>(string path,object body)=>SendAsync<T>(HttpMethod.Post,path,body);
    /// <summary>Excludes URL identifiers, bodies, headers, recovery keys and capabilities from diagnostic output.</summary>
    async Task<T> SendAsync<T>(HttpMethod method,string path,object? body){using var request=new HttpRequestMessage(method,"/api/"+path);if(body is not null)request.Content=JsonContent.Create(body);if(IsLogEnabled)logger.LogInformation("Portal request started; method={Method}; bodyPresent={BodyPresent}",method.Method,body is not null);using var response=await http.SendAsync(request);if(IsLogEnabled)logger.LogInformation("Portal response received; status={Status}",(int)response.StatusCode);if(!response.IsSuccessStatusCode){var error=await response.Content.ReadFromJsonAsync<ServiceFailure>();var message=error?.Code switch{"configuration"=>"The service is temporarily unavailable. Try again later.","service_failure"=>"This action did not finish. Refresh the project before trying again.","billing_configuration"=>"Checkout is unavailable. Free checks remain available.",_=>error?.Error??"This action could not be completed."};throw new PortalFailure(response.StatusCode,message,error?.Code??"service_failure");}return await response.Content.ReadFromJsonAsync<T>()??throw new InvalidOperationException("The service response was empty.");}
}
/// <summary>A user-facing bounded refusal without request credentials or raw customer data.</summary>
public sealed class PortalFailure(HttpStatusCode status,string message,string code):Exception(message)
{
    public HttpStatusCode Status {get;}=status;
    public string Code {get;}=code;
}
