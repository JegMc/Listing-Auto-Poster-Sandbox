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
        var location = !string.IsNullOrWhiteSpace(listing.Location)
            ? listing.Location
            : listing.Address;

        var priceText = listing.Price > 0
            ? listing.Price.ToString("C0")
            : "Price on request";

        var lengthText = listing.LengthFeet.HasValue
            ? $"{listing.LengthFeet.Value:0.#} ft"
            : "Unspecified";

        var yearText = listing.YearBuilt.HasValue
            ? listing.YearBuilt.Value.ToString()
            : "Unspecified";

        var cabinText = listing.Cabins.HasValue
            ? listing.Cabins.Value.ToString()
            : "Unspecified";

        var guestText = listing.Guests.HasValue
            ? listing.Guests.Value.ToString()
            : "Unspecified";

        var speedText = listing.MaxSpeedKnots.HasValue
            ? $"{listing.MaxSpeedKnots.Value:0.#} knots"
            : "Unspecified";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("""
                You write polished social media captions for a yacht brokerage marketing team.

                Rules:
                - Write one platform-neutral caption that can work for Facebook, LinkedIn, or Instagram text.
                - Use only the yacht facts provided.
                - Do not invent amenities, locations, awards, refit history, availability, crew details, or contact details.
                - Do not mention that you are an AI.
                - Do not use markdown formatting.
                - Use a professional yacht broker tone: polished, specific, and restrained.
                - Mention concrete yacht facts when available: builder, length, year, location, cabins, guest capacity, speed, price.
                - Keep the post between 80 and 180 words.
                - End with a soft call to action.
                - Include 2 to 5 relevant yacht or brokerage hashtags at the end.
                """),
            new UserChatMessage($"""
                Create a social media caption from the information below.

                The information may be structured yacht data, rough user notes, or a plain English marketing request.
                Treat the Description / Custom Details section as the most important source of truth.

                Optional structured fields:
                Yacht name/listing title: {listing.Title}
                Builder: {listing.Builder}
                Brokerage/company: {listing.BrokerageCompany}
                Length: {lengthText}
                Year built: {yearText}
                Location: {location}
                Asking price: {priceText}
                Cabins: {cabinText}
                Guest capacity: {guestText}
                Max speed: {speedText}

                Description / Custom Details:
                {listing.Description}
                """)
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            messages,
            cancellationToken: cancellationToken);

        return completion.Content[0].Text.Trim();
    }
}