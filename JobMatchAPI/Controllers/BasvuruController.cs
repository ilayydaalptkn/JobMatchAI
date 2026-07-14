using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobMatchAPI.Data;
using JobMatchAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Basvuru")]
    [Authorize]
    public class BasvuruController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public BasvuruController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        [HttpGet("ogrenci/{kullaniciId}")]
        [Authorize(Roles = "Ogrenci")]
        public async Task<IActionResult> GetOgrenciBasvurulari(int kullaniciId)
        {
            if (User.KullaniciIdAl() != kullaniciId)
                return Forbid();

            var basvuruListesi = await (from b in _veriTabani.Basvurular
                                        join i in _veriTabani.Ilanlar on b.IlanId equals i.Id
                                        join isveren in _veriTabani.Kullanicilar on i.KullaniciId equals isveren.Id into isverenGrup
                                        from isv in isverenGrup.DefaultIfEmpty()
                                        where b.KullaniciId == kullaniciId
                                        orderby b.Id descending
                                        select new
                                        {
                                            Id = b.Id,
                                            KullaniciId = b.KullaniciId,
                                            IlanId = b.IlanId,
                                            BasvuruTarihi = b.BasvuruTarihi,
                                            Durum = b.Durum,
                                            IlanBaslik = i.Baslik,
                                            SirketAdi = i.SirketAdi,
                                            Sehir = i.Sehir,
                                            IsverenEposta = isv != null ? isv.Eposta : "İletişim bilgisi yok",
                                            AiSkoru = b.AiSkoru,
                                            AiGeriBildirim = b.AiGeriBildirim
                                        }).ToListAsync();

            return Ok(basvuruListesi);
        }

        [HttpPost("durum-degistir/{basvuruId}/{yeniDurum}")]
        [Authorize(Roles = "Isveren")]
        public async Task<IActionResult> DurumDegistir(int basvuruId, string yeniDurum)
        {
            var basvuru = await _veriTabani.Basvurular.FindAsync(basvuruId);
            if (basvuru == null) return NotFound("Başvuru bulunamadı.");

            var ilan = await _veriTabani.Ilanlar.FindAsync(basvuru.IlanId);
            if (ilan?.KullaniciId != User.KullaniciIdAl())
                return Forbid();

            if (yeniDurum is not ("Onaylandi" or "Reddedildi" or "Beklemede"))
                return BadRequest("Geçersiz durum.");

            basvuru.Durum = yeniDurum;
            await _veriTabani.SaveChangesAsync();

            return Ok(new { mesaj = $"Başvuru durumu '{yeniDurum}' olarak güncellendi." });
        }
    }
}
