# 🚀 JobMatch AI — Staj & Aday Eşleştirme Platformu

JobMatch AI, öğrenci özgeçmişleri (CV) ile işveren staj ilanlarını **gelişmiş semantik analiz algoritması** kullanarak dinamik olarak eşleştiren, gerçek zamanlı bildirim ve aday havuzu yönetimi sunan web tabanlı bir platformdur.

---

## ✨ Öne Çıkan Özellikler (Core Features)

* **🧠 Gelişmiş AI Eşleştirme Motoru:** Kelime köklerini analiz ederek yetenek, konum ve pozisyon bazlı %0-%100 arası akıllı uyum skoru hesaplar.
* **🔔 Canlı Takip & Bildirim Merkezi:** İşveren başvuru durumunu (Onay/Red) güncellediği an öğrenci ekranında mikro-animasyonlu canlı popup bildirimleri tetiklenir.
* **👥 Global Yetenek Havuzu (Talent Pool):** İşverenlerin sistemdeki tüm adayları yeteneklerine ve şehirlerine göre anlık (event-driven) filtreleyebileceği gelişmiş arama modülü.
* **🎨 Premium UI/UX Tasarımı:** Tailwind CSS ile güçlendirilmiş, `hover:scale` mikro etkileşimleri ve modern katman efektleri içeren arayüz.

---

## 🏗️ Kullanılan Teknolojiler (Tech Stack)

### **Backend & Veri Katmanı**
* **C# / .NET Core Web API:** Kurumsal ve ölçeklenebilir backend mimarisi.
* **Entity Framework Core (EF Core):** Veritabanı erişimi ve ORM yönetimi.
* **SQL Server:** İlişkisel veritabanı şeması ve Referential Integrity kontrolü.

### **Frontend & Tasarım**
* **HTML5 / JavaScript (ES6+):** Asenkron durum yönetimi ve event-driven arayüz.
* **Tailwind CSS:** Premium ve mikro-animasyonlu responsive arayüz tasarımı.

---

## 🔬 Mimari Yaklaşımlar ve Optimizasyonlar

Projenin geliştirilme sürecinde veri erişim katmanında (Data Access) şu kritik mühendislik çözümleri uygulanmıştır:
1.  **Eager Loading Stratejisi:** SQL tarafında `N+1 sorgu problemini` engellemek amacıyla `.Include(b => b.Kullanici)` kullanılarak veriler tek bir `INNER JOIN` ile asenkron çekilmiştir.
2.  **Circular Reference Engellenmesi:** Nesnelerin serileştirilmesi (Serialization) esnasında Swagger'ı kilitleyen sonsuz döngü problemi `[JsonIgnore]` özniteliği ile çözülmüştür.
3.  **İleri Düzey Hata Yönetimi:** Veritabanı kısıtlama ihlallerini izole etmek için loglama altyapısına `ex.InnerException` katmanı eklenmiştir.

---

## 🚀 Kurulum ve Çalıştırma (Installation)

1. Projeyi klonlayın:
   ```bash
   git clone [https://github.com/ilayydaalptkn/JobMatchAI.git](https://github.com/ilayydaalptkn/JobMatchAI.git)
