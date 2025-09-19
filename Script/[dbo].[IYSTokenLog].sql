SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[IYSTokenLog]
(
    [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_IYSTokenLog] PRIMARY KEY,
    [CompanyCode] NVARCHAR(32) NOT NULL,
    [AccessTokenMasked] NVARCHAR(256) NULL,
    [RefreshTokenMasked] NVARCHAR(256) NULL,
    [TokenCreateDateUtc] DATETIME2(0) NOT NULL,
    [TokenRefreshDateUtc] DATETIME2(0) NULL,
    [Operation] NVARCHAR(32) NOT NULL,
    [ServerIdentifier] NVARCHAR(128) NOT NULL,
    [CreatedAtUtc] DATETIME2(0) NOT NULL CONSTRAINT [DF_IYSTokenLog_CreatedAtUtc] DEFAULT (SYSUTCDATETIME())
);
GO

CREATE NONCLUSTERED INDEX [IX_IYSTokenLog_CompanyCode]
    ON [dbo].[IYSTokenLog]([CompanyCode], [CreatedAtUtc]);
GO
