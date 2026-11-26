---
name: future-exchange-import
description: İleride ayrı bir uygulama Exchange'ten kişileri StaffSync DB'sine aktaracak; StaffSync kullanıcıların eriştiği yer, manuel kişi/kullanıcı ekleme de olacak
metadata:
  type: project
---

Kullanıcı 2026-11-26: "ileride başka bir uygulama yapacağım, Exchange'ten numaraları/bilgileri bunun DB'sine aktaracak. Burası kullanıcıların erişeceği yer. Yeri geldiğinde manuel numara ya da kullanıcı ekleyecekler."

Bu tarihte sistemde eksik olanlar (incelemeyle tespit):
- Contact'ta kaynak (Exchange/Manuel) ve dış anahtar (Exchange ObjectGuid vb.) yok → aktarım eşleştirmesi ve manuel kayıtları koruma yapılamaz.
- Doğrudan SQL ile yazılırsa `UpdatedDate` (sadece EF SaveChanges'te damgalanıyor) ve `DirectoryChangeNotifier` (vcf/csv) tetiklenmez → mobil değişikliği görmez.
- CreateContact e-posta + cep numarasını zorunlu tutuyor (santral/oda gibi kayıtlar eklenemez); DisplayName/JobTitle/Department/Company 50 karakter.
- `Auth/Register` herkese açık; admin rolü atama ucu yok; manuel ekleme sadece Swagger'dan (admin ekranı yok).

**Why:** Veri modeli ve yetki yapısı bu iki kaynaklı (Exchange + manuel) senaryoya göre şekillenmeli.
**How to apply:** Contact/Auth'a dokunan her işte bu senaryoyu gözet; aktarımın API üzerinden mi DB'ye doğrudan mı yapılacağı kullanıcıya soruldu (2026-11-26). Bkz. [[project-overview]], [[mobile-scope]].
