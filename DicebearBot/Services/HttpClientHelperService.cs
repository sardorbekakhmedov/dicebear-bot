using Microsoft.Extensions.Logging;

namespace DicebearBot.Services;

public interface IHttpClientHelperService
{
    Task<(Stream, bool responseStatus)> GetImageStreamAsync(string command, string seed);
}


public class HttpClientHelperService(
    HttpClient httpClient,
    ILogger<HttpClientHelperService> logger) : IHttpClientHelperService
{

    public async Task<(Stream, bool responseStatus)> GetImageStreamAsync(string command, string seed)
    {
        var url = $"https://api.dicebear.com/8.x{command}/png?seed={seed}";

        try
        {
            var response = await httpClient.GetAsync(url);

            logger.LogInformation($"Http request Url: {url}, Status http request: {response.StatusCode}");
            
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadAsStreamAsync(), response.IsSuccessStatusCode);
        }
        catch (DicebearBotException ex)
        {
            logger.LogError(ex, $"Failed to complete the request,  error message: {ex.Message}", ex.Message);
            throw new Exception($"Error message: {ex.Message},  Full error data:  {ex}", ex);
        }
    }
}


public class DicebearBotException(string message) : Exception(message);