use SfDcMasterData

SELECT   
'BOI' AS CompanyCode, -- BOP, BOPK 
Id as SalesforceId, 
663585 as IysCode, 
622055 AS BrandCode, 
CreatedOn as  ConsentDate, 
'HS_CAGRI_MERKEZI' as [Source], 
PCEmail as Recipient, 
'BIREYSEL' AS RecipientType, 
'ONAY' as Status, 
'EPOSTA' as [Type], 
GETDATE() CreateDate
from MasterDataCustomer mdc
where ISNULL(PCEmail, '') <> ''
and SfId between 80000 and 100000
UNION 
SELECT 
'BOI' AS CompanyCode, -- BOP, BOPK 
Id as SalesforceId, 
663585 as IysCode, 
622055 AS BrandCode, 
CreatedOn as  ConsentDate, 
'HS_CAGRI_MERKEZI' as [Source], 
'+90' + Gsm as Recipient, 
'BIREYSEL' AS RecipientType, 
'ONAY' as Status, 
'MESAJ' as [Type], 
GETDATE() CreateDate
from MasterDataCustomer mdc
where ISNULL(Gsm, '') <> ''
and SfId between 100000 and 120000

