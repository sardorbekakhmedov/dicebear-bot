using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DicebearBot.Services;

public interface IHttpClientHelperService
{
    Task<Stream> GetImageStreamAsync(string command, string seed, CancellationToken cancellationToken);

    Task<Stream> GetImageStreamAsync(string command, string seed, string formatImage, string? backgroundColor = null,
        CancellationToken cancellationToken = default);
}


public class HttpClientHelperService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpClientHelperService> logger) : IHttpClientHelperService
{

    public async Task<Stream> GetImageStreamAsync(string command, string seed, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Dicebear:BaseUrl"];

        if (string.IsNullOrEmpty(baseUrl))
        {
            logger.LogError("Dicebear API BaseUrl not found in configuration!, Path: DicebearBot/appsettings.json, Parameter: Dicebear:BaseUrl");
            throw new DicebearBotException("Dicebear API BaseUrl not found in configuration!, Path: DicebearBot/appsettings.json, Parameter: Dicebear:BaseUrl");
        }

        var url = $"{baseUrl}{command}/png?seed={seed}";

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);

            logger.LogInformation($"Http request Url: {url}, Status http request: {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
        catch (DicebearBotException ex)
        {
            logger.LogError(ex, $"Failed to complete the request,  error message: {ex.Message}");
            throw new DicebearBotException($"Error message: {ex.Message},  Full error data:  {ex}");
        }
    }
    
    public async Task<Stream> GetImageStreamAsync(string command, string seed, string formatImage, string? backgroundColor = null, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Dicebear:BaseUrl"];

        if (string.IsNullOrEmpty(baseUrl))
        {
            logger.LogError("Dicebear API BaseUrl not found in configuration!, Path: DicebearBot/appsettings.json, Parameter: Dicebear:BaseUrl");
            throw new DicebearBotException("Dicebear API BaseUrl not found in configuration!, Path: DicebearBot/appsettings.json, Parameter: Dicebear:BaseUrl");
        }
        
        var url = $"{baseUrl}{command}/{formatImage}?seed={seed}";
        
        var newUrl = backgroundColor != null ? $"{url}&background={backgroundColor}" : url;

        try
        {
            var response = await httpClient.GetAsync(newUrl, cancellationToken);

            logger.LogInformation($"Http request Url: {newUrl}, Status http request: {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (DicebearBotException ex)
        {
            logger.LogError(ex, $"Failed to complete the request,  error message: {ex.Message}");
            throw new DicebearBotException($"Error message: {ex.Message},  Full error data:  {ex}");
        }
    }

}
/*https://api.dicebear.com/8.x/bottts/png?seed=sd
https://api.dicebear.com/8.x/bottts/PNG?seed=ao*/