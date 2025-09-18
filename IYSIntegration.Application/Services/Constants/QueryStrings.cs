namespace IYSIntegration.Application.Services.Constants
{
    public partial class QueryStrings
    {
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

        public static string CheckConsentRequest = @"
        IF @Status IN ('RED', 'RET')
            BEGIN
                IF EXISTS (SELECT 1 
                           FROM SfdcMasterData.dbo.IysPullConsent WITH (NOLOCK) 
                           WHERE Recipient = @Recipient)
                   OR EXISTS (SELECT 1 
                              FROM SfdcMasterData.dbo.IYSConsentRequest WITH (NOLOCK) 
                              WHERE Recipient = @Recipient 
                                AND IsProcessed = 1 
                                AND IsOverDue != 1)
                    SELECT 1;
                ELSE
                    SELECT 0;
            END
            ELSE
                SELECT 1;";

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


        public static string GetMaxBatchId = @"
            SELECT MAX(ISNULL(BatchId, 0)) FROM IYSConsentRequest (nolock)
                    WHERE ISNULL(BatchId, 0) <> 0 ";

        public static string GetLastConsentRequest = @"
            SELECT TOP 1
                Id,
                IysCode,
                BrandCode,
                ConsentDate,
                Source,
                Recipient,
                RecipientType,
                Status,
                Type
            FROM SfdcMasterData.dbo.IYSConsentRequest (NOLOCK)
            WHERE CompanyCode = @CompanyCode AND Recipient = @Recipient AND IsProcessed = 1
            ORDER BY CreateDate DESC";

        public static string GetLastConsentDate = @"
            SELECT TOP 1 ConsentDate
            FROM (
                SELECT ConsentDate
                FROM SfdcMasterData.dbo.IYSConsentRequest (NOLOCK)
                WHERE CompanyCode = @CompanyCode AND Recipient = @Recipient AND IsProcessed = 1
                UNION ALL
                SELECT ConsentDate
                FROM SfdcMasterData.dbo.IysPullConsent (NOLOCK)
                WHERE CompanyCode = @CompanyCode AND Recipient = @Recipient
            ) AS Consents
            ORDER BY ConsentDate DESC";

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

        public static string GetLastConsents = @"
            WITH FilteredConsents AS (
                SELECT CompanyCode, Recipient, RecipientType, Status, ConsentDate
                FROM SfdcMasterData.dbo.IYSConsentRequest (NOLOCK)
                WHERE IsProcessed = 1
                  AND CompanyCode = @CompanyCode
                  AND Recipient IN @Recipients
                UNION ALL
                SELECT CompanyCode, Recipient, RecipientType, Status, ConsentDate
                FROM SfdcMasterData.dbo.IysPullConsent (NOLOCK)
                WHERE CompanyCode = @CompanyCode
                  AND Recipient IN @Recipients
            )
            SELECT CompanyCode, Recipient, RecipientType, Status, convert(varchar, ConsentDate, 20) as ConsentDate
            FROM (
                SELECT CompanyCode, Recipient, RecipientType, Status, ConsentDate,
                       ROW_NUMBER() OVER(PARTITION BY CompanyCode, Recipient, ISNULL(RecipientType, '') ORDER BY ConsentDate DESC) AS RN
                FROM FilteredConsents
            ) AS Ranked
            WHERE RN = 1;";

        public static string InsertConsentRequestWitBatch = @"
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
                IsProcessed,
                BatchId,
                [Index],
                LogId,
                IsSuccess,
                BatchError
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
                1,
                @BatchId,
                @Index,
                @LogId,
                @IsSuccess,
                @BatchError
                );
            SELECT SCOPE_IDENTITY()
            ";

        public static string InsertMultipleConsentQuery = @"
            INSERT INTO IYSMultipleConsentQuery(IysCode, BrandCode, BatchId, RequestId, CreateDate, QueryDate)
            VALUES(@IysCode, @BrandCode, @BatchId, @RequestId, GETDATE(), DATEADD(SECOND, {0}, GETDATE()))";

        public static string GetConsentRequests = @"
            SELECT TOP {0}
                    CompanyCode,
                    Id,
                    IysCode,
                    BrandCode,
                    convert(varchar, ConsentDate,20) as ConsentDate,
                    [Source],
                    Recipient,
                    RecipientType,
                    Status,
                Type,
                                ISNULL(BatchId,0) AS BatchId,
                                ISNULL([Index], 0) AS [Index]
                        FROM dbo.IYSConsentRequest (nolock)
            WHERE
                                ISNULL(IsProcessed, 0) = @IsProcessed
                                AND IsOverdue IS NULL
                                AND ISNULL(IsPulled, 0) = 1
            ORDER BY
                                Id DESC;
           ";

        public static string GetPendingConsentsWithoutPull = @"
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
              AND ISNULL(cr.IsPulled, 1) = 0
            ORDER BY cr.Id DESC;
            ";

        public static string MarkConsentsAsNotPulled = @"
            UPDATE dbo.IYSConsentRequest
            SET IsPulled = 0,
                UpdateDate = GETDATE()
            WHERE Id IN @Ids;
            ";

        public static string MarkConsentsAsPulled = @"
            UPDATE dbo.IYSConsentRequest
            SET IsPulled = 1,
                UpdateDate = GETDATE()
            WHERE Id IN @Ids;
            ";

        public static string MarkConsentsOverdue = @"
            UPDATE dbo.IYSConsentRequest
            SET IsOverdue = 1,
                UpdateDate = GETDATE()
            WHERE ISNULL(IsProcessed, 0) = 0
              AND ISNULL(IsOverdue, 0) = 0
              AND CreateDate < DATEADD(DAY, -@MaxAgeInDays, GETDATE());
            ";

        public static string MarkDuplicateConsentsOverdue = @"
            WITH DuplicateRows AS (
                SELECT Id
                FROM (
                    SELECT Id,
                           ROW_NUMBER() OVER(
                                PARTITION BY CompanyCode, Recipient, ISNULL(RecipientType, ''), ISNULL(Type, ''), ISNULL(Status, '')
                                ORDER BY CreateDate DESC, Id DESC) AS RN
                    FROM dbo.IYSConsentRequest WITH (NOLOCK)
                    WHERE ISNULL(IsProcessed, 0) = 0
                      AND ISNULL(IsOverdue, 0) = 0
                ) Ranked
                WHERE RN > 1
            )
            UPDATE target
            SET IsOverdue = 1,
                UpdateDate = GETDATE()
            FROM dbo.IYSConsentRequest AS target
            INNER JOIN DuplicateRows AS dup ON target.Id = dup.Id;
            ";

        public static string MarkDuplicateConsentsOverdueForRecipient = @"
            WITH DuplicateRows AS (
                SELECT Id
                FROM (
                    SELECT Id,
                           ROW_NUMBER() OVER(
                                PARTITION BY CompanyCode, Recipient, ISNULL(RecipientType, ''), ISNULL(Type, ''), ISNULL(Status, '')
                                ORDER BY CreateDate DESC, Id DESC) AS RN
                    FROM dbo.IYSConsentRequest WITH (NOLOCK)
                    WHERE ISNULL(IsProcessed, 0) = 0
                      AND ISNULL(IsOverdue, 0) = 0
                      AND CompanyCode = @CompanyCode
                      AND Recipient = @Recipient
                      AND ISNULL(RecipientType, '') = ISNULL(@RecipientType, '')
                      AND ISNULL(Type, '') = ISNULL(@Type, '')
                      AND ISNULL(Status, '') = ISNULL(@Status, '')
                ) Ranked
                WHERE RN > 1
            )
            UPDATE target
            SET IsOverdue = 1,
                UpdateDate = GETDATE()
            FROM dbo.IYSConsentRequest AS target
            INNER JOIN DuplicateRows AS dup ON target.Id = dup.Id;
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

        public static string UpdateBatchId = @"
			DECLARE @MaxBatchId INT
			SELECT @MaxBatchId = MAX(BatchId) from IysConsentRequest (NOLOCK)
			SELECT @MaxBatchId = ISNULL(@MaxBatchId, 0) + 1

			;With CTE AS
			(
			SELECT TOP 100 PERCENT Id,
			ROW_NUMBER() OVER (ORDER BY Id ASC) AS RowNumber
			FROM IysConsentRequest (NOLOCK)
				WHERE CompanyCode  = '{0}'
				AND IsProcessed = 0
				ORDER BY Id ASC
			)
			UPDATE IYS  
			SET IYS.[Index] = ((CTE.RowNumber -1) % {1}) + 1,
			IYS.BatchId = CEILING((CTE.RowNumber -1 )/{1}) + @MaxBatchId
			FROM IysConsentRequest IYS
			INNER JOIN CTE ON IYS.[Id] = CTE.[Id]

		";

        public static string GetBatchSummary = @"
            SELECT TOP {0} BatchId, count(*) as TotalCount, MAX(IysCode) as IysCode, MAX(BrandCode) as BrandCode from IYSConsentRequest (NOLOCK)
			WHERE ISNULL(IsProcessed, 0) = 0
			GROUP BY BatchId
			ORDER BY BatchId ASC
			;";

        public static string GetBatchConsentRequests = @"
            SELECT 
	            Id,
	            IysCode,
	            BrandCode,
	            convert(varchar, ConsentDate,20) as ConsentDate,
	            [Source],
	            Recipient,
	            RecipientType,
	            Status,
                Type,
				ISNULL(BatchId,0) AS BatchId,
				ISNULL([Index], 0) AS [Index]
			FROM dbo.IYSConsentRequest (nolock)
                WHERE BatchId = @BatchId
				AND ISNULL(IsProcessed, 0) = 0
                ORDER BY [Index] ASC;
           ";

        public static string UpdateBatchConsentRequests = @"
            UPDATE dbo.IYSConsentRequest
            SET LogId = @LogId,    
                UpdateDate = GETDATE(),
                IsProcessed = 1
            WHERE BatchId = @BatchId;

            INSERT INTO IYSMultipleConsentQuery(IysCode, BrandCode, BatchId, RequestId, CreateDate, QueryDate)
			VALUES(@IysCode, @BrandCode, @BatchId, @RequestId, GETDATE(), DATEADD(SECOND, {0}, GETDATE()))";


        public static string GetUnprocessedMultipleConsenBatches = @"
            SELECT TOP {0} IysCode, BrandCode, BatchId, RequestId FROM IYSMultipleConsentQuery (NOLOCK)
			WHERE QueryDate < GETDATE() AND ISNULL(IsProcessed, 0) = 0";


        public static string UpdateMultipleConsentQueryDate = @"
			UPDATE IYSMultipleConsentQuery 
			SET LogId = @LogId,
			IsProcessed = 1,
			UpdateDate = GETDATE()
			WHERE BatchId = @BatchId;

            UPDATE dbo.IYSConsentRequest
            SET IsSuccess = 1,
                UpdateDate = GETDATE()
            WHERE BatchId = @BatchId
            AND IsProcessed = 1 AND IsSuccess IS NULL";

        public static string UpdateMultipleConsentItem = @"
			IF @IsQueryResult = 1
				UPDATE dbo.IYSConsentRequest
				SET IsSuccess = @IsSuccess,
					UpdateDate = GETDATE(),
					IsProcessed = 1,
					BatchError = @BatchError
				WHERE BatchId = @BatchId AND [Index] = @Index
			ELSE
				UPDATE dbo.IYSConsentRequest
				SET IsSuccess = @IsSuccess,
					UpdateDate = GETDATE(),
					IsProcessed = 1,
					BatchError = @BatchError,
					LogId = @LogId
				WHERE BatchId = @BatchId AND [Index] = @Index
			";


        public static string ReorderBatch = @"
			DECLARE @BatchId INT  
				
			SELECT @BatchId = MAX(BatchId) FROM IYSConsentRequest (nolock)
			WHERE ISNULL(BatchId, 0) <> 0 
	
			PRINT '@BatchId: ' + CAST(@BatchId AS VARCHAR)
				
			;With CTE AS
			(
				SELECT TOP (100) PERCENT Id,
				ROW_NUMBER() OVER (ORDER BY Id ASC) AS RowNumber
				FROM IYSConsentRequest (NOLOCK)
					WHERE BatchId = @OldBatchId
					AND ISNULL(IsProcessed, 0) = 0
					ORDER BY Id ASC
			)
	
			UPDATE IYS  
			SET IYS.[Index] = CTE.RowNumber,
			IYS.BatchId = ISNULL(@BatchId, 0) + 1
			FROM IYSConsentRequest IYS
			INNER JOIN CTE ON IYS.[Id] = CTE.[Id]";

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
