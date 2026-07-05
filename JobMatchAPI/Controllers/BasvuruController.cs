using Microsoft.AspNetCore.Mvc;
using JobMatchAPI.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobMatchAPI.Controllers
{
    [ApiController]
    [Route("api/Basvuru")]
    public class BasvuruController : ControllerBase
    {
        private readonly VeriTabaniBaglantisi _veriTabani;

        public BasvuruController(VeriTabaniBaglantisi veriTabani)
        {
            _veriTabani = veriTabani;
        }

        [HttpGet("listele")]
        public async Task<IActionResult> Listele()
        {
            try
            {
                if (_veriTabani.Basvurular == null) return Ok(new List<object>());
                var basvurular = await _veriTabani.Basvurular.ToListAsync();
                return Ok(basvurular);
            }
            catch (Exception ex) { return StatusCode(500, $"Hata: {ex.Message}"); }
        }

        [HttpGet("ogrenci/{kullaniciId}")]
        public async Task<IActionResult> GetOgrenciBasvurulari(int kullaniciId)
        {
            try
            {
                if (_veriTabani.Basvurular == null || _veriTabani.Ilanlar == null) return Ok(new List<object>());

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
                                                IsverenEposta = isv != null ? isv.Eposta : "İletişim bilgisi yok",
                                                AiSkoru = b.AiSkoru,
                                                AiGeriBildirim = b.AiGeriBildirim
                                            }).ToListAsync();

                return Ok(basvuruListesi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Başvuru listeleme backend hatası: {ex.Message}");
            }
        }

        [HttpPost("durum-degistir/{basvuruId}/{yeniDurum}")]
        public async Task<IActionResult> DurumDegistir(int basvuruId, string yeniDurum)
        {
            try
            {
                if (_veriTabani.Basvurular == null) return BadRequest("Veritabanı tablosuna erişilemiyor.");

                var basvuru = await _veriTabani.Basvurular.FindAsync(basvuruId);
                if (basvuru == null) return NotFound("İlgili başvuru kaydı bulunamadı.");

                basvuru.Durum = yeniDurum;
                await _veriTabani.SaveChangesAsync();

                return Ok(new { mesaj = $"Başvuru durumu başarıyla '{yeniDurum}' yapıldı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Durum güncelleme hatası: {ex.Message}");
            }
        }
    }
}