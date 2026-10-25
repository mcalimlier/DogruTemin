# DoğruTemin bütünlük ve arşiv uygulama planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Kullanıcının dokuz maddelik isteğini mevcut uygulamada tamamlamak.
**Architecture:** Tek güncel JSON kaydı, içerik özetiyle bağlanan son PDF ve kayıtlı veriden üretim. WPF arşiv tablosuna satır eylemleri ve EDHS kart dili.
**Tech Stack:** C#, .NET 8, WPF, xUnit, PDFsharp.
**Spec:** docs/superpowers/specs/2026-10-25-integrity-archive-design.md

## Global Constraints
- 10 ürün, 5 firma, en fazla iki ondalık giriş.
- Düzeltme açık onayla mevcut kaydın yerini alır; eski sürümler arşiv listesinde çoğalmaz.
- Kullanıcı verisiyle test yapılmaz; geçici test kökleri kullanılır.

## Review Focus
- Yüklenen fazla satırların kaybolmaması.
- İptal edilen değişikliğin diske yazılmaması.
- Eski çıktıların güncel görünmemesi.
- Uzun Türkçe metnin PDF'de eksik basılmaması.
- Küçük pencerede son satır ve düğmelerin erişilebilirliği.

## Tasks
- [x] 1. Sayısal sınırlar ve kapasite: NumberParser, ProcurementLimits, StorageService, hesap/validasyon/UI; sınır regresyonları ilk koşumda beklenen hataları verdi, düzeltmelerden sonra geçti.
- [x] 2. Kayıt düzeltme onayı ve içerik kimliği: ProcurementData, RecordIntegrity, ProcurementWorkflow; onay/iptal/çakışma testleri.
- [x] 3. PDF sığma kontrolü ve kayıtlı çıktı servisi: PdfVectorWriter, SavedDocumentService; taşma, tüm alanlar, dosya koruma testleri.
- [x] 4. Kalıcı teklif kaydı ve çıktı akışını servislerle bağla; arşiv satır eylemleri/durumları ve sayısal/tarih sıralaması.
- [x] 5. EDHS kartları ve ekran profilleri; gerçek WPF yerleşim testleri ve görsel inceleme.
- [x] 6. Release derlemesi, 122/122 test, diff incelemesi, kullanım notu ve kanıt raporu. Ayrı altajan aracı bulunmadığından diff incelemesi bu oturumda yapıldı.

Ek doğrulama: PKCS geçişli bağımlılığı 8.0.2'ye güncellendi; güvenlik taraması temiz. Eski şema kaydı, otomatik yedek kapalıyken de tek seferlik geçiş yedeğiyle korunuyor. Donanım kabulünün sınırları kabul raporunda belirtilmiştir.
