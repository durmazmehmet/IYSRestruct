using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Brand;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/{iysCode}")]
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;
        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        [Route("brands")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int iysCode)
        {
            var result = await _brandService.GetBrands(new GetBrandRequest { IysCode = iysCode });
            return Ok(result);
        }
    }
}
