using System.Text.Json.Serialization;
namespace RetryAssurance.Contracts;

/// <summary>A synthetic one-operation behavior contract pinned by a server-owned version and hash.</summary>
public sealed record BehaviorContract(string Name,string KeyHeader,string KeyOwner,List<string> AllowedTerminals,int EffectLimit=1,int OperationCount=1);
/// <summary>A fixed maintained fault pack; no customer executable response script belongs here.</summary>
public sealed record ScenarioPack(string Id,string Version,string Title,string Scenario,int DelayMs,int MaxAttempts,string Description,ScenarioOperation Operation,int EvidenceVersion,string Hash);
/// <summary>The sole sandbox method/path accepted by the runner transport.</summary>
public sealed record ScenarioOperation(string Method,string Path);
/// <summary>A short-lived execution capability returned once to the invoking runner and held in memory.</summary>
public sealed record ExecutionGrant(string RunId,string RunKey,string LogicalOperationId,string ScenarioBaseUrl,ScenarioPack Pack,int ContractRevision,string ContractHash,BehaviorContract Contract,long ExpiresAt);
/// <summary>Server-proven effects are separate from this explicitly declared caller observation.</summary>
public sealed record ClientObservation(string Terminal,string? ReturnedResourceId=null,string Observation="");
/// <summary>Allowlisted accepted HTTP attempt metadata; body and authorization values are never included.</summary>
public sealed class RequestEvidence
{
    public string Id {get;set;}="";
    [JsonPropertyName("attempt_id")] public string AttemptId {get;set;}="";
    public int Sequence {get;set;}
    public string Method {get;set;}="";
    [JsonPropertyName("path_label")] public string PathLabel {get;set;}="";
    [JsonPropertyName("key_hash")] public string KeyHash {get;set;}="";
    [JsonPropertyName("payload_hash")] public string PayloadHash {get;set;}="";
    [JsonPropertyName("body_bytes")] public int BodyBytes {get;set;}
    [JsonPropertyName("received_at")] public long ReceivedAt {get;set;}
    [JsonPropertyName("resource_id")] public string? ResourceId {get;set;}
    [JsonPropertyName("response_status")] public int ResponseStatus {get;set;}
    [JsonPropertyName("response_policy")] public string ResponsePolicy {get;set;}="";
    public string Effect {get;set;}="";
}
/// <summary>A resource actually committed by the synthetic service, isolated to its run.</summary>
public sealed record ResourceEvidence(string Id,[property:JsonPropertyName("key_hash")]string KeyHash,[property:JsonPropertyName("payload_hash")]string PayloadHash,[property:JsonPropertyName("created_by_request")]string CreatedByRequest,[property:JsonPropertyName("committed_at")]long CommittedAt);
/// <summary>A result derived by the server from accepted request relationships and durable resources.</summary>
public sealed record ExecutionResult(string Verdict,int ResourceCount,int RequestCount,int KeyCount,List<string> Issues,List<string> ServerProven,ReportedResult RunnerReported,string Coverage);
/// <summary>The server marks a client result as reported and checks its resource reference without attesting arbitrary code.</summary>
public sealed record ReportedResult(string Terminal,string? ReturnedResourceId,bool ReturnedResourceObserved,bool IndependentlyAttested);
/// <summary>The client's explicitly submitted note, distinct from observed request effects.</summary>
public sealed record RunnerObservation(string? ReturnedResourceId,string Detail,string Source);
/// <summary>A version-pinned server execution with no account or capability hashes exposed.</summary>
public sealed class ExecutionRecord
{
    public string Id {get;set;}="";public string ProjectId {get;set;}="";public string LogicalOperationId {get;set;}="";
    public int ContractRevision {get;set;}public string ContractHash {get;set;}="";public BehaviorContract Contract {get;set;}=new("","","",[]);
    public string PackVersion {get;set;}="";public string PackHash {get;set;}="";public string Scenario {get;set;}="";
    public string Client {get;set;}="";public string PackageVersion {get;set;}="";public string CommitId {get;set;}="";public string? ReplayOf {get;set;}
    public string State {get;set;}="";public long CreatedAt {get;set;}public long ExpiresAt {get;set;}public long? FinishedAt {get;set;}
    public ExecutionResult? Result {get;set;}
    public RunnerObservation? RunnerObservation {get;set;}
}
/// <summary>The same actual evidence response is consumed by the portal and portable runner.</summary>
public sealed record ExecutionEvidence(ExecutionRecord Run,List<RequestEvidence> Requests,List<ResourceEvidence> Resources);
/// <summary>A controlled before/after comparison; different contract/pack pins are explicitly non-comparable.</summary>
public sealed record ExecutionComparison(ExecutionEvidence Current,ExecutionEvidence Baseline,bool Comparable,string Reason);
/// <summary>A bounded user-facing API failure envelope.</summary>
public sealed record ServiceFailure(string Error,string Code);
