using System.Net.Http.Json;
using RetryAssurance.Contracts;
namespace RetryAssurance.Examples;

/// <summary>A real first-party JSON HTTP client; the two modes differ only in ownership of its retry key.</summary>
public sealed class SyntheticJobClient(HttpClient http,bool regenerateKeyOnRetry)
{
    /// <summary>Invokes one logical create operation with at most two real HTTP attempts using exclusively synthetic input.</summary>
    public async Task<ClientObservation> CreateJobAsync(CancellationToken cancellationToken)
    {
        var operationKey=Guid.NewGuid().ToString();
        for(var attempt=0;attempt<2;attempt++){
            var key=regenerateKeyOnRetry?Guid.NewGuid().ToString():operationKey;
            using var request=new HttpRequestMessage(HttpMethod.Post,"jobs");request.Headers.Add("Idempotency-Key",key);
            request.Headers.Add("X-API-Key","DUMMY_CUSTOM_HEADER_MUST_BE_STRIPPED");
            request.Content=JsonContent.Create(new{input="ra_synthetic_document_v1",clientReference="explicitly-synthetic-example"});
            try{using var response=await http.SendAsync(request,HttpCompletionOption.ResponseContentRead,cancellationToken);response.EnsureSuccessStatusCode();var job=await response.Content.ReadFromJsonAsync<SyntheticJob>(cancellationToken);return job is null?new("failure",null,"The response body was empty."):new("success",job.Id,"Resource ID returned.");}
            catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested&&attempt==0){/* The first real HttpClient timeout triggers this client's one internal retry. */}
        }
        return new("indeterminate",null,"Both requests timed out. The final resource count is unknown to the client.");
    }
    /// <summary>The synthetic service returns this actual resource identity, not an adapter-generated success marker.</summary>
    sealed record SyntheticJob(string Id,string Status,bool Synthetic);
}
