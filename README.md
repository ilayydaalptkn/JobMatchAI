# JobMatch AI — Staj & Aday Eşleştirme Platformu

JobMatch AI, öğrenci CV'leri ile işveren staj ilanlarını **kural tabanlı AI eşleştirme motoru** ile analiz eden, gerçek zamanlı bildirim ve aday havuzu yönetimi sunan full-stack bir platformdur.

**Canlı Demo:** [login.html](https://ilayydaalptkn.github.io/JobMatchAI/login.html)

---

## Demo Hesaplar

| Rol | E-posta | Şifre |
|-----|---------|-------|
| Öğrenci | `demo@jobmatch.ai` | `Demo123!` |
| İşveren | `isveren@jobmatch.ai` | `Demo123!` |

> Render ücretsiz planda sunucu uyuyorsa ilk istek 30–60 saniye sürebilir.

---

## Mimari

```
GitHub Pages (Frontend)  →  Render (ASP.NET Core API)  →  Supabase (PostgreSQL)
                              ↕
                         Adzuna / Jobicy API (ilan botu)
```

| Katman | Teknoloji |
|--------|-----------|
| Frontend | HTML5, JavaScript, Tailwind CSS |
| Backend | C# .NET 8 Web API, JWT Auth |
| Veritabanı | PostgreSQL (Supabase), EF Core |
| Deploy | GitHub Pages + Render + Docker |

---

## Özellikler

- JWT tabanlı kimlik doğrulama ve rol yönetimi (Öğrenci / İşveren)
- BCrypt ile şifre hashleme
- CV profil yönetimi (yetenekler, tecrübeler, hedef pozisyon)
- Kural tabanlı AI uyum skoru (%0–100)
- İlan yayınlama, başvuru, onay/red akışı
- İşveren aday havuzu (yetenek ve şehir filtresi)
- Arka plan ilan botu (Adzuna + Jobicy API)
- Canlı başvuru bildirimleri

---

## API Endpoints

### Kimlik (Anonim)
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/Kimlik/kayit` | Yeni kullanıcı kaydı |
| POST | `/api/Kimlik/giris` | Giriş + JWT token |

### CV (JWT gerekli)
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/Cv/profil` | CV profilini getir |
| PUT | `/api/Cv/profil` | CV profilini güncelle |

### İlanlar
| Method | Endpoint | Rol | Açıklama |
|--------|----------|-----|----------|
| GET | `/api/Ilan/listele` | Anonim | Tüm ilanlar |
| GET | `/api/Ilan/listele?kullaniciId=1` | Anonim | AI skorlu ilanlar |
| POST | `/api/Ilan/ekle` | İşveren | İlan oluştur |
| POST | `/api/Ilan/basvuru-yap` | Öğrenci | Başvuru yap |
| GET | `/api/Ilan/basvuranlar/{ilanId}` | İşveren | Başvuranları listele |
| POST | `/api/Ilan/durum-guncelle` | İşveren | Başvuru durumu güncelle |

### Başvurular (JWT gerekli)
| Method | Endpoint | Rol | Açıklama |
|--------|----------|-----|----------|
| GET | `/api/Basvuru/ogrenci/{id}` | Öğrenci | Kendi başvuruları |

### Aday Havuzu (JWT + İşveren)
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/Aday/havuz?yetenek=react&sehir=ankara` | Filtrelenmiş adaylar |

---

## Yerel Kurulum

### Gereksinimler
- .NET 8 SDK
- PostgreSQL (veya Supabase hesabı)

### Adımlar

```bash
git clone https://github.com/ilayydaalptkn/JobMatchAI.git
cd JobMatchAI/JobMatchAPI

# appsettings.Development.json içine connection string ekle
dotnet ef database update
dotnet run
```

API: `http://localhost:5000/swagger`

Frontend: `index.html` ve `login.html` dosyalarını tarayıcıda açın.

---

## Render Deploy

Environment variables ayarlayın (`.env.example` dosyasına bakın):

```
ConnectionStrings__DefaultConnection=<supabase-connection-string>
JWT_KEY=<güçlü-32+-karakter-anahtar>
```

---

## Testler

```bash
dotnet test JobMatchAPI.Tests/JobMatchAPI.Tests.csproj
```

GitHub Actions CI her push'ta otomatik build ve test çalıştırır.

---

## Proje Yapısı

```
JobMatchAI/
├── index.html              # Ana uygulama (öğrenci/işveren paneli)
├── login.html              # Giriş ve kayıt
├── JobMatchAPI/
│   ├── Controllers/        # API endpoint'leri
│   ├── Services/           # AI matcher, JWT, şifre hash
│   ├── Models/             # Veritabanı modelleri
│   ├── Data/               # EF Core DbContext
│   ├── Migrations/         # Veritabanı migration'ları
│   └── ArkaPlanServisleri/ # İlan botu
├── JobMatchAPI.Tests/      # Unit + integration testler
└── .github/workflows/      # CI pipeline
```

---

## Lisans

MIT — Portfolyo ve eğitim amaçlı kullanım için uygundur.
