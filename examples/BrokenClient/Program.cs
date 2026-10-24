using RetryAssurance.Examples;
using RetryAssurance.Runner;
return await RunnerCommand.ExecuteAsync(args,"RetryAssurance.FirstParty.BrokenClient","1.0.0",(context,cancellationToken)=>new SyntheticJobClient(context.HttpClient,true).CreateJobAsync(cancellationToken));
