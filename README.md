# IYSRestruct

## Scheduled Controller

`IYSIntegration.WorkerService` fonksiyonları API içine taşınmıştır. Aşağıdaki uç noktalar `ScheduledController` altında yer alır:

- `POST /api/scheduled/single-consent`
- `POST /api/scheduled/multiple-consent`
- `POST /api/scheduled/pull-consent`
- `POST /api/scheduled/sf-consent`
- `POST /api/scheduled/send-consent-error`

Bu uç noktalar harici zamanlayıcılar tarafından tetiklenmelidir.

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
- `IYSErrorMail` (Subject, To, From, FromDisplayName, Url)

### Çalıştırma Aralıkları

Hosted servislerin eski çalışma sıklıkları aşağıda dakika cinsinden belirtilmiştir:

- `MultipleConsentQueryDelay`: 0,5 dk
- `MultipleConsentRequestDelay`: 1 dk
- `SingleConsentQueryDelay`: 5 dk
- `PullConsentQueryDelay`: 60 dk
- `SfAddConsentDelay`: 1,5 dk
- `MultipleConsentQueryCheckAfter`: 1 dk

Bu aralıklar yapılandırma dosyalarından kaldırılmıştır. API uç noktalarını kendi zamanlayıcınız ile bu aralıkları dikkate alarak tetikleyebilirsiniz.
