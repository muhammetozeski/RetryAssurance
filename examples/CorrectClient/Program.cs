using RetryAssurance.Examples;
using RetryAssurance.Runner;
return await RunnerCommand.ExecuteAsync(args,"RetryAssurance.FirstParty.CorrectClient","1.0.0",(context,cancellationToken)=>new SyntheticJobClient(context.HttpClient,false).CreateJobAsync(cancellationToken));
