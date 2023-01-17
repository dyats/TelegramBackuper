namespace OrderGiv3r.Application.Extensions;

public static class FileExtensions
{
    /// <summary>
    /// Returns you a boolean value that states if file exists AND if it's downloaded at 100%
    /// </summary>
    public static bool IsFileAlreadyExistsAndFullyDownloaded(string fileToScan, long fileSizeToDownload)
    {
        var isFileExists = File.Exists(fileToScan);

        if (!isFileExists)
        {
            return false;
        }

        var fileInfo = new FileInfo(fileToScan);

        // if file exists but not downloaded for 100%, let's download it again
        var isFileFullyDownloaded = fileInfo.Length == fileSizeToDownload;
        if (!isFileFullyDownloaded)
        {
            Console.WriteLine($"Overwriting the file {fileInfo.Name}.");

            return false;
        }

        return true;
    }
}
