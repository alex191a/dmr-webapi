using DMRWebScrapper_service.Code;
using DMRWebScrapper_service.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DMRWebScrapper_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LicensePlateController : ControllerBase
    {


        private readonly ILogger<LicensePlateController> _logger;

        public LicensePlateController(ILogger<LicensePlateController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/Getinfo")]
        [EnableCors]

        public async Task<IActionResult> GetNew(string nummerplade)
        {
            try
            {
                Bildata? bildata = await DMRProxy.HentOplysninger(nummerplade, DateTime.Now);
                return Ok(bildata);
            }
            catch
            (Exception ex)
            {

                return Problem(ex.Message);
            }
        }
        [HttpGet("/GetHistory")]
        public async Task<IActionResult> GetOld(string nummerplade, DateTime dato)
        {
            try
            {
                Bildata? bildata = await DMRProxy.HentOplysninger(nummerplade, dato);
                return Ok(bildata);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        [HttpGet("/IsPolice")]
        public async Task<IActionResult> GetIsPolice(string nummerplade)
        {
            try
            {
                Bildata? bildata = await DMRProxy.HentOplysninger(nummerplade, DateTime.Now);
                return Ok(bildata.IsPolice);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
