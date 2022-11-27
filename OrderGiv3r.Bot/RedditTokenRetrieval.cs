using Reddit.AuthTokenRetriever;
using System.Diagnostics;

namespace OrderGiv3r.Bot;

public static class RedditTokenRetrieval
{
    public static (string accessToken, string refreshToken) AuthorizeUser(string appId, string appSecret = null, int port = 8080)
    {
        // Create a new instance of the auth token retrieval library.  --Kris
        AuthTokenRetrieverLib authTokenRetrieverLib = new AuthTokenRetrieverLib(appId, port, appSecret: appSecret);

        // Start the callback listener.  --Kris
        // Note - Ignore the logging exception message if you see it.  You can use Console.Clear() after this call to get rid of it if you're running a console app.
        authTokenRetrieverLib.AwaitCallback();

        // Open the browser to the Reddit authentication page.  Once the user clicks "accept", Reddit will redirect the browser to localhost:8080, where AwaitCallback will take over.  --Kris
        OpenBrowser(authTokenRetrieverLib.AuthURL());

        Console.WriteLine("Accept reddit rules. Press any key and continue...");
        Console.ReadKey();
        Console.WriteLine("\r");

        // Cleanup.  --Kris
        authTokenRetrieverLib.StopListening();

        return (authTokenRetrieverLib.AccessToken, authTokenRetrieverLib.RefreshToken);
    }

    public static void OpenBrowser(string authUrl, string browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe")
    {
        try
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(authUrl);
            Process.Start(processStartInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // This typically occurs if the runtime doesn't know where your browser is.  Use BrowserPath for when this happens.  --Kris
            ProcessStartInfo processStartInfo = new ProcessStartInfo(browserPath)
            {
                Arguments = authUrl
            };
            Process.Start(processStartInfo);
        }
    }
}
