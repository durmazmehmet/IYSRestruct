namespace IYSIntegration.Application.Constanst
{
    public class QueryStrings
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
            SELECT SCOPE_IDENTITY()
            ";

        public static string UpdateConsentRequest = @"
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
                ISNULL(cl.Id, 0) as LogId,
                ISNULL(cr.IsProcessed,0) as IsProcessed,
                cr.IsSuccess, 
                cl.Response,
                convert(varchar, cl.UpdateDate, 20) as SendDate
            FROM IYSConsentRequest cr(NOLOCK)
            LEFT JOIN IYSCallLog cl (nolock) ON (cr.LogId = cl.Id)
            WHERE cr.Id = @Id";

        public static string GetMaxBatchId = @"
            SELECT MAX(ISNULL(BatchId, 0)) FROM IYSConsentRequest (nolock)
		    WHERE ISNULL(BatchId, 0) <> 0 ";

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
    }
}
