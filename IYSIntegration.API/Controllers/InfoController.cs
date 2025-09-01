using IYSIntegration.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/{iysCode}")]
    public class InfoController : ControllerBase
    {
        private readonly IInfoService _InfoService;
        public InfoController(IInfoService InfoService)
        {
            _InfoService = InfoService;
        }

        [Route("town")]
        [HttpGet]
        public async Task<IActionResult> GetTowns(int iysCode)
        {
            var result = await _InfoService.GetTowns(iysCode);
            return Ok(result);
        }

        [Route("town/{code}")]
        [HttpGet]
        public async Task<IActionResult> GetTown(int iysCode, string code)
        {
            var result = await _InfoService.GetTown(iysCode, code);
            return Ok(result);
        }


        [Route("cities")]
        [HttpGet]
        public async Task<IActionResult> GetCities(int iysCode)
        {
            var result = await _InfoService.GetCities(iysCode);
            return Ok(result);
        }

        [Route("cities/{code}")]
        [HttpGet]
        public async Task<IActionResult> GetCity(int iysCode, string code)
        {
            var result = await _InfoService.GetCity(iysCode, code);
            return Ok(result);
        }
    }
}
