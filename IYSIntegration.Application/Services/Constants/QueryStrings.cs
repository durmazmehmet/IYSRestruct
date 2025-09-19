namespace IYSIntegration.Application.Services.Constants
{
    public partial class QueryStrings
    {
        public static string InsertTokenLog = @"
            INSERT INTO SfdcMasterData.dbo.IYSTokenLog
                (
                    CompanyCode,
                    AccessTokenMasked,
                    RefreshTokenMasked,
                    TokenCreateDateUtc,
                    TokenRefreshDateUtc,
                    Operation,
                    ServerIdentifier,
                    CreatedAtUtc
                )
            VALUES(
                @CompanyCode,
                @AccessTokenMasked,
                @RefreshTokenMasked,
                @TokenCreateDateUtc,
                @TokenRefreshDateUtc,
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
            SET IsSuccess = @IsSuccess,
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

        public static string UpdateConsentRequest = @"
			IF EXISTS (SELECT * FROM dbo.IYSConsentRequest (nolock) WHERE Id = @Id)
			BEGIN
				UPDATE dbo.IYSConsentRequest
				SET IsSuccess = @IsSuccess,
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
                AND CreateDate between (@CreateDate -1) AND @CreateDate
                ORDER BY CreateDate ASC;
		";

    }
}
