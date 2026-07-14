using JobMatchAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CORS VE GÜVENLİK AYARLARI (RENDER İÇİN EN BAŞA ALINDI) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("HerKeseIzinVer", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BuSizinCokGizliVeGuvenliKriptografikAnahtarinizdir123!"))
    };
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<VeriTabaniBaglantisi>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<JobMatchAPI.ArkaPlanServisleri.TumSitelerBotServisi>();

var app = builder.Build();

// --- 2. MIDDLEWARE SIRALAMASI (HTTP PIPELINE) ---
app.UseStaticFiles();

// 🚨 CRITICAL: CORS mutlaka Authentication ve Routing işlemlerinden önce tetiklenmelidir!
app.UseCors("HerKeseIzinVer");

app.UseSwagger();
app.UseSwaggerUI();

// 🚨 RENDER İÇİN ÖNEMLİ: HTTPS Redirection tamamen kaldırıldı! Çünkü Render trafiği zaten kendisi HTTPS'e zorlar.
// Sunucu içinde tekrar yönlendirme yapmak sonsuz döngüye (Infinite Loop) ve çökmeye yol açar.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- 3. HAZIR VERİ BESLEME SİSTEMİ (DATA SEEDING) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<VeriTabaniBaglantisi>();
        context.Database.Migrate();

        if (context.Ilanlar.Count() <= 2)
        {
            var hazirIlanlar = new List<JobMatchAPI.Models.Ilan>
            {
                new JobMatchAPI.Models.Ilan {
                    Baslik = "Junior .NET Developer",
                    SirketAdi = "E-Ticaret Çözümleri Ltd. (Kariyer.net'ten Alındı)",
                    Sehir = "Ankara",
                    Aciklama = "Aranan Kriterler: C#, ASP.NET Core, Entity Framework, PostgreSQL. Ankara ofisimizde çalışacak yetiştirilmek üzere junior geliştiriciler aranıyor.",
                    Maas = "Asgari Ücret + Yol",
                    YayinlanmaTarihi = DateTime.UtcNow
                },
                new JobMatchAPI.Models.Ilan {
                    Baslik = "React Front-End Engineer",
                    SirketAdi = "Global Yazılım Teknolojileri (LinkedIn'den Alındı)",
                    Sehir = "İstanbul",
                    Aciklama = "Aranan Kriterler: React.js, Tailwind CSS, TypeScript, Redux. Modern arayüz mimarilerine hakim, kullanıcı deneyimine önem veren takım arkadaşı.",
                    Maas = "Dolgun Ücret",
                    YayinlanmaTarihi = DateTime.UtcNow
                }
            };

            foreach (var ilan in hazirIlanlar)
            {
                bool varMi = context.Ilanlar.Any(i => i.Baslik.ToLower() == ilan.Baslik.ToLower() && i.SirketAdi.ToLower() == ilan.SirketAdi.ToLower());
                if (!varMi) context.Ilanlar.Add(ilan);
            }

            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SİSTEM HATASI]: Veri enjeksiyonunda hata: {ex.Message}");
    }
}

app.Run();