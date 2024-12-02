using LabeledByAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LabeledByAI;

public abstract class BaseFunction<TBody>(ILogger logger)
{
    public virtual async Task<IActionResult> Run(HttpRequest request)
    {
        logger.LogInformation("Function is starting...");

        var parsedBody = await ParseRequestBodyAsync(request);

        if (parsedBody is null)
        {
            logger.LogError("No new issue was provided in the request body.");
            return new BadRequestObjectResult("The new issue is null.");
        }

        try
        {
            return await OnRun(request, parsedBody);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute the function.");
            return new BadRequestObjectResult("Failed to execute the function.");
        }
        finally
        {
            logger.LogInformation("Function run is complete.");
        }
    }

    protected abstract Task<IActionResult> OnRun(HttpRequest request, TBody parsedBody);

    private async Task<TBody?> ParseRequestBodyAsync(HttpRequest request)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<TBody>(request.Body, JsonExtensions.SerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize the request body.");
            return default;
        }
    }
}
