using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ListingAutoPosterSandbox.Web.Models;

namespace ListingAutoPosterSandbox.Web.Services;

public class OpenAiCaptionGenerator : ICaptionGenerator
{
    private const long MaxImageBytes = 10 * 1024 * 1024;

    private readonly HttpClient _openAiClient;
    private readonly HttpClient _imageHttpClient;
    private readonly IWebHostEnvironment _environment;
    private readonly string _model;

    public OpenAiCaptionGenerator(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        _model = configuration["OpenAI:Model"] ?? "gpt-5-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. Set it with: dotnet user-secrets set \"OpenAI:ApiKey\" \"YOUR_KEY\"");
        }

        _environment = environment;

        _openAiClient = httpClientFactory.CreateClient();
        _openAiClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _openAiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        // Separate client so the OpenAI Authorization header is never sent to external image hosts.
        _imageHttpClient = httpClientFactory.CreateClient();
        _imageHttpClient.Timeout = TimeSpan.FromSeconds(15);

        _imageHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ListingAutoPosterSandbox/1.0");

        _imageHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/jpeg"));

        _imageHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/png"));

        _imageHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/gif"));

        _imageHttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/webp"));
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

        var userPrompt = $"""
            Create a social media caption from the information below.

            The information may be structured yacht data, rough user notes, or a plain English marketing request.
            Treat the Description / Custom Details section as the most important source of truth.

            If a hero image is provided, use it only as visual context.
            You may mention visual traits that are clearly visible, such as exterior profile, deck space, flybridge, salon, water setting, or overall presentation.
            Do not invent yacht facts from the image.

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
            """;

        var imageInput = await BuildImageInputAsync(
            listing.ImageUrl,
            cancellationToken);

        var requestBody = BuildRequestBody(
            userPrompt,
            imageInput);

        using var firstResponse = await PostOpenAiRequestAsync(
            requestBody,
            cancellationToken);

        var firstResponseJson = await firstResponse.Content.ReadAsStringAsync(
            cancellationToken);

        if (!firstResponse.IsSuccessStatusCode)
        {
            // If OpenAI rejects the image, retry once without image context.
            // This prevents placeholder/broken/unsupported image URLs from crashing the workflow.
            if (!string.IsNullOrWhiteSpace(imageInput) &&
                IsInvalidImageResponse(firstResponseJson))
            {
                return await GenerateTextOnlyCaptionAsync(
                    userPrompt,
                    cancellationToken);
            }

            throw new InvalidOperationException(
                $"OpenAI caption generation failed with status {(int)firstResponse.StatusCode}: {firstResponseJson}");
        }

        var caption = ExtractOutputText(firstResponseJson);

        if (string.IsNullOrWhiteSpace(caption) && !string.IsNullOrWhiteSpace(imageInput))
        {
            // Successful HTTP response, but no usable text. Retry without image.
            // This handles odd image/model responses without killing the workflow.
            caption = await GenerateTextOnlyCaptionAsync(
                userPrompt,
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(caption))
        {
            throw new InvalidOperationException(
                $"OpenAI returned an empty caption. Raw response preview: {TrimForDebug(firstResponseJson)}");
        }

        return caption.Trim();
    }

    private async Task<string> GenerateTextOnlyCaptionAsync(
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var textOnlyRequestBody = BuildRequestBody(
            userPrompt,
            imageInput: null);

        using var retryResponse = await PostOpenAiRequestAsync(
            textOnlyRequestBody,
            cancellationToken);

        var retryResponseJson = await retryResponse.Content.ReadAsStringAsync(
            cancellationToken);

        if (!retryResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI caption generation failed with status {(int)retryResponse.StatusCode}: {retryResponseJson}");
        }

        var retryCaption = ExtractOutputText(retryResponseJson);

        if (string.IsNullOrWhiteSpace(retryCaption))
        {
            throw new InvalidOperationException(
                $"OpenAI returned an empty text-only caption. Raw response preview: {TrimForDebug(retryResponseJson)}");
        }

        return retryCaption.Trim();
    }

    private Dictionary<string, object?> BuildRequestBody(
        string userPrompt,
        string? imageInput)
    {
        var content = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["type"] = "input_text",
                ["text"] = userPrompt
            }
        };

        if (!string.IsNullOrWhiteSpace(imageInput))
        {
            content.Add(new Dictionary<string, object?>
            {
                ["type"] = "input_image",
                ["image_url"] = imageInput
            });
        }

        return new Dictionary<string, object?>
        {
            ["model"] = _model,
            ["instructions"] = """
                You write polished social media captions for a yacht brokerage marketing team.

                Rules:
                - Write one platform-neutral caption that can work for Facebook, LinkedIn, or Instagram text.
                - Use only the yacht facts provided.
                - Do not invent amenities, locations, awards, refit history, availability, crew details, or contact details.
                - If an image is provided, use it only for visible presentation details.
                - Do not claim visual features unless they are clearly visible in the image.
                - Do not mention that you are an AI.
                - Do not use markdown formatting.
                - Use a professional yacht broker tone: polished, specific, and restrained.
                - Mention concrete yacht facts when available: builder, length, year, location, cabins, guest capacity, speed, price.
                - Keep the post between 80 and 180 words.
                - End with a soft call to action.
                - Include 2 to 5 relevant yacht or brokerage hashtags at the end.
                """,
            ["input"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = content
                }
            },
            ["max_output_tokens"] = 1200
        };
    }

    private async Task<HttpResponseMessage> PostOpenAiRequestAsync(
        Dictionary<string, object?> requestBody,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(requestBody);

        using var httpContent = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return await _openAiClient.PostAsync(
            "responses",
            httpContent,
            cancellationToken);
    }

    private async Task<string?> BuildImageInputAsync(
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return await BuildRemoteImageDataUrlAsync(
                absoluteUri,
                cancellationToken);
        }

        var localPath = TryGetLocalImagePath(imageUrl);

        if (localPath is null || !System.IO.File.Exists(localPath))
        {
            return null;
        }

        return await BuildLocalImageDataUrlAsync(
            localPath,
            cancellationToken);
    }

    private async Task<string?> BuildRemoteImageDataUrlAsync(
        Uri imageUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                imageUri);

            using var response = await _imageHttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = InferImageContentTypeFromPath(imageUri.AbsolutePath);
            }

            if (!IsSupportedOpenAiImageContentType(contentType))
            {
                return null;
            }

            var contentLength = response.Content.Headers.ContentLength;

            if (contentLength.HasValue && contentLength.Value > MaxImageBytes)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(
                cancellationToken);

            if (bytes.Length == 0 || bytes.Length > MaxImageBytes)
            {
                return null;
            }

            if (!LooksLikeImage(bytes, contentType))
            {
                return null;
            }

            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> BuildLocalImageDataUrlAsync(
        string localPath,
        CancellationToken cancellationToken)
    {
        var contentType = InferImageContentTypeFromPath(localPath);

        if (!IsSupportedOpenAiImageContentType(contentType))
        {
            return null;
        }

        var fileInfo = new FileInfo(localPath);

        if (!fileInfo.Exists || fileInfo.Length == 0 || fileInfo.Length > MaxImageBytes)
        {
            return null;
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(
            localPath,
            cancellationToken);

        if (!LooksLikeImage(bytes, contentType))
        {
            return null;
        }

        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private string? TryGetLocalImagePath(string imageUrl)
    {
        if (!imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = imageUrl
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot");
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(webRootPath, relativePath));

        var normalizedWebRootPath = Path.GetFullPath(webRootPath);

        if (!fullPath.StartsWith(normalizedWebRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private static string? InferImageContentTypeFromPath(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => null
        };
    }

    private static bool IsSupportedOpenAiImageContentType(
        string? contentType)
    {
        return contentType is
            "image/jpeg" or
            "image/png" or
            "image/gif" or
            "image/webp";
    }

    private static bool LooksLikeImage(
        byte[] bytes,
        string? contentType)
    {
        if (bytes.Length < 12)
        {
            return false;
        }

        if (contentType == "image/jpeg")
        {
            return bytes[0] == 0xFF &&
                   bytes[1] == 0xD8;
        }

        if (contentType == "image/png")
        {
            return bytes[0] == 0x89 &&
                   bytes[1] == 0x50 &&
                   bytes[2] == 0x4E &&
                   bytes[3] == 0x47;
        }

        if (contentType == "image/gif")
        {
            return bytes[0] == 0x47 &&
                   bytes[1] == 0x49 &&
                   bytes[2] == 0x46;
        }

        if (contentType == "image/webp")
        {
            return bytes[0] == 0x52 &&
                   bytes[1] == 0x49 &&
                   bytes[2] == 0x46 &&
                   bytes[3] == 0x46 &&
                   bytes[8] == 0x57 &&
                   bytes[9] == 0x45 &&
                   bytes[10] == 0x42 &&
                   bytes[11] == 0x50;
        }

        return false;
    }

    private static bool IsInvalidImageResponse(
        string responseJson)
    {
        return responseJson.Contains("invalid_image", StringComparison.OrdinalIgnoreCase) ||
               responseJson.Contains("does not represent a valid image", StringComparison.OrdinalIgnoreCase) ||
               responseJson.Contains("unsupported image", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractOutputText(
        string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return string.Empty;
        }

        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextElement))
        {
            var directOutputText = ExtractStringOrJoinedArray(outputTextElement);

            if (!string.IsNullOrWhiteSpace(directOutputText))
            {
                return directOutputText;
            }
        }

        if (root.TryGetProperty("output", out var outputElement) &&
            outputElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement) ||
                    contentElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement))
                    {
                        var text = ExtractStringOrJoinedArray(textElement);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }

                    if (contentItem.TryGetProperty("output_text", out var nestedOutputTextElement))
                    {
                        var nestedOutputText = ExtractStringOrJoinedArray(nestedOutputTextElement);

                        if (!string.IsNullOrWhiteSpace(nestedOutputText))
                        {
                            return nestedOutputText;
                        }
                    }
                }
            }
        }

        var recursiveText = FindFirstTextValue(root);

        return recursiveText ?? string.Empty;
    }

    private static string ExtractStringOrJoinedArray(
        JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        parts.Add(value);
                    }
                }
            }

            return string.Join(
                Environment.NewLine,
                parts);
        }

        return string.Empty;
    }

    private static string? FindFirstTextValue(
        JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("text") || property.NameEquals("output_text")) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                var nestedValue = FindFirstTextValue(property.Value);

                if (!string.IsNullOrWhiteSpace(nestedValue))
                {
                    return nestedValue;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nestedValue = FindFirstTextValue(item);

                if (!string.IsNullOrWhiteSpace(nestedValue))
                {
                    return nestedValue;
                }
            }
        }

        return null;
    }

    private static string TrimForDebug(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const int maxLength = 1200;

        return value.Length <= maxLength
            ? value
            : value[..maxLength] + "...";
    }
}