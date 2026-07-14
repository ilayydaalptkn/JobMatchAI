using JobMatchAPI.Data;
using JobMatchAPI.Helpers;
using JobMatchAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Aday")]
    [Authorize(Roles = "Isveren")]
    public class AdayController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public AdayController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        [HttpGet("havuz")]
        public async Task<IActionResult> AdayHavuzu([FromQuery] string? yetenek, [FromQuery] string? sehir)
        {
            var adaylar = await _veriTabani.Kullanicilar
                .Where(k => k.Rol == "Ogrenci" && k.CvHedefIs != "")
                .ToListAsync();

            var sonuc = adaylar
                .Select(k => new
                {
                    k.Id,
                    k.AdSoyad,
                    k.Eposta,
                    k.Telefon,
                    k.CvOkul,
                    k.CvHedefIs,
                    k.CvSehir,
                    CvYetenekler = AiMatcherService.CvJsonAyristir(k.CvYetenekler),
                    CvTecrubeler = AiMatcherService.CvJsonAyristir(k.CvTecrubeler)
                })
                .Where(a =>
                {
                    var yeteneklerStr = string.Join(" ", a.CvYetenekler).ToLower();
                    var sehirStr = (a.CvSehir ?? "").ToLower();
                    var yetenekUyum = string.IsNullOrWhiteSpace(yetenek) || yeteneklerStr.Contains(yetenek.ToLower());
                    var sehirUyum = string.IsNullOrWhiteSpace(sehir) || sehirStr.Contains(sehir.ToLower());
                    return yetenekUyum && sehirUyum;
                })
                .ToList();

            return Ok(sonuc);
        }
    }
}
