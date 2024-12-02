
var builder = DistributedApplication.CreateBuilder(args);

// Only use Application Insights in the published app.
var insights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights("app-insights")
    : null;

// Use the Azure OpenAI in the published app, but a local Ollama
// in development or connect to an existing Azure OpenAI.
IResourceBuilder<IResourceWithConnectionString> ai =
    builder.ExecutionContext.IsPublishMode
        ? builder.AddAzureOpenAI("ai")
            .AddDeployment(new("ai-model", "gpt-4o-mini", "2024-07-18", "GlobalStandard"))
#if USE_LOCAL_AI
        : builder.AddOllama("ai")
            .WithDataVolume()
            .WithContainerRuntimeArgs("--gpus=all")
            .WithOpenWebUI()
            .AddModel("ai-model", "llama3.1");
#else
        : builder.AddConnectionString("ai");
#endif

// Specify the storage for the function.
var funcStorage = builder.AddAzureStorage("func-storage")
    .RunAsEmulator();

// Export the function with public endpoints.
var func = builder
    .AddAzureFunctionsProject<Projects.LabeledByAI>("labeled-by-ai")
    .WithExternalHttpEndpoints()
    .WithHostStorage(funcStorage)
    .WithReference(ai)
    .WithOptionalReference(insights);

builder.Build().Run();
