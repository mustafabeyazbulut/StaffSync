---
name: package-license-pins
description: MediatR 12.x'te sabit (ticari lisans); AutoMapper projeden kaldırılacak (14.0.0'da güvenlik açığı) — yerine elle eşleme
metadata:
  type: feedback
---

- **MediatR** 12.x'in en güncel ücretsiz sürümünde (12.5.0) kalır; 13+ sürüme **yükseltilmez**.
- **AutoMapper** kullanılmaz: kullanıcı 2026-11-24'te "çıkar, elle eşle" dedi. Ücretsiz son sürüm 14.0.0'da 3 yüksek seviye güvenlik açığı var (GHSA-6vhm-j9h7-f9gh, GHSA-gpwp-5797-mjff, GHSA-rvv3-g6hj-g44x), düzeltmeler sadece ücretli 15.1.4+/16.3+'te. Yerine `ContactMappings` gibi static extension metotlarla elle eşleme. Mapster vb. başka bir mapper önerme.
- Diğer tüm paketler en güncel sürüme çekilebilir.

**Why:** 2025'te ikisi de ticari lisansa geçti (Lucky Penny Software); şirket cirosu Community lisans sınırının (5M$) üstünde.
**How to apply:** Paket güncellemesinde MediatR'ı `[12.*,13)` aralığında tut; yeni kodda mapper kütüphanesi kullanma. Bkz. [[project-overview]].
