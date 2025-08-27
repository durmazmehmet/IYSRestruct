# IYSRestruct

## Scheduled Controller

`IYSIntegration.WorkerService` fonksiyonları API içine taşınmıştır. Aşağıdaki uç noktalar `ScheduledController` altında yer alır:

- `POST /api/scheduled/single-consent`
- `POST /api/scheduled/multiple-consent`
- `POST /api/scheduled/pull-consent`
- `POST /api/scheduled/sf-consent`
- `GET /api/scheduled/send-consent-error/report?date=YYYY-MM-DD`
- `GET /api/scheduled/send-consent-error/report-excel?date=YYYY-MM-DD`

İlk dört uç nokta harici zamanlayıcılar tarafından tetiklenmeli ve her çağrı işlenen kayıt sayısı ile oluşan hata mesajlarını döner:

- `successCount` – başarıyla işlenen kayıt sayısı
- `failedCount` – hata alan kayıt sayısı
- `errors` – oluşan hata mesajlarının listesi

`send-consent-error` uç noktaları belirtilen tarih için ya JSON hata listesi ya da Base64 kodlu Excel çıktısı döner.

## Konfigürasyon

`IYSIntegration.WorkerService` projesindeki `appsettings.json` dosyasındaki parametreler API projesine taşınmıştır.
Önemli anahtarlar:

- `MultipleConsentQueryBatchCount`
- `MultipleConsentBatchCount`
- `MultipleConsentBatchSize`
- `SingleConsentProcessRowCount`
- `RunAsSingle`
- `PullConsentBatchSize`
- `SfConsentProcessRowCount`

### Çalıştırma Aralıkları

Hosted servislerin eski çalışma sıklıkları aşağıda dakika cinsinden belirtilmiştir:

- `MultipleConsentQueryDelay`: 0,5 dk
- `MultipleConsentRequestDelay`: 1 dk
- `SingleConsentQueryDelay`: 5 dk
- `PullConsentQueryDelay`: 60 dk
- `SfAddConsentDelay`: 1,5 dk
- `MultipleConsentQueryCheckAfter`: 1 dk

Bu aralıklar yapılandırma dosyalarından kaldırılmıştır. API uç noktalarını kendi zamanlayıcınız ile bu aralıkları dikkate alarak tetikleyebilirsiniz.
