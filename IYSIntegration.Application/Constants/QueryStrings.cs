namespace IYSIntegration.Application.Constants
{
    public class QueryStrings
    {
        public static string GetConsentRequests = @"
            SELECT TOP {0} 
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
            ORDER BY 
				Id DESC;
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
