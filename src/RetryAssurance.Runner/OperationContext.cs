using RetryAssurance.Contracts;
namespace RetryAssurance.Runner;

/// <summary>Supplies the actual SDK's transport and a single logical identity created outside all SDK retry loops.</summary>
public sealed class OperationContext
{
    /// <summary>The adapter must inject this client into its real SDK so the destination guard applies to all its requests.</summary>
    public HttpClient HttpClient {get;}
    /// <summary>Identifies one runner callback invocation; a retry does not create another logical operation.</summary>
    public string LogicalOperationId {get;}
    /// <summary>The immutable operator-maintained scenario description, without a production endpoint or key.</summary>
    public ScenarioPack Pack {get;}
    internal OperationContext(HttpClient client,ExecutionGrant grant){HttpClient=client;LogicalOperationId=grant.LogicalOperationId;Pack=grant.Pack;}
}
