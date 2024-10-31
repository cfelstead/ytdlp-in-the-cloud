IF (NOT EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo' 
                 AND  TABLE_NAME = 'YtDlp_DownloadRequests'))
BEGIN

CREATE TABLE [dbo].[YtDlp_DownloadRequests](
    [DownloadId] [uniqueidentifier] NOT NULL,
    [DownloadRequested] [datetime2](7) NOT NULL,
    [Url] [varchar](500) NOT NULL,
    [DownloadStarted] [datetime2](7) NULL,
    [DownloadCompleted] [datetime2](7) NULL,
    [DownloadError] [varchar](500) NULL,
    CONSTRAINT [PK_YtDlp_DownloadRequests] PRIMARY KEY CLUSTERED
(
[DownloadId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]

END