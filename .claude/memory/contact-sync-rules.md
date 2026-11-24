---
name: contact-sync-rules
description: Telefon rehberine senkron kuralları — eklenen kişiler StaffSync'ten geldiği belli olacak, senkron kapatılınca hepsi silinecek
metadata:
  type: project
---

Telefona aktarılan kişiler için iki zorunlu kural (kullanıcı 2026-11-24'te istedi):

1. **Kaynağı belli olacak** — kullanıcı rehberinde bir kişinin StaffSync'ten geldiğini anlayabilmeli.
2. **Senkron kapatılınca StaffSync'in eklediği tüm kişiler silinecek** — kullanıcının kendi kişilerine asla dokunulmaz.

Uygulama planı:
- **Android:** Kendi hesap tipi (`AccountManager` + SyncAdapter, hesap adı "StaffSync"). Kişiler rehberde ayrı hesap altında görünür; kapatınca hesap silinir → kişileri de toplu gider.
- **iOS:** Uygulamalar kendi hesabını/container'ını açamaz. Kişiler "StaffSync" adlı `CNGroup`'a eklenir, kurum alanı/not alanında işaret bırakılır, oluşturulan kişilerin `identifier`'ları uygulamada saklanır; kapatınca bu listeden silinir. Kullanıcı kişiyi elle düzenlemiş/birleştirmişse silme hatası sessizce atlanır.
- Profil'de senkron kapatılırken onay sorulur ("Rehberden N kişi kaldırılacak").

**Why:** Çalışanlar kendi rehberinde şirket kişilerini ayırt edebilmeli; senkronu kapatan veya ayrılan biri şirket numaralarını taşımamalı.
**How to apply:** Senkron servisini yazarken kişi ekleme/güncelleme/silme her zaman bu işaret üzerinden yapılır; işaretsiz kişiye dokunulmaz. Bkz. [[mobile-scope]].
