﻿using TL;

namespace OrderGiv3r.ContentBackuper.Interfaces;

public interface IBackupService
{
    Task DownloadDocumentFromTgAsync(MessageMedia media);
    Task DownloadPhotoFromTgAsync(Photo photo);
    Task DownloadVideoFromTgAsync(Document document);
    Task DownloadVideoFromSiteAsync(int videoNumber, string baseUrl, string htmlMatchCondition, int regexMatchGroup);
    Task DownloaFileFromTwitterAsync(long tweetId);
}
