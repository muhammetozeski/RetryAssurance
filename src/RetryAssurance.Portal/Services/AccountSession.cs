using RetryAssurance.Portal.Models;
namespace RetryAssurance.Portal.Services;

/// <summary>Shares only the current actual account summary; recovery keys and capabilities stay in their explicit views.</summary>
public sealed class AccountSession(PortalApi api)
{
    public UserAccount? Account {get;private set;}
    public bool Loaded {get;private set;}
    public event Action? Changed;
    /// <summary>Loads the actual cookie-backed session; read failure is not represented as a successful sign-out.</summary>
    public async Task RefreshAsync(){var value=await api.GetAsync<AccountReply>("account");Account=value.Account;Loaded=true;Changed?.Invoke();}
    /// <summary>Updates the shared account only after an actual create/recovery acknowledgement.</summary>
    public void Accept(UserAccount? account){Account=account;Loaded=true;Changed?.Invoke();}
}
