using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LabeledByAI.Services;

public static partial class ChatClientExtensions
{
    public static async Task<string?> CompleteJsonAsync(this IChatClient chatClient, string systemPrompt, string assistantPrompt, ILogger? logger = null)
    {
        logger?.LogInformation("Generating OpenAI request...");

        IList<ChatMessage> messages =
        [
            new(ChatRole.System, systemPrompt),
            new(ChatRole.Assistant, assistantPrompt),
        ];

        logger?.LogInformation(
            """
            messages >>>
            {messages}
            <<< messages
            """,
            string.Join(Environment.NewLine, messages.Select(m => $"{m.Role} => {m.Text}")));

        logger?.LogInformation("Sending a request to OpenAI...");

        var options = new ChatOptions
        {
            MaxOutputTokens = 1000,
            ResponseFormat = ChatResponseFormat.Json
        };
        var response = await chatClient.CompleteAsync(messages, options);

        logger?.LogInformation("OpenAI has replied.");

        logger?.LogInformation(
            """
            response >>>
            {response}
            <<< response
            """,
            response);

        var responseJson = response.ToString();

        return responseJson;
    }
}
