using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.RetailerAccess;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/{iysCode}/brands/{brandCode}")]
    public class RetailerAccessController : ControllerBase
    {
        private readonly IRetailerAccessService _retailerAccessService;
        public RetailerAccessController(IRetailerAccessService retailerAccessService)
        {
            _retailerAccessService = retailerAccessService;
        }

        [Route("consents/retailers/access")]
        [HttpPost]
        public async Task<IActionResult> AddRetailerAccess(int iysCode, int brandCode, [FromBody] Common.Base.RetailerRecipientAccess retailerRecipientAccess)
        {
            var request = new AddRetailerAccessRequest { IysCode = iysCode, BrandCode = brandCode, RetailerRecipientAccess = retailerRecipientAccess };
            var result = await _retailerAccessService.AddRetailerAccess(request);
            return Ok(result);
        }

        [Route("retailers/access/list")]
        [HttpPost]
        public async Task<IActionResult> QueryRetailerAccess(int iysCode, int brandCode, [FromBody] Common.Base.RecipientKey recipientKey, int? offset, int? limit)
        {
            var request = new QueryRetailerAccessRequest { IysCode = iysCode, BrandCode = brandCode, Offset = offset, Limit = limit, RecipientKey = recipientKey };
            var result = await _retailerAccessService.QueryRetailerAccess(request);
            return Ok(result);
        }

        [Route("consents/retailers/access/remove")]
        [HttpPost]
        public async Task<IActionResult> DeleteRetailerAccess(int iysCode, int brandCode, [FromBody] Common.Base.RetailerRecipientAccess retailerRecipientAccess)
        {
            var request = new DeleteRetailerAccessRequest { IysCode = iysCode, BrandCode = brandCode, RetailerRecipientAccess = retailerRecipientAccess };
            var result = await _retailerAccessService.DeleteRetailerAccess(request);
            return Ok(result);
        }

        [Route("consents/retailers/access")]
        [HttpPut]
        public async Task<IActionResult> UpdateRetailerAccess(int iysCode, int brandCode, [FromBody] Common.Base.RetailerRecipientAccess retailerRecipientAccess)
        {
            var request = new UpdateRetailerAccessRequest { IysCode = iysCode, BrandCode = brandCode, RetailerRecipientAccess = retailerRecipientAccess };
            var result = await _retailerAccessService.UpdateRetailerAccess(request);
            return Ok(result);
        }

        [Route("consents/retailers/access/remove/all")]
        [HttpPost]
        public async Task<IActionResult> DeleteAllRetailersAccess(int iysCode, int brandCode, [FromBody] Common.Base.RetailerRecipientAccess retailerRecipientAccess)
        {
            var request = new DeleteAllRetailersAccessRequest { IysCode = iysCode, BrandCode = brandCode, RetailerRecipientAccess = retailerRecipientAccess };
            var result = await _retailerAccessService.DeleteAllRetailersAccess(request);
            return Ok(result);
        }
    }
}
