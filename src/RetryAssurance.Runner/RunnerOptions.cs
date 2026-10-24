namespace RetryAssurance.Runner;

/// <summary>Non-secret reproducibility labels and destination; the project capability is supplied separately.</summary>
public sealed record RunnerOptions(Uri ServiceOrigin,string Client,string PackageVersion,string CommitId,int? ContractRevision=null,string? ReplayOf=null,TimeSpan? AttemptTimeout=null)
{
    /// <summary>Accepts only the original HTTPS root or the supported mounted service base before sending credentials.</summary>
    public void Validate(){if(ServiceOrigin.Scheme!=Uri.UriSchemeHttps||!string.IsNullOrEmpty(ServiceOrigin.UserInfo)||!(ServiceOrigin.AbsolutePath is "/" or "/retry-assurance/")||!string.IsNullOrEmpty(ServiceOrigin.Query)||!string.IsNullOrEmpty(ServiceOrigin.Fragment))throw new ArgumentException("Use an HTTPS service base ending in / or /retry-assurance/, with no credentials, query or fragment.");if(string.IsNullOrWhiteSpace(Client)||Client.Length>160||string.IsNullOrWhiteSpace(PackageVersion)||PackageVersion.Length>80||string.IsNullOrWhiteSpace(CommitId)||CommitId.Length>100)throw new ArgumentException("Supply bounded actual client, package version and commit/build labels.");if(AttemptTimeout is {} timeout&&(timeout<TimeSpan.FromMilliseconds(200)||timeout>TimeSpan.FromMilliseconds(2500)))throw new ArgumentException("For this three-second fault pack, use a real HTTP timeout between 200 and 2,500 milliseconds.");}
}
/// <summary>A sanitized local observation of actual transport events, measured on this runner's monotonic clock.</summary>
public sealed record TransportObservation(string AttemptId,string State,double ElapsedMilliseconds,string? ErrorType=null,bool CallerCanceled=false);
/// <summary>The executed outcome and allowlisted local transport observations; no capability is included.</summary>
public sealed record RunnerResult(RetryAssurance.Contracts.ExecutionEvidence Evidence,IReadOnlyList<TransportObservation> Transport,bool DurableEffectObservedBeforeClientCompleted);
