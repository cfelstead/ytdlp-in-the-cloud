INSERT INTO YtDlp_DownloadRequests
(DownloadId, DownloadRequested, Url)
VALUES
    (NEWID(), GETUTCDATE(), 'https://www.youtube.com/playlist?list=PLUOequmGnXxPPcrN0PFclBABXEckcPzYY'),
    (NEWID(), GETUTCDATE(), 'https://www.youtube.com/watch?v=Z7TeTIs4KOE')