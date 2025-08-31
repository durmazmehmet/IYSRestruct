# IYSRestruct

## Consent Schedule ve Error Report Controller'ları

`IYSIntegration.WorkerService` fonksiyonları API içine taşınmıştır. Konsent işlemleri artık `ConsentScheduleController` ve `ConsentErrorReportController` altında gruplanmıştır:

### ConsentScheduleController uç noktaları

- `POST /api/consent-schedule/single-consent`
- `POST /api/consent-schedule/multiple-consent`
- `POST /api/consent-schedule/pull-consent`
- `POST /api/consent-schedule/sf-consent`

### ConsentErrorReportController uç noktaları

- `POST /api/consent-error-report/send-consent-error/excel`
- `POST /api/consent-error-report/send-consent-error/json`

`send-consent-error` uç noktaları `date` sorgu parametresi alır ve istenen tarih için hatalı kayıtları döndürür.

Bu uç noktalar harici zamanlayıcılar tarafından tetiklenmelidir. Tüm uç noktalar `ResponseBase` tipinde cevap döndürür; başarı durumunda işlem sayılarını, hata durumunda ise mesajları içerir.

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
