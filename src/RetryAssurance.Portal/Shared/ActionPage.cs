using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace RetryAssurance.Portal.Shared;

/// <summary>Contains a whole browser action; unsuccessful work never proceeds to a fabricated success message.</summary>
public abstract class ActionPage:ComponentBase,IDisposable
{
    [Inject] protected IJSRuntime Browser {get;set;}=default!;
    [Inject] protected ILogger<ActionPage> ActionLogger {get;set;}=default!;
    protected bool Busy,Failed,Disposed;
    protected string Message="";
    /// <summary>Serializes explicit actions and presents a bounded error while logging only exception type.</summary>
    protected async Task Act(Func<Task> action){if(Busy||Disposed)return;Busy=true;Message="";Failed=false;try{await action();}catch(Exception error){ShowFailure(error);}finally{Busy=false;}}
    /// <summary>Contains a route read failure only after the page confirms the response still belongs to its current identity.</summary>
    protected void ShowFailure(Exception error){ActionLogger.LogWarning("Portal action failed; type={ErrorType}",error.GetType().Name);Failed=true;Message=error switch{Services.PortalFailure service=>service.Message,ArgumentException invalid=>invalid.Message,JSException=>"Copy or download failed. You can select the visible text instead.",_=>"This action did not finish. Refresh the project before trying again."};}
    /// <summary>Records a result only after the operation actually acknowledged it.</summary>
    protected void Done(string message){Message=message;Failed=false;}
    /// <summary>Uses the browser's clipboard without putting a secret in a URL or diagnostic log.</summary>
    protected async Task Copy(string value){var copied=await Browser.InvokeAsync<bool>("RetryAssurancePortal.copy",value);Done(copied?"Copied.":"Select the field and copy it manually.");}
    /// <summary>Downloads explicitly selected text locally; it is not posted to a third party.</summary>
    protected async Task Download(string filename,string value,string mime)=>await Browser.InvokeVoidAsync("RetryAssurancePortal.download",filename,value,mime);
    /// <summary>Shows journal timestamps without claiming they measure an exact commit-completion instant.</summary>
    protected static string Time(long value)=>DateTimeOffset.FromUnixTimeMilliseconds(value).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
    protected static string ClientOutcome(string value)=>value switch{"success"=>"Success","indeterminate"=>"Unresolved timeout","failure"=>"Failure",_=>value};
    /// <summary>Stops post-navigation action starts; each page also checks route identity before installing late reads.</summary>
    public void Dispose()=>Disposed=true;
}
