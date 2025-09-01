using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Retailer;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/{iysCode}/brands/{brandCode}")]
    public class RetailerController : ControllerBase
    {
        private readonly IRetailerService _retailerService;
        public RetailerController(IRetailerService retailerService)
        {
            _retailerService = retailerService;
        }

        [Route("retailers")]
        [HttpPost]
        public async Task<IActionResult> AddRetailer(int iysCode, int brandCode, [FromBody] Common.Base.Retailer retailer)
        {
            var request = new AddRetailerRequest { IysCode = iysCode, BrandCode = brandCode, Retailer = retailer };
            var result = await _retailerService.AddRetailer(request);
            return Ok(result);
        }

        [Route("retailers/{retailerCode}")]
        [HttpGet]
        public async Task<IActionResult> GetAllRetailers(int iysCode, int brandCode, int retailerCode)
        {
            var request = new GetRetailerRequest { IysCode = iysCode, BrandCode = brandCode, RetailerCode = retailerCode };
            var result = await _retailerService.GetRetailer(request);
            return Ok(result);
        }

        [Route("retailers/{retailerCode}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteRetailer(int iysCode, int brandCode, int retailerCode)
        {
            var request = new DeleteRetailerRequest { IysCode = iysCode, BrandCode = brandCode, RetailerCode = retailerCode };
            var result = await _retailerService.DeleteRetailer(request);
            return Ok(result);
        }

        [Route("retailers")]
        [HttpGet]
        public async Task<IActionResult> GetAllRetailers(int iysCode, int brandCode, int? offset, int? limit)
        {
            var request = new GetAllRetailersRequest { IysCode = iysCode, BrandCode = brandCode, Offset = offset, Limit = limit };
            var result = await _retailerService.GetAllRetailers(request);
            return Ok(result);
        }
    }
}
