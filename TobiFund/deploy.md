# 🚀 راهنمای استقرار (Deploy) — صندوق سرمایه‌گذاری طوبی

این سند نحوه **Build**، **Publish** و **استقرار** نرم‌افزار را روی ویندوز توضیح می‌دهد.
نرم‌افزار یک **Desktop Application آفلاین** است؛ کاربر نهایی برای اجرا به اینترنت، SQL Server یا هیچ سرویس خارجی نیاز ندارد.

---

## ۱. پیش‌نیازها (روی máy Build)

| مورد | توضیحات |
|---|---|
| سیستم‌عامل | Windows 10/11 (64-bit) — سیستم مقصد هم 64-bit یا 32-bit (طبق نوع انتشار) |
| .NET 8 SDK | برای Build/Publish روی máy Build: https://dotnet.microsoft.com/download/dotnet/8.0 |
| Visual Studio 2022 | اختیاری — workload «.NET desktop development» (برای توسعه و Debug) |
| Inno Setup 6 | فقط برای ساخت فایل نصب `.exe` — https://jrsoftware.org/isdl.php |

> ⚠️: Build پروژه WPF **فقط روی Windows** انجام می‌شود (WPF خاص ویندوز است).
> Build روی Linux فقط برای CI با `-p:EnableWindowsTargeting=true` ممکن است؛
> برای استقرار نهایی، Build روی Windows توصیه می‌شود.

---

## ۲. Build (تست Build)

```powershell
dotnet build -c Release
```

یا Build فقط یک پروژه:

```powershell
dotnet build src/TobiFund.WPF/TobiFund.WPF.csproj -c Release
```

## ۳. اجرای محلی (Run)

```powershell
dotnet run --project src/TobiFund.WPF -c Release
```

## ۴. انتشار Self-Contained Single-File (پیشنهادی — یک فایل exe مستقل)

با این دستور یک **فایل exe واحد** ساخته می‌شود که:

- نیازی به نصب .NET Runtime روی سیستم مقصد ندارد
- نیازی به نصب SQLite ندارد (داخل exe بسته‌بندی شده)
- کاملاً آفلاین کار می‌کند

```powershell
dotnet publish src/TobiFund.WPF/TobiFund.WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

خروجی: `publish/TobiFund.exe`

> این فایل را می‌توانید روی هر ویندوز 64-bit با کپی کردن اجرا کنید.
>
> نحوه اجرا:
> ۱. `publish/TobiFund.exe` را اجرا کنید.
> ۲. در اولین اجرا دیتابیس به‌صورت خودکار در `%AppData%\TobiFund\Data\Fund.db` ساخته می‌شود.
> ۳. با `admin` / `123456789` وارد شوید.

### نسخه Portable
فولدر `publish` را روی فلش/هارد اکسترنال کپی کنید و `TobiFund.exe` را اجرا کنید.
(دیتابیس کماکان در AppData کاربر ذخیره می‌شود؛ برای DB کاملاً قابل‌حمل،
کلید `Database:Path` را در `appsettings.json` به یک مسیر نسبی مثل `Fund.db` تغییر دهید.)

## ۵. انتشار Framework-Dependent (exe کوچک‌تر، نیازمند .NET 8 Runtime)

اگر روی سیستم مقصد **.NET 8 Desktop Runtime** نصب است:

```powershell
dotnet publish src/TobiFund.WPF/TobiFund.WPF.csproj -c Release -r win-x64 --self-contained false -o publish-fdd
```

## ۶. ساخت Installer (Inno Setup)

۱. ابتدا Publish را انجام دهید (بخش ۴).
۲. اسکریپت `installer/TobiFundSetup.iss` را با **Inno Setup Compiler** باز کنید.
۳. دکمه **Compile** را بزنید.

خروجی: `installer-output/TobiFundSetup.exe` — یک فایل نصب استاندارد ویندوز
(قابل نصب در `Program Files`، ساخت میان‌بر روی Desktop و Start Menu، و uninstall).

> نام فایل نصب: `TobiFundSetup.exe` — معادل `FundManagementSetup.exe` در سند طراحی.

## ۷. اسکریپت خودکار Publish

```powershell
.\publish.ps1
```

این اسکریپت Publish (بخش ۴) را به‌صورت خودکار انجام می‌دهد و خروجی را در `publish/` می‌گذارد.

---

## ۸. محل ذخیره داده‌ها (سیستم مقصد)

| داده | مسیر |
|---|---|
| دیتابیس SQLite | `%AppData%\TobiFund\Data\Fund.db` |
| فایل‌های پشتیبان | `%AppData%\TobiFund\Backup\Fund_yyyy-MM-dd_HH-mm.db` |
| لاگ‌های برنامه | `%AppData%\TobiFund\Logs\app-yyyyMMdd.log` |

> مسیر دیتابیس **مستقل از محل اجرای EXE** است و در پوشه AppData کاربر (قابل‌نوشتن) ذخیره می‌شود.
> برای override، کلید `Database:Path` در `appsettings.json` کنار exe را تنظیم کنید.

## ۹. کاربر پیش‌فرض

```
Username: admin
Password: 123456789
```

- رمز عبور به‌صورت **Hash (PBKDF2-SHA256)** ذخیره می‌شود.
- پس از اولین ورود، banner «تغییر رمز عبور» نمایش داده می‌شود.

## ۱۰. ساختار پوشه Publish

```
publish/
├── TobiFund.exe          ← فایل اجرایی (Single-File)
├── appsettings.json      ← تنظیمات (مسیر دیتابیس)
└── (کتابخانه‌های Native در اولین اجرا استخراج می‌شوند)
```

## ۱۱. عیب‌یابی (Troubleshooting)

| مشکل | راه‌حل |
|---|---|
| پیام Windows Defender SmartScreen | «More info → Run anyway» — exe امضا نشده است. برای استقرار رسمی، exe را با گواهی digital امضا کنید. |
| exe اجرا می‌شود ولی چیزی نمایش نمی‌دهد | پوشه `%AppData%\TobiFund\Logs` را بررسی کنید؛ پوشه AppData را دستی ایجاد کنید. |
| فونت فارسی | فونت Tahoma به‌صورت پیش‌فرض روی همه ویندوزها موجود است. |
| Build خطای WPF روی Linux | WPF فقط روی Windows Build/Publish می‌شود (یا با `-p:EnableWindowsTargeting=true`). |
| نسخه 32-bit | به‌جای `-r win-x64` از `-r win-x86` استفاده کنید. |

## ۱۲. تست

```powershell
dotnet test -c Release
```

تست‌های Unit شامل: محاسبه امتیاز، مبلغ قسط، اولین سررسید، ۱۲ آبونمان در سال،
بدهی سررسیدشده، و یکتا بودن Snapshot روزانه.

---

## 📋 چک‌لیست استقرار نهایی

- [ ] `dotnet build -c Release` بدون خطا
- [ ] `dotnet test -c Release` همه تست‌ها Pass
- [ ] `dotnet publish ... -r win-x64 --self-contained true -p:PublishSingleFile=true` انجام شد
- [ ] `publish/TobiFund.exe` روی یک ویندوز «تمیز» (بدون .NET) تست شد
- [ ] ورود با `admin` / `123456789` تست شد
- [ ] ثبت عضو، سرمایه، وام، قسط، آبونمان تست شد
- [ ] پشتیبان‌گیری و بازیابی تست شد
- [ ] (اختیاری) Installer با Inno Setup ساخته و تست شد
