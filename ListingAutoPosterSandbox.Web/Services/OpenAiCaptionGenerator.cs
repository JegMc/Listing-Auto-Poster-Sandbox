using ListingAutoPosterSandbox.Web.Models;
using OpenAI.Chat;

namespace ListingAutoPosterSandbox.Web.Services;

public class OpenAiCaptionGenerator : ICaptionGenerator
{
    private readonly ChatClient _chatClient;

    public OpenAiCaptionGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-5-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. Set it with:\n\n dotnet user-secrets set \"OpenAI:ApiKey\" \"YOUR_KEY\" \n\n");
        }

        _chatClient = new ChatClient(model: model, apiKey: apiKey);
    }

    public async Task<string> GenerateCaptionAsync(
        Listing listing,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("""
                You write professional real estate social media captions.

                Rules:
                - Write one caption only.
                - Make it sound polished but not exaggerated.
                - Mention the listing's strongest selling points.
                - Include 3 to 5 relevant hashtags.
                - Do not invent facts that are not provided.
                - Do not include emojis.
                """),

            new UserChatMessage($"""
                Create a social media caption for this real estate listing.

                Title: {listing.Title}
                Address: {listing.Address}
                Price: {listing.Price:C0}
                Description: {listing.Description}
                """)
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        return completion.Content[0].Text.Trim();
    }
}