
var builder = DistributedApplication.CreateBuilder(args);

var insights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights("app-insights")
    : null;

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai") // deploy with app
        .AddDeployment(new("ai-model", "gpt-4o-mini", "2024-07-18", "GlobalStandard"))
    : builder.AddConnectionString("openai"); // use existing

var funcStorage = builder.AddAzureStorage("func-storage")
    .RunAsEmulator();

var func = builder
    .AddAzureFunctionsProject<Projects.LabeledByAI>("labeled-by-ai")
    .WithExternalHttpEndpoints()
    .WithHostStorage(funcStorage)
    .WithReference(openai)
    .WithOptionalReference(insights);

builder.Build().Run();
