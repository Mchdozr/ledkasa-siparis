# LEDKASA Sipariş — GitHub → Plesk

Hedef: `https://siparis.ledkasa.com.tr`  
Repo: `https://github.com/Mchdozr/ledkasa-siparis`  
Sunucu: Linux Plesk — MariaDB 10.4

`main` kaynağı tutar. Her push’ta Actions derler ve **`plesk`** dalına yayın çıktısını yazar. Plesk bu dalı `httpdocs`’a çeker.

## Plesk Git

1. `siparis.ledkasa.com.tr` → **Git** (yoksa **Alan Adı Ekle / Git deposu**)
2. Depo: `https://github.com/Mchdozr/ledkasa-siparis.git`
3. Dal: **`plesk`** (`main` değil)
4. Dağıtım dizini: `/siparis.ledkasa.com.tr/httpdocs` (`/httpdocs` ana site, kullanma)
5. Webhook / otomatik dağıtım açık
6. Özel repo ise Plesk’e GitHub SSH deploy key veya erişim jetonu ver

İlk `plesk` dalı Actions’tan gelir; bağlantıyı o iş bittikten sonra kur.

## .NET Core ekranı (dosyalar geldikten sonra)

| Alan | Değer |
|---|---|
| Uygulama kökü | `/siparis.ledkasa.com.tr/httpdocs` |
| Belge kökü | `/siparis.ledkasa.com.tr/httpdocs/wwwroot` |
| Başlatma dosyası | `LedKasa.Siparis.dll` |
| Etkinleştirildi | işaretle |

Ortam değişkenleri (Plesk .NET Core → Düzenle):

- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ConnectionStrings__DefaultConnection` = `Server=localhost;Port=3306;Database=ledkasa_siparis;User=ledkasa_siparis;Password=...`
- `SEED__ADMINPASSWORD` = ilk yönetici şifresi
- `Seed__AdminEmail` = `admin@ledkasa.com.tr`
- `Telegram__BotToken` = BotFather token
- `Telegram__ChatId` = bildirim gidecek chat id (grup için `-100...`; gruba `/id` yazınca bot gösterir)
- `Telegram__AppUrl` = `https://siparis.ledkasa.com.tr`

Parolaları repoya yazmayın.

## Veritabanı

Plesk → `ledkasa.com.tr` → Veritabanı: `ledkasa_siparis` / kullanıcı `ledkasa_siparis` / MariaDB / yalnızca yerel erişim.
