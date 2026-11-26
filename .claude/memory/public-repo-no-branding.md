---
name: public-repo-no-branding
description: StaffSync GitHub'da herkese açık — repoya (kod, docs, hafıza, commit mesajı) hiçbir şirket adı/logosu/ekran görüntüsü girmez
metadata:
  type: feedback
---

Kullanıcı 2026-11-26: "bu proje globale açık, bunda şirkete ait bir şey olmasın" ve "memoride de yazmasın, ya da onları commitle göndermeyelim".

- Kod, docs, `.claude/memory`, commit mesajları ve test verisinde şirket adı, logo, marka rengi adı, şirket e-posta alan adı veya şirket verisi içeren ekran görüntüsü olmaz. Örnek şirket gerekiyorsa "ACME".
- Şirkete özel kopyayı kullanıcı ayrı (özel) repoda yapacak; marka/plan/mockup dosyaları masaüstüne taşındı.
- Eski git geçmişinde şirket adı geçiyor; kullanıcı "dokunma" dedi — geçmişi yeniden yazmayı tekrar önerme.
- Uygulamaya dair ekran görüntüsü/tasarım dosyası repoya eklenecekse önce şirket verisi içermediğini kontrol et.

**Why:** Repo public; şirket bilgisi dışarı sızmamalı.
**How to apply:** Her commit öncesi `grep -ri` ile şirket adını tara. Bkz. [[design-polish-by-claude]].
