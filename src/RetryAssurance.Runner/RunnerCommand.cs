using System.Globalization;
using System.Text.Json;
using RetryAssurance.Contracts;
using RetryAssurance.Runner.Infrastructure;
namespace RetryAssurance.Runner;

/// <summary>A portable command boundary used by real adapter examples; secrets come only from the process environment.</summary>
public static class RunnerCommand
{
    const bool IsLogEnabled=true;
    /// <summary>Parses non-secret run labels, executes one actual adapter operation, and returns CI-friendly verdict codes.</summary>
    public static async Task<int> ExecuteAsync(string[] args,string clientName,string packageVersion,Func<OperationContext,CancellationToken,Task<ClientObservation>> operation)
    {
        if(args.Contains("--help")){Console.WriteLine("Usage: dotnet <adapter>.dll --server https://your-service-origin --commit actual-commit [--version actual-package-version] [--revision N] [--baseline previous-run-id] [--json output-file]\nSet RA_PROJECT_TOKEN privately in the environment. Do not put capabilities or production credentials into command arguments.\nExit codes: 0 server pass, 1 server fail, 2 incomplete/action failure. Only explicitly synthetic requests are supported.");return 0;}
        using var canceled=new CancellationTokenSource();ConsoleCancelEventHandler cancellation=(_,eventArgs)=>{eventArgs.Cancel=true;canceled.Cancel();};Console.CancelKeyPress+=cancellation;
        try{
            var result=await RunSafelyAsync(async token=>{
                SettingsManager.LoadSettings(AppConstants.ConfigFileName,false);Logger.ActivateLogging=Settings.EnableLogging.Value;
                Dictionary<string,string> values=[];for(var index=0;index<args.Length;index+=2){if(index+1>=args.Length||!new[]{"--server","--commit","--version","--revision","--baseline","--json"}.Contains(args[index])||!values.TryAdd(args[index],args[index+1]))throw new ArgumentException("Use the documented argument pairs; secrets are not command-line arguments.");}
                var server=values.GetValueOrDefault("--server")??throw new ArgumentException("Supply --server with the actual HTTPS service origin.");var commit=values.GetValueOrDefault("--commit")??throw new ArgumentException("Supply --commit with the actual build or source identifier.");int? revision=values.TryGetValue("--revision",out var number)?int.Parse(number,CultureInfo.InvariantCulture):null;var options=new RunnerOptions(new Uri(server),clientName,values.GetValueOrDefault("--version")??packageVersion,commit,revision,values.GetValueOrDefault("--baseline"));var capability=Environment.GetEnvironmentVariable("RA_PROJECT_TOKEN")??throw new ArgumentException("Set RA_PROJECT_TOKEN privately in the process environment.");using var runner=new RetryAssuranceClient(options);var completed=await runner.RunAsync(capability,operation,token);
                Console.WriteLine($"Run: {completed.Evidence.Run.Id}\nServer verdict: {completed.Evidence.Run.Result?.Verdict}\nDurable resources: {completed.Evidence.Resources.Count}\nAccepted HTTP attempts: {completed.Evidence.Requests.Count}\nRunner-declared terminal: {completed.Evidence.Run.Result?.RunnerReported.Terminal}\nJournal observed before logical callback completion: {completed.DurableEffectObservedBeforeClientCompleted} (local observation; not proof of ordering before the first timeout)");
                foreach(var message in completed.Evidence.Run.Result?.Issues??[])Console.WriteLine("Result detail: "+message);
                Console.WriteLine("Evidence page: "+new Uri(options.ServiceOrigin,"runs/"+completed.Evidence.Run.Id));
                if(values.TryGetValue("--json",out var path)){var fullPath=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);await File.WriteAllTextAsync(fullPath,JsonSerializer.Serialize(completed,new JsonSerializerOptions(JsonSerializerDefaults.Web){WriteIndented=true}),token);Log($"Sanitized runner evidence written; path={fullPath}; capabilitiesIncluded=false",isRun:IsLogEnabled);}
                return completed;
            },canceled.Token,"ExecuteRealClientAdapter");
            if(!result.Succeeded||result.Value is null){Console.Error.WriteLine("The adapter action did not complete. No passing verdict was fabricated. Read the local metadata log and preserve the run ID if one was returned.");return 2;}
            return result.Value.Evidence.Run.Result?.Verdict switch{"pass"=>0,"fail"=>1,_=>2};
        }finally{Console.CancelKeyPress-=cancellation;}
    }
}
