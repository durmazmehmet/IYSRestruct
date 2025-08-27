USE [SfdcMasterData]
GO
/****** Object:  StoredProcedure [dbo].[CreateConsentRequests_BOP]    Script Date: 8/20/2020 1:19:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[CreateConsentRequests_BOP]
AS BEGIN 
	
	DECLARE @BOIPermission varchar(200),
			@BOPPermission varchar(200),
			@BOIIysCode NUMERIC(8) = 663585,
			@BOIBrandCode NUMERIC(8) = 622055,
			@BOPIysCode NUMERIC(8) = 657079,
			@BOPBrandCode NUMERIC(8) = 603020,
			@BOPKIysCode NUMERIC(8) = 674971,
			@BOPKBrandCode NUMERIC(8) = 604650;

	DECLARE @Id varchar(100) ,
	@IYSType__c varchar(100) ,
	@PersonEmail varchar(100) ,
	@PersonMobilePhone varchar(100) ,
	@BMW_Campaign_Permissions__pc varchar(100) ,
	@BMW_MC_Campaign_Permissions__pc varchar(100) ,
	@MINI_Campaign_Permissions__pc varchar(100) ,
	@BOIETKSource__c varchar(100) ,
	@BOIETKDate__c smalldatetime,
	@Jaguar_Campaign_Permissions__pc varchar(100) ,
	@Land_Rover_Campaign_Permissions__pc varchar(100) ,
	@BOPETKSource__c varchar(100) ,
	@BOPETKDate__c smalldatetime,
	@Premium_Kiralama_Campaign_Permissions__pc varchar(100) ,
	@BOPKETKDate__c smalldatetime,
	@BOPKETKSource__c varchar(100);

DECLARE cur_consent_request CURSOR FOR
	SELECT
	Id,
	IYSType__c,
	PersonEmail,
	PersonMobilePhone,
	BMW_Campaign_Permissions__pc,
	BMW_MC_Campaign_Permissions__pc,
	MINI_Campaign_Permissions__pc,
	BOIETKSource__c,
	BOIETKDate__c,
	Jaguar_Campaign_Permissions__pc,
	Land_Rover_Campaign_Permissions__pc,
	BOPETKSource__c,
	BOPETKDate__c,
	Premium_Kiralama_Campaign_Permissions__pc,
	BOPKETKDate__c,
	BOPKETKSource__c
FROM
	SfdcMasterData.dbo.IysSalesforceRawData (NOLOCK)

OPEN cur_consent_request;

FETCH NEXT
FROM
cur_consent_request
INTO
	@Id,
	@IYSType__c,
	@PersonEmail,
	@PersonMobilePhone,
	@BMW_Campaign_Permissions__pc,
	@BMW_MC_Campaign_Permissions__pc,
	@MINI_Campaign_Permissions__pc,
	@BOIETKSource__c,
	@BOIETKDate__c ,
	@Jaguar_Campaign_Permissions__pc,
	@Land_Rover_Campaign_Permissions__pc,
	@BOPETKSource__c,
	@BOPETKDate__c ,
	@Premium_Kiralama_Campaign_Permissions__pc,
	@BOPKETKDate__c ,
	@BOPKETKSource__c 
	
WHILE @@FETCH_STATUS = 0 
BEGIN 
	

	SELECT @BOIPermission = ISNULL(@BMW_Campaign_Permissions__pc, '') + ISNULL(@BMW_MC_Campaign_Permissions__pc, '') + ISNULL(@MINI_Campaign_Permissions__pc, ''),
		   @BOPPermission = ISNULL(@Jaguar_Campaign_Permissions__pc, '') + ISNULL(@Land_Rover_Campaign_Permissions__pc, '')	
	
	-- BOI Permission
	IF CHARINDEX('Email',@BOPPermission) > 0 AND ISNULL(@PersonEmail, '') <> '' 
	BEGIN
		INSERT INTO SfdcMasterData.dbo.IysConsentRequestTemp
			(CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, [Source], Recipient, RecipientType, Status, [Type], CreateDate)
		VALUES('BOP',@Id, @BOPIysCode, @BOPBrandCode, CONVERT(VARCHAR, @BOPETKDate__c, 20),@BOPETKSource__c, @PersonEmail, 'BIREYSEL','ONAY', 'EPOSTA', GETDATE());
		IF @IYSType__c <> 'Birey'	
			INSERT INTO SfdcMasterData.dbo.IysConsentRequestTemp
				(CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, [Source], Recipient, RecipientType, Status, [Type], CreateDate)
			VALUES('BOP',@Id, @BOPIysCode, @BOPBrandCode, CONVERT(VARCHAR, @BOPETKDate__c, 20),@BOPETKSource__c, @PersonEmail, 'TACIR','ONAY', 'EPOSTA', GETDATE());
	END
	
	
	IF ISNULL(@PersonMobilePhone, '') <> ''
	BEGIN
		-- Telefon no doğrulaması yapılacak
		SET @PersonMobilePhone = '+90' + @PersonMobilePhone;

	IF CHARINDEX('Telefon',@BOPPermission) > 0 
		BEGIN
			INSERT INTO SfdcMasterData.dbo.IysConsentRequestTemp
				(CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, [Source], Recipient, RecipientType, Status, [Type], CreateDate)
			VALUES('BOP',@Id, @BOPIysCode, @BOPBrandCode, CONVERT(VARCHAR, @BOPETKDate__c, 20),@BOPETKSource__c, @PersonMobilePhone, 'BIREYSEL','ONAY', 'ARAMA', GETDATE());
			IF @IYSType__c <> 'Birey'	
				INSERT INTO SfdcMasterData.dbo.IysConsentRequestTemp
					(CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, [Source], Recipient, RecipientType, Status, [Type], CreateDate)
				VALUES('BOP',@Id, @BOPIysCode, @BOPBrandCode, CONVERT(VARCHAR, @BOPETKDate__c, 20),@BOPETKSource__c, @PersonMobilePhone, 'TACIR','ONAY', 'ARAMA', GETDATE());
		END
		
		IF CHARINDEX('SMS',@BOPPermission) > 0 
		BEGIN
			INSERT INTO SfdcMasterData.dbo.IysConsentRequestTemp
				(CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, [Source], Recipient, RecipientType, Status, [Type], CreateDate)
			VALUES('BOP',@Id, @BOPIysCode, @BOPBrandCode, CONVERT(VARCHAR, @BOPETKDate__c, 20),@BOPETKSource__c, @PersonMobilePhone, 'BIREYSEL','ONAY', 'MESAJ', GETDATE());
			IF @IYSType__c <> 'Birey'	
				INSERT INTO SfdcMasterData.dbo.IysConsentRequestTemp
					(CompanyCode, SalesforceId, IysCode, BrandCode, ConsentDate, [Source], Recipient, RecipientType, Status, [Type], CreateDate)
				VALUES('BOP',@Id, @BOPIysCode, @BOPBrandCode, CONVERT(VARCHAR, @BOPETKDate__c, 20),@BOPETKSource__c, @PersonMobilePhone, 'TACIR','ONAY', 'MESAJ', GETDATE());
		END
	END

	FETCH NEXT
	FROM
		cur_consent_request
	INTO
		@Id,
		@IYSType__c,
		@PersonEmail,
		@PersonMobilePhone,
		@BMW_Campaign_Permissions__pc,
		@BMW_MC_Campaign_Permissions__pc,
		@MINI_Campaign_Permissions__pc,
		@BOIETKSource__c,
		@BOIETKDate__c ,
		@Jaguar_Campaign_Permissions__pc,
		@Land_Rover_Campaign_Permissions__pc,
		@BOPETKSource__c,
		@BOPETKDate__c ,
		@Premium_Kiralama_Campaign_Permissions__pc,
		@BOPKETKDate__c ,
		@BOPKETKSource__c
END;

CLOSE cur_consent_request;

DEALLOCATE cur_consent_request;
END