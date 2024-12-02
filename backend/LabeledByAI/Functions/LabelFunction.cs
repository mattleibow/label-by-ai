using LabeledByAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LabeledByAI;

public class LabelFunction(GetBestLabelService service, ILogger<LabelFunction> logger)
    : BaseFunction<GetBestLabelRequest>(logger)
{
    [Function("label")]
    public override Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request) =>
        base.Run(request);

    protected override async Task<IActionResult> OnRun(HttpRequest request, GetBestLabelRequest parsedBody)
    {
        try
        {
            var response = await service.ExecuteAsync(parsedBody, request.GetGithubToken());
            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute the service.");
            return new BadRequestObjectResult("Failed to execute the service.");
        }
    }
}
