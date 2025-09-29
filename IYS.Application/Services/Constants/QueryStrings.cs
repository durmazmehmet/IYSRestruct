namespace IYS.Application.Services.Constants
{
    public partial class QueryStrings
    {
        public static string InsertTokenLog = @"
            INSERT INTO SfdcMasterData.dbo.IYSTokenLog
                (
                    CompanyCode,
                    AccessTokenMasked,
                    RefreshTokenMasked,
                    TokenUpdateDateUtc,
                    Operation,
                    ServerIdentifier,
                    CreatedAtUtc
                )
            VALUES(
                @CompanyCode,
                @AccessTokenMasked,
                @RefreshTokenMasked,
                @TokenUpdateDateUtc,
                @Operation, 
                @ServerIdentifier,
                SYSUTCDATETIME()
                );";

        public static string InsertRequest = @"
            INSERT INTO SfdcMasterData.dbo.IYSCallLog
                (IysCode,
                 Action,
                 Url, 
                 Method,   
                 Request, 
                 CreateDate,
                 BatchId
                )
            VALUES(
                @IysCode,
                @Action,
                @Url,
                @Method,
                @Request,
                GETDATE(),
                @BatchId
                );
            SELECT SCOPE_IDENTITY()
            ";

        public static string UpdateRequest = @"
            UPDATE SfdcMasterData.dbo.IYSCallLog
            SET Response = @Response,
                IsSuccess = @IsSuccess,
                ResponseCode = @ResponseCode,
                UpdateDate = GETDATE()
            WHERE Id = @Id;";

        public static string InsertConsentRequest = @"
            IF @Status IN ('RED', 'RET')
            BEGIN
                IF EXISTS (SELECT 1 FROM SfdcMasterData.dbo.IysPullConsent (NOLOCK) WHERE Recipient = @Recipient)
                   OR EXISTS (SELECT 1 FROM SfdcMasterData.dbo.IYSConsentRequest (NOLOCK) WHERE Recipient = @Recipient AND IsProcessed = 1)
                BEGIN
                    INSERT INTO SfdcMasterData.dbo.IYSConsentRequest
                        (
                        CompanyCode,
                        SalesforceId,
                        IysCode,
                        BrandCode,
                        ConsentDate,
                        Source,
                        Recipient,
                        RecipientType,
                        Status,
                        Type,
                        CreateDate,
                        IsProcessed
                        )
                    VALUES(
                        @CompanyCode,
                        @SalesforceId,
                        @IysCode,
                        @BrandCode,
                        @ConsentDate,
                        @Source,
                        @Recipient,
                        @RecipientType,
                        @Status,
                        @Type,
                        GETDATE(),
                        0
                        );
                    SELECT SCOPE_IDENTITY();
                END
                ELSE
                    SELECT 0;
            END
            ELSE
            BEGIN
                INSERT INTO SfdcMasterData.dbo.IYSConsentRequest
                    (
                    CompanyCode,
                    SalesforceId,
                    IysCode,
                    BrandCode,
                    ConsentDate,
                    Source,
                    Recipient,
                    RecipientType,
                    Status,
                    Type,
                    CreateDate,
                    IsProcessed
                    )
                VALUES(
                    @CompanyCode,
                    @SalesforceId,
                    @IysCode,
                    @BrandCode,
                    @ConsentDate,
                    @Source,
                    @Recipient,
                    @RecipientType,
                    @Status,
                    @Type,
                    GETDATE(),
                    0
                    );
                SELECT SCOPE_IDENTITY();
            END";

        public static string UpdateConsentRequestFromCommon = @"
            UPDATE SfdcMasterData.dbo.IYSConsentRequest
                                SET BatchId = @BatchId,
                                        IsSuccess = @IsSuccess,
                                        LogId = @LogId,
                                        UpdateDate = GETDATE(),
                IsProcessed = 1,
                TransactionId = @TransactionId,
                CreationDate = @CreationDate
            WHERE Id = @Id;";

        public static string QueryConsentRequest = @"
            SELECT
                cr.Id,
                ISNULL(cl.Id, 0) AS LogId,
                ISNULL(cr.IsProcessed, 0) AS IsProcessed,
                cr.IsSuccess,
                cl.Response,
                CONVERT(varchar(19), cl.UpdateDate, 20) AS SendDate
            FROM IYSConsentRequest cr WITH (NOLOCK)
            LEFT JOIN IYSCallLog cl WITH (NOLOCK) ON cr.LogId = cl.Id
            WHERE cr.Recipient LIKE '%' + @recipient + '%'";

        public static string GetConsentRequestById = @"
            SELECT TOP 1
                cr.CompanyCode,
                cr.Id,
                cr.IysCode,
                cr.BrandCode,
                CONVERT(varchar(19), cr.ConsentDate, 20) AS ConsentDate,
                cr.[Source],
                cr.Recipient,
                cr.RecipientType,
                cr.Status,
                cr.[Type]
            FROM dbo.IYSConsentRequest cr WITH (NOLOCK)
            WHERE cr.Id = @Id;";

        public static string CheckPullConsent = @"
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM SfdcMasterData.dbo.IysPullConsent (NOLOCK)
                WHERE CompanyCode = @CompanyCode
                  AND Recipient = @Recipient
                  AND (@Type IS NULL OR @Type = '' OR ISNULL(Type, '') = ISNULL(@Type, ''))
            ) THEN 1 ELSE 0 END;";

        public static string CheckSuccessfulConsentRequest = @"
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM SfdcMasterData.dbo.IYSConsentRequest (NOLOCK)
                WHERE CompanyCode = @CompanyCode
                  AND Recipient = @Recipient
                  AND (@Type IS NULL OR @Type = '' OR ISNULL(Type, '') = ISNULL(@Type, ''))
                  AND ISNULL(IsProcessed, 0) = 1
                  AND ISNULL(IsSuccess, 0) = 1
                  AND ISNULL(IsOverdue, 0) = 0
                  AND (BatchError IS NULL OR LTRIM(RTRIM(BatchError)) = '')
            ) THEN 1 ELSE 0 END;";

        public static string GetExistingConsentRecipients = @"
            SELECT DISTINCT Recipient
            FROM (
                SELECT Recipient
                FROM SfdcMasterData.dbo.IysPullConsent (NOLOCK)
                WHERE CompanyCode = @CompanyCode
                  AND Recipient IN @Recipients
                  AND (@Type IS NULL OR @Type = '' OR ISNULL(Type, '') = ISNULL(@Type, ''))
                UNION ALL
                SELECT Recipient
                FROM SfdcMasterData.dbo.IYSConsentRequest (NOLOCK)
                WHERE CompanyCode = @CompanyCode
                  AND Recipient IN @Recipients
                  AND (@Type IS NULL OR @Type = '' OR ISNULL(Type, '') = ISNULL(@Type, ''))
                  AND ISNULL(IsProcessed, 0) = 1
                  AND ISNULL(IsSuccess, 0) = 1
                  AND ISNULL(IsOverdue, 0) = 0
                  AND (BatchError IS NULL OR LTRIM(RTRIM(BatchError)) = '')
            ) Existing;";

        public static string GetLatestConsentStates = @"
            WITH LatestConsent AS (
                SELECT
                    Recipient,
                    Status,
                    ConsentDate,
                    ROW_NUMBER() OVER (
                        PARTITION BY Recipient
                        ORDER BY ConsentDate DESC, SourcePriority ASC
                    ) AS RowNumber
                FROM (
                    SELECT
                        pc.Recipient,
                        pc.Status,
                        pc.ConsentDate,
                        0 AS SourcePriority
                    FROM SfdcMasterData.dbo.IysPullConsent pc WITH (NOLOCK)
                    WHERE pc.CompanyCode = @CompanyCode
                      AND ISNULL(pc.RecipientType, '') = ISNULL(@RecipientType, '')
                      AND ISNULL(pc.[Type], '') = ISNULL(@Type, '')
                      AND pc.Recipient IN @Recipients
                    UNION ALL
                    SELECT
                        cr.Recipient,
                        cr.Status,
                        cr.ConsentDate,
                        1 AS SourcePriority
                    FROM SfdcMasterData.dbo.IYSConsentRequest cr WITH (NOLOCK)
                    WHERE cr.CompanyCode = @CompanyCode
                      AND ISNULL(cr.RecipientType, '') = ISNULL(@RecipientType, '')
                      AND ISNULL(cr.[Type], '') = ISNULL(@Type, '')
                      AND cr.Recipient IN @Recipients
                      AND ISNULL(cr.IsProcessed, 0) = 1
                      AND ISNULL(cr.IsSuccess, 0) = 1
                      AND ISNULL(cr.IsOverdue, 0) = 0
                      AND (cr.BatchError IS NULL OR LTRIM(RTRIM(cr.BatchError)) = '')
                ) Combined
            )
            SELECT Recipient, Status, ConsentDate
            FROM LatestConsent
            WHERE RowNumber = 1;";

        public static string GetPendingConsents = @"
            SELECT TOP {0}
                    cr.CompanyCode,
                    cr.Id,
                    cr.IysCode,
                    cr.BrandCode,
                    CONVERT(varchar, cr.ConsentDate,20) AS ConsentDate,
                    cr.[Source],
                    cr.Recipient,
                    cr.RecipientType,
                    cr.Status,
                    cr.Type
            FROM dbo.IYSConsentRequest cr WITH (NOLOCK)
            WHERE ISNULL(cr.IsProcessed, 0) = 0
              AND ISNULL(cr.IsOverdue, 0) = 0
              --AND ISNULL(cr.IsPulled, 1) = 0
            ORDER BY cr.Id DESC;
            ";

        public static string GetPendingMultipleConsents = @"
            SELECT TOP (@RowCount)
                cr.Id,
                cr.CompanyCode,
                cr.SalesforceId,
                cr.IysCode,
                cr.BrandCode,
                CONVERT(varchar(19), cr.ConsentDate, 20) AS ConsentDate,
                cr.[Source],
                cr.Recipient,
                cr.RecipientType,
                cr.Status,
                cr.[Type],
                cr.BatchId,
                CONVERT(varchar(19), cr.CreateDate, 20) AS CreateDate
            FROM dbo.IYSConsentRequest cr WITH (NOLOCK)
            WHERE ISNULL(cr.IsProcessed, 0) = 0
              AND ISNULL(cr.IsOverdue, 0) = 0
            ORDER BY cr.CreateDate ASC, cr.Id ASC;";

        public static string GetPendingBatchIds = @"
            SELECT DISTINCT TOP (@MaxBatchCount)
                cr.BatchId
            FROM dbo.IYSConsentRequest cr WITH (NOLOCK)
            WHERE ISNULL(cr.IsProcessed, 0) = 1
              AND cr.BatchId IS NOT NULL
              AND cr.BatchId > 0
              AND (cr.IsSuccess IS NULL OR cr.IsSuccess = 0)
              AND ISNULL(cr.UpdateDate, cr.CreateDate) <= DATEADD(SECOND, -@MinimumAgeSeconds, GETDATE())
            ORDER BY cr.BatchId;";

        public static string GetConsentsByBatchId = @"
            SELECT
                cr.Id,
                cr.CompanyCode,
                cr.SalesforceId,
                cr.IysCode,
                cr.BrandCode,
                CONVERT(varchar(19), cr.ConsentDate, 20) AS ConsentDate,
                cr.[Source],
                cr.Recipient,
                cr.RecipientType,
                cr.Status,
                cr.[Type],
                cr.BatchId,
                cr.LogId,
                CONVERT(varchar(19), cr.CreateDate, 20) AS CreateDate
            FROM dbo.IYSConsentRequest cr WITH (NOLOCK)
            WHERE cr.BatchId = @BatchId
            ORDER BY cr.Id ASC;";

        public static string UpdateConsentRequest = @"
			IF EXISTS (SELECT * FROM dbo.IYSConsentRequest (nolock) WHERE Id = @Id)
			BEGIN
				UPDATE dbo.IYSConsentRequest
                                SET BatchId = @BatchId,
                                        IsSuccess = @IsSuccess,
                                        LogId = @LogId,
					UpdateDate = GETDATE(),
					IsProcessed = 1,
					TransactionId = @TransactionId,
					CreationDate = @CreationDate,
					BatchError = @BatchError,
					IsOverdue = @IsOverdue
				WHERE Id = @Id
		   END;";

        public static string GetPullRequestLog = @"
			SELECT Id, CompanyCode, IysCode, BrandCode, AfterId, UpdateDate
			FROM dbo.IysPullRequestLog (NOLOCK)
			WHERE CompanyCode = @CompanyCode;";

        public static string UpdatePullRequestLog = @"
		 IF EXISTS (Select * from dbo.IysPullRequestLog (NOLOCK) WHERE CompanyCode = @CompanyCode)		
				UPDATE dbo.IysPullRequestLog
				SET AfterId = @AfterId,
				UpdateDate = GETDATE(),
				RequestDate = GETDATE()			
				WHERE CompanyCode = @CompanyCode
		   ELSE 
				INSERT INTO dbo.IysPullRequestLog
				(CompanyCode, IysCode, BrandCode, AfterId, UpdateDate)
				VALUES(@CompanyCode, @IysCode, @BrandCode, @AfterId, GETDATE());

		;";

        public static string UpdateTokenResponseLog = @"
                 IF EXISTS (Select * from dbo.IysTokenResponseLog (NOLOCK) WHERE IysCode = @IysCode)
                                UPDATE dbo.IysTokenResponseLog
                                SET TokenResponse = @TokenResponse,
                                HaltUntilUtc = @HaltUntilUtc,
                                UpdateDate = GETDATE()
                                WHERE IysCode = @IysCode
                   ELSE
                                INSERT INTO dbo.IysTokenResponseLog
                                (IysCode, TokenResponse, RequestDate, HaltUntilUtc)
                                VALUES(@IysCode, @TokenResponse, GETDATE(), @HaltUntilUtc);

                ;";

        public static string GetTokenResponseLog = @"
                SELECT TOP 1 TokenResponse, HaltUntilUtc FROM dbo.IysTokenResponseLog (NOLOCK) WHERE IysCode = @IysCode;";

        public static string UpsertTokenHaltUntil = @"
                 IF EXISTS (Select * from dbo.IysTokenResponseLog (NOLOCK) WHERE IysCode = @IysCode)
                                UPDATE dbo.IysTokenResponseLog
                                SET HaltUntilUtc = @HaltUntilUtc,
                                UpdateDate = GETDATE()
                                WHERE IysCode = @IysCode
                   ELSE
                                INSERT INTO dbo.IysTokenResponseLog
                                (IysCode, TokenResponse, RequestDate, HaltUntilUtc)
                                VALUES(@IysCode, NULL, GETDATE(), @HaltUntilUtc);

                ;";

        public static string GetTokenHaltUntil = @"
                SELECT TOP 1 HaltUntilUtc FROM dbo.IysTokenResponseLog (NOLOCK) WHERE IysCode = @IysCode;";

        public static string UpdateJustRequestDateOfPullRequestLog = @"
		 IF EXISTS (Select * from dbo.IysPullRequestLog (NOLOCK) WHERE CompanyCode = @CompanyCode)		
				UPDATE dbo.IysPullRequestLog
				SET RequestDate = GETDATE()				
				WHERE CompanyCode = @CompanyCode
		   ELSE 
				INSERT INTO dbo.IysPullRequestLog
				(CompanyCode, IysCode, BrandCode, AfterId, UpdateDate)
				VALUES(@CompanyCode, @IysCode, @BrandCode, '', GETDATE());

		;";

        public static string InsertPullConsent = @"
            INSERT INTO dbo.IysPullConsent
                (CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, CreationDate, Source,
				 Recipient, RecipientType, Status, Type, TransactionId, CreateDate
                )
            VALUES(
                @CompanyCode, @SalesforceId, @IysCode, @BrandCode, @ConsentDate, @CreationDate, @Source, 
				@Recipient, @RecipientType, @Status, @Type, @TransactionId, GETDATE()
                );
		";

        public static string GetPullConsentRequests = @"
            SELECT TOP {0}
                                Id,
                                CompanyCode,
                                SalesforceId,
                                IysCode,
                                BrandCode,
                                convert(varchar, ConsentDate, 120) as ConsentDate,
                                convert(varchar, CreationDate, 120) as CreationDate,
                                [Source],
                                Recipient,
                                RecipientType,
                                Status,
                                [Type],
                                TransactionId,
                                IsSuccess,
                                CreateDate,
                                UpdateDate,
                                LogId,
                                IsProcessed,
                                Error
                        FROM dbo.IysPullConsent (nolock)
                WHERE ISNULL(IsProcessed, 0) = @IsProcessed
                ORDER BY CreateDate ASC;
                ";

        public static string GetPullConsentsByFilter = @"
            SELECT
                                Id,
                                CompanyCode,
                                ConsentDate,
                                Recipient,
                                RecipientType,
                                [Type],
                                Status,
                                IsProcessed
                        FROM dbo.IysPullConsent (nolock)
                WHERE CompanyCode IN @CompanyCodes
                  AND RecipientType = @RecipientType
                  AND CreateDate >= @StartDate
                ORDER BY CreateDate DESC;
                ";

        public static string UpdatePullConsentStatuses = @"
            UPDATE dbo.IysPullConsent
            SET Status = @Status,
                UpdateDate = GETDATE()
            WHERE CompanyCode = @CompanyCode
              AND RecipientType = @RecipientType
              AND [Type] = @Type
              AND Recipient IN @Recipients;
            ";

        public static string UpdateSfConsentRequest = @"
            UPDATE dbo.IysPullConsent
            SET IsSuccess = @IsSuccess,
                LogId = @LogId,
                UpdateDate = GETDATE(),
				Error = @Error,
                IsProcessed = 1
            WHERE Id = @Id;";

        public static string GetIYSConsentRequestErrors = @"
            SELECT 
				Id, 
				CompanyCode, 
				SalesforceId, 
				IysCode, 
				BrandCode, 
				convert(varchar, ConsentDate, 120) as ConsentDate, 
				convert(varchar, CreationDate, 120) as CreationDate, 
				[Source], 
				Recipient, 
				RecipientType, 
				Status, 
				[Type], 
				TransactionId, 
				IsSuccess, 
				UpdateDate, 
				LogId, 
				IsProcessed, 
				BatchError,
			    convert(varchar, CreateDate, 120) as CreateDate
			FROM dbo.IYSConsentRequest (nolock)
                WHERE IsSuccess = 0
                AND CreateDate >= CAST(@CreateDate AS datetime)
                AND CreateDate <  DATEADD(DAY, 1, CAST(@CreateDate AS datetime))
                ORDER BY CreateDate ASC;
		";

    }
}
