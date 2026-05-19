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
                "OpenAI API key is missing. Set it with: dotnet user-secrets set \"OpenAI:ApiKey\" \"YOUR_KEY\"");
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
                You write polished Facebook posts for a brokerage marketing team.

                Rules:
                - Write one Facebook post only.
                - Use only the listing facts provided.
                - Do not invent amenities, locations, specs, availability, awards, or contact details.
                - Do not use emojis.
                - Do not mention that you are an AI.
                - Do not use markdown formatting.
                - Use a professional, clear, marketing-friendly tone.
                - Keep the post between 80 and 180 words.
                - End with a soft call to action.
                - Include 2 to 4 relevant hashtags at the end.
                """),
            new UserChatMessage($"""
                Create a Facebook post for this listing.

                Listing title:
                {listing.Title}

                Address or location:
                {listing.Address}

                Price:
                {listing.Price:C0}

                Description:
                {listing.Description}
                """)
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        return completion.Content[0].Text.Trim();
    }
}