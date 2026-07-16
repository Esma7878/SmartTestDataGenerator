#  Smart Test Data Generator

Smart Test Data Generator; yazılımcıların, QA/test mühendislerinin ve veritabanı yöneticilerinin ilişkisel test verilerini saniyeler içinde üretmesini, farklı formatlarda dışa aktarmasını ve dinamik REST Mock API'ler oluşturmasını sağlayan profesyonel düzeyde bir web aracıdır.

Proje, **Clean Architecture** prensiplerine uygun olarak, test veritabanı okuma/yazma maliyeti oluşturmadan tamamen bellek (in-memory) üzerinde veri üretecek şekilde tasarlanmıştır.

---

##  Özellikler

*   ** Gelişmiş Dashboard (Panel)**: Chart.js destekli çizgi ve doughnut grafikler ile son 7 günlük veri üretim istatistikleri, en çok tercih edilen dışa aktarma formatları, iğnelenmiş şablonlar ve gerçek zamanlı son aktiviteler akışı.
*   ** Akıllı İlişkisel Veri Üretimi**: Sütunlar arasında yabancı anahtar (**Foreign Key**) ilişkisi kurabilme. Arka planda çalışan **Topolojik Sıralama (Topological Sort)** algoritması sayesinde ilişkisel tablolar döngüsel bağımlılık yaratmadan doğru hiyerarşide üretilir (örn. önce `Müşteriler` üretilir, ardından bu müşterilerin gerçek Id'leri seçilerek `Siparişler` tablosu beslenir).
*   ** Çoklu Dosya Formatı Desteği (Export)**:
    *   **CSV (Ziplenmiş)**: Çoklu tabloların her birini ayrı birer CSV dosyası olarak üretip tek bir zip dosyası içinde sunar.
    *   **Excel (ClosedXML)**: Her tablonun ayrı bir sekme olduğu, otomatik sütun genişlikli ve özel renk tasarımlı tablolar.
    *   **SQL Insert Script**: SQL Server, MySQL, PostgreSQL ve SQLite sözdizimlerine uyumlu kaçış karakterleriyle hazır insert dosyaları.
    *   **XML & JSON**: Standart yapısal çıktılar.
    *   **PDF Raporu (QuestPDF)**: Şablon bilgilerini ve her tablonun ilk 5 satırlık örnek veri tablosunu içeren şık tasarımlı rapor çıktısı.
*   ** Dinamik REST Mock API**: Şablondaki tablolar için otomatik olarak `/api/mock/{templateId}/{tableName}?count=X` uç noktalarını oluşturur. Frontend ve mobil geliştiricilerin veritabanı kurmadan < 50ms sürede JSON verileri tüketmesini sağlar.
*   ** CSV'den Sürükle-Bırak Şema Import**: Sahip olduğunuz fiziksel bir `.csv` dosyasını sürükleyip bırakarak başlık satırlarını okutabilir; sütun adlarından veri tiplerini akıllıca tahmin edip şablonu anında oluşturabilirsiniz.
*   ** Klavye Kısayolları & Hızlı Erişim**: `G + D` (Dashboard), `G + T` (Şablonlar), `G + M` (Mock API), `G + A` (Ayarlar) gibi kısayollarla hızlı geçiş.
*   ** Karanlık ve Aydınlık Tema**: Tarayıcı yerel belleği (`localStorage`) ile entegre, sayfa yenilendiğinde göz kırpmayan akıcı Dark Mode desteği.

---

##  Kullanılan Teknolojiler

*   **Çekirdek**: .NET 8.0, C#, ASP.NET Core MVC
*   **Veri Katmanı**: Entity Framework Core, SQLite (Dosya tabanlı yerel veritabanı)
*   **Veri Üretim Motoru**: Bogus (Sahte veri üretim kütüphanesi)
*   **Dosya Motorları**: ClosedXML (Excel), CsvHelper (CSV), QuestPDF (PDF Raporlama)
*   **Frontend**: Bootstrap 5, Bootstrap Icons, Chart.js, Vanilla Javascript

---

##  Proje Mimarisi (Clean Architecture)

Proje, bağımlılıkların içe doğru akmasını sağlayan ve iş mantığını dış katmanlardan soyutlayan 4 katmanlı temiz bir yapıya sahiptir:

*   **`SmartTestDataGenerator.Core` (Domain)**: İş kuralları, çekirdek varlıklar (Entities), repository arayüzleri ve veri tipi enumları.
*   **`SmartTestDataGenerator.Application` (Business Logic)**: Servis kontratları, DTO'lar, AutoMapper profilleri ve iş mantığı.
*   **`SmartTestDataGenerator.Infrastructure` (Data & External Services)**: DbContext, generic repository implementasyonları, Bogus tabanlı veri üretici servisi ve ClosedXML/QuestPDF dışa aktarım servisleri.
*   **`SmartTestDataGenerator.Web` (Presentation)**: MVC Controller yapıları, Razor Views, static css/js dosyaları, dinamik REST Mock API endpointleri ve ayar yönetim paneli.

---

##  Hızlı Başlangıç

### Gereksinimler
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   Visual Studio 2022 (v17.8+) veya VS Code

### Kurulum Adımları

1.  Projeyi klonlayın veya zip olarak indirin.
2.  Terminali açıp çözüm (solution) dizinine gidin:
    ```bash
    cd SmartTestDataGenerator
    ```
3.  Projeyi derleyin (NuGet paketleri otomatik yüklenir):
    ```bash
    dotnet build
    ```
4.  Web projesini başlatın (Veritabanı ve seed veriler ilk çalışmada otomatik oluşturulur):
    ```bash
    dotnet run --project src/SmartTestDataGenerator.Web
    ```
5.  Tarayıcınızdan konsolda belirtilen adresi açın (Varsayılan: `http://localhost:5258`).

---

##  Lisans

Bu proje **MIT** lisansı altında lisanslanmıştır. Ticari ve kişisel amaçlarla özgürce kullanılabilir ve değiştirilebilir.
