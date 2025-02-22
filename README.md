# Eş Zamanlı Sipariş Yönetimi ve Stok Güncelleme Sistemi

Bu proje, eş zamanlı sipariş yönetimi ve stok güncellemelerini gerçekleştiren bir sistem tasarlamak amacıyla geliştirilmiştir. Multithreading ve senkronizasyon mekanizmaları kullanılarak aynı kaynağa eş zamanlı erişim problemleri çözülmüştür. **ASP.NET Core MVC** kullanılarak geliştirilen bu proje, **MSSQL** veritabanı ile çalışmaktadır ve SignalR ile sipariş işleme animasyonları gerçekleştirilmiştir.

## Kullanılan Teknolojiler
- **ASP.NET Core MVC** (Backend)
- **HTML, CSS, JavaScript** (Frontend)
- **MSSQL** (Veritabanı)
- **Entity Framework Core** (ORM)
- **SignalR** (Gerçek Zamanlı Sipariş Animasyonu)
- **Multithreading & Thread Havuzu** (Eşzamanlı İşlem Yönetimi)
- **Mutex & Semaphore** (Senkronizasyon Mekanizmaları)
- **jQuery & AJAX** (Dinamik UI Güncellemeleri)
- **Chart.js** (Grafiksel Stok Görselleştirme)

## **Kurulum ve Çalıştırma**  

### **1. Projeyi Klonlayın**  

Aşağıdaki komutları terminal veya komut istemcisine girerek projeyi klonlayın:  

```sh
git clone https://github.com/zeynepkou/E-Commerce.git
cd E-Commerce
```

### **2. Uygulamayı Çalıştırın**  

Visual Studio'da `F5` tuşuna basarak veya terminalde aşağıdaki komutu çalıştırarak uygulamayı başlatabilirsiniz:  

```sh
dotnet run
```

## Ana Özellikler

### 1. Müşteri Yönetimi
- Müşteri kaydı, giriş ve profil güncelleme işlemleri
- Premium ve Normal müşteri sınıfları
- Dinamik öncelik sıralaması ile müşteri bekleme süresi bazlı öncelik değişimi
- Öncelik skoru hesaplaması:
  ```
  Öncelik Skoru = Temel Öncelik Skoru + (Bekleme Süresi × 0.5)
  ```
  - Premium Müşteri: **15** Temel Öncelik Skoru
  - Normal Müşteri: **10** Temel Öncelik Skoru

### 2. Stok Yönetimi
- Admin yeni ürün ekleyebilir, silebilir, stok güncelleyebilir.
- Eş zamanlı stok güncellemesi Mutex ve Semaphore kullanılarak senkronize edilir.
- Eğer stok yetersizse sipariş reddedilir.

### 3. Sipariş Yönetimi
- Premium müşteriler önceliklidir, sipariş öncelik sıralaması dinamik olarak güncellenir.
- Müşteri, her üründen en fazla 5 adet alabilir.
- Bütçe kontrolü: Eğer müşteri bütçesi yetersizse işlem reddedilir.
- Admin siparişleri onaylayabilir veya reddedebilir.
- Tüm siparişleri onaylama butonu ile SignalR üzerinden sipariş işleme animasyonu başlatılır.
- Sipariş işlemleri tamamlandıkça gerçek zamanlı UI güncellemesi yapılır

### 4. Dinamik Öncelik ve Bekleme Sistemi
- Bekleyen müşteriler için öncelik skoru anlık olarak güncellenir.
- Bekleme süresi arttıkça normal müşterilerin önceliği artar.
- Premium müşteri olan biri, sırasını koruyarak sonraki siparişlerinde premium statüsünden yararlanır.

### 5. Loglama & Hata Yönetimi
- Tüm müşteri ve admin işlemleri loglanır.
- **Gerçek zamanlı log paneli** ile hata ve başarılı işlemler UI’da listelenir.
- Örnek hata mesajları:
  - Ürün stoğu yetersiz
  - Müşteri bakiyesi yetersiz

### 6. SignalR ile Gerçek Zamanlı Sipariş İşleme Animasyonu
- **Admin “Tüm Siparişleri Onayla” butonuna bastığında:**
  - SignalR gerçek zamanlı sipariş işleme animasyonunu başlatır.
  - Onaylanan siparişler sırasıyla UI üzerinde animasyonla güncellenir.
  - Sipariş durumu **“Bekleniyor” → “İşleniyor” → “Tamamlandı”** olarak değişir

## Kullanıcı Arayüzü

### 1. **Admin Paneli**
- **Müşteri listesi:** ID, Ad, Tür, Bekleme Süresi, Öncelik Skoru
- **Bekleme durumu:** **Bekleniyor, İşleniyor, Tamamlandı** renk kodlu durum bilgisi

### 2. **Müşteri Paneli**
- **Sipariş oluşturma:** Ürün seçimi, adet girişi, sipariş butonu

### 3. **Ürün Stok Durumu Paneli**
- **Ürün tablosu:** Ürün Adı, Stok Miktarı, Fiyat
- **Grafik gösterimi:**
  - **Bar ve dairesel grafik ile stok durumu**
  - **Kritik stok seviyeleri için renk değişimiyle uyarı**

### 4. **Gerçek Zamanlı Log Paneli**
- **Başarılı ve başarısız işlemler listelenir**
- Örnek loglar:
  ```
  Müşteri 1, Product3’ten 2 adet aldı. İşlem Başarılı.
  Müşteri 2, Product5’ten 3 adet almak istedi. Yetersiz Stok.
  ```

### 5. **Dinamik Öncelik ve Bekleme Paneli**
- Müşterilerin bekleme süresi ve öncelik skoru tabloda anlık olarak güncellenir.
- Müşteri sırası animasyonla güncellenerek hareket eder.

### 6. **Sipariş İşleme Animasyonu**
- **Admin tüm siparişleri onayladığında:**
  - Siparişler sırasıyla **işleniyor animasyonu** ile görselleştirilir.
  - **Bekleyen siparişler yukarı kayarak işlenmeye başlar**.
  - Tamamlanan siparişler **“Tamamlandı”** olarak listelenir.

## **Ekran Görüntüleri**  

<table>
  <tr>
    <td><img src="https://github.com/zeynepkou/E-Commerce/blob/cee1e0810ad616171644f2b67ea342256ba9a7aa/images2/ECommerce1.png"/></td>
    <td><img src="https://github.com/zeynepkou/E-Commerce/blob/cee1e0810ad616171644f2b67ea342256ba9a7aa/images2/ECommerce2.png"/></td>
  </tr>
  <tr>
    <td><img src="https://github.com/zeynepkou/E-Commerce/blob/cee1e0810ad616171644f2b67ea342256ba9a7aa/images2/ECommerce3.png"/></td>
    <td><img src="https://github.com/zeynepkou/E-Commerce/blob/cee1e0810ad616171644f2b67ea342256ba9a7aa/images2/ECommerce4.png"/></td>
  </tr>

</table>


## **Proje Videosu**

![Video Önizleme](https://github.com/zeynepkou/E-Commerce/blob/cee1e0810ad616171644f2b67ea342256ba9a7aa/images2/ECommerce.gif)






