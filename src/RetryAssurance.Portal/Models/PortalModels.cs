namespace RetryAssurance.Portal.Models;

/// <summary>A real secret-backed account alias, not a verified person or email identity.</summary>
public sealed record UserAccount(string Id,string Alias,long CreatedAt);
/// <summary>Account reads and creation share a typed response; only creation includes a one-time recovery key.</summary>
public sealed record AccountReply(UserAccount? Account,string? RecoveryKey=null,string? Warning=null);
/// <summary>An actual member-visible project summary.</summary>
public sealed record ProjectSummary(string Id,string Name,int ContractRevision,long CreatedAt,long UpdatedAt,string Role);
/// <summary>Projects returned by the current actual session.</summary>
public sealed record ProjectList(List<ProjectSummary> Projects);
/// <summary>Identity returned only after an actual project insert.</summary>
public sealed record CreatedProject(string ProjectId);
/// <summary>Current authorized project metadata.</summary>
public sealed record ProjectIdentity(string Id,string Name,string Role,int ContractRevision,long CreatedAt);
/// <summary>An immutable behavior contract and scenario content identity.</summary>
public sealed record ContractRevision(int Revision,RetryAssurance.Contracts.BehaviorContract Document,string DocumentHash,string PackVersion,string PackHash,long CreatedAt);
/// <summary>A capability list never contains bearer values or hashes.</summary>
public sealed record CapabilitySummary(string Id,string Label,long CreatedAt,long ExpiresAt,long? RevokedAt,bool CanRevoke=false);
/// <summary>The project detail combines real role, immutable contracts and revocable capability metadata.</summary>
public sealed record ProjectDetail(ProjectIdentity Project,List<ContractRevision> Contracts,List<CapabilitySummary> Capabilities);
/// <summary>A newly created capability is shown once for intentional private copying.</summary>
public sealed record CreatedCapability(string Id,string Token,long ExpiresAt,string Scope);
/// <summary>A real one-use invitation.</summary>
public sealed record CreatedInvitation(string Token,long ExpiresAt,string Role);
/// <summary>Only an actual accepted invitation returns its project identity.</summary>
public sealed record JoinedInvitation(string ProjectId,string Role);
/// <summary>An account-authorized member record, private to the project owner view.</summary>
public sealed record ProjectMember(string Id,string Alias,string Role);
/// <summary>The actual project's members.</summary>
public sealed record MemberList(List<ProjectMember> Members);
/// <summary>An actual run-list entry; no invented counters or execution result is substituted.</summary>
public sealed record RunSummary(string Id,string LogicalOperationId,string Client,string PackageVersion,string CommitId,int ContractRevision,string PackVersion,string State,string? Verdict,long CreatedAt,long? FinishedAt);
/// <summary>The most recent real project executions.</summary>
public sealed record RunList(List<RunSummary> Runs);
/// <summary>Available immutable maintained packs.</summary>
public sealed record PackList(List<RetryAssurance.Contracts.ScenarioPack> Packs);
/// <summary>A provider's real acknowledged revision update.</summary>
public sealed record UpdatedRevision(int Revision);
/// <summary>A completed server action; no local fake success is fabricated.</summary>
public sealed record Acknowledgement(bool Ok);
/// <summary>The configured subscription plan and actual current account allowance.</summary>
public sealed record BillingPlan(string Name,int AmountCents,string Currency,string Interval,int ProjectLimit,int MonthlyRunLimit);
/// <summary>A verified provider subscription record, explicitly distinguishing test from live.</summary>
public sealed record SubscriptionRecord(string Id,string Status,int IsLive,long ExpiresAt);
/// <summary>Billing configuration and entitlement come from the actual service.</summary>
public sealed record BillingState(bool Configured,string Mode,bool Premium,BillingPlan Plan,List<SubscriptionRecord> Subscriptions,string PriceStatus);
/// <summary>An actual provider-created checkout or billing-portal destination.</summary>
public sealed record BillingDestination(string Url,string? Mode=null,string? Warning=null);
