# 🌳 صندوق سرمایه‌گذاری طوبی — Tobi Fund Management

یک نرم‌افزار **Desktop** کامل، حرفه‌ای، امن و **کاملاً آفلاین (Offline)** برای مدیریت صندوق.HandleFocusColor و اعضای صندوق — ساخته‌شده با **C# / .NET 8 / WPF / MVVM / SQLite / EF Core**.

> کاربر نهایی برای اجرا به اینترنت، سرور، SQL Server، MySQL یا هیچ سرویس خارجی نیاز ندارد.

---

## ✨ قابلیت‌ها

| بخش | توضیحات |
|---|---|
| 🔐 ورود | صفحه Login (RTL فارسی) با تصویر درخت طوبی، هش امن رمز (PBKDF2-SHA256)، پیام «نام کاربری یا رمز عبور صحیح نیست.» |
| 👤 کاربر پیش‌فرض | `admin` / `123456789` — ساخته‌شده خودکار در اولین اجرا + پیشنهاد تغییر رمز |
| 🛡️ RBAC | نقش‌های `Admin` و `Operator` — اپراتور نمی‌تواند کاربران/تنظیمات/پشتیبان را مدیریت کند |
| 📊 داشبورد | آمار کلی (اعضا، سرمایه، وام، بدهی‌ها، امتیاز) + اعضای برتر + هشدارها (معوق‌ها، نزدیک به سررسید، بدهکار) |
| 👥 اعضا | افزودن/ویرایش/حذف منطقی/جستجو (نام، نام خانوادگی، شناسه، شماره تماس)/مرتب‌سازی/جزئیات کامل |
| 💰 سرمایه | ثبت تراکنش (واریز/برداشت/تعدیل) — تاریخچه کامل حفظ می‌شود، هیچ‌گاه فقط مقدار نهایی ذخیره نمی‌شود |
| 🏦 وام | ثبت وام + تولید خودکار اقساط (اولین سررسید = تاریخ + ۳۰ روز، بقیه ماه شمسی به ماه) |
| 📆 اقساط | پرداخت کامل/جزئی، وضعیت‌های Future/Due/PartiallyPaid/Paid/Overdue، محاسبه اقساط سررسیدشده |
| 🧾 آبونمان | ۱۲ آبونمان ماهانه در سال (پیش‌فرض ۱۵۰٬۰۰۰)، پرداخت کامل/جزئی، محاسبه آبونمان معوق |
| ⭐ امتیاز | `Floor(سرمایه/۱۰۰۰) − Floor(مانده وام/۱۰۰۰)` — Snapshot روزانه (DailyScores) با قید یکتا (MemberId+ScoreDate) |
| 🏆 رتبه‌بندی | مرتب‌سازی اعضا بر اساس امتیاز کل (Rank 1 = بیشترین) |
| 📑 گزارش‌ها | ۷ گزارش + **Print** + **Export Excel** + **Export PDF** |
| 👤 کاربران | ایجاد کاربر (مدیر/اپراتور)، فعال/غیرفعال‌سازی (فقط مدیر) |
| ⚙️ تنظیمات | تعداد پیش‌فرض اقساط، روز تا اولین سررسید، مبلغ آبونمان، ضرایب امتیاز (فقط مدیر) |
| 💾 پشتیبان‌گیری | Backup → `Backup/Fund_yyyy-MM-dd_HH-mm.db` + Restore با تأیید کاربر (فقط مدیر) |
| 📜 Audit Log | ثبت رویدادهای مهم: Login, Logout, ایجاد/ویرایش/حذف عضو، سرمایه، وام، قسط، آبونمان، تنظیمات، Restore |
| 📅 تقویم شمسی | تاریخ‌ها در DB به‌صورت Gregorian ذخیره می‌شوند؛ نمایش شمسی با ارقام فارسی در لایه Presentation |
| 📅 انتخابگر تاریخ | فیلدهای تاریخ فقط‌خواندنی هستند؛ با کلیک، پاپ‌اپ انتخاب روز / ماه / سال شمسی باز می‌شود |
| 📥 واردات اکسل | خواندن فایل اکسل اعضا — ستون‌ها: **شناسه، نام، نام خانوادگی، سرمایه، وام** — ثبت عضو + سرمایه/وام اولیه با اعتبارسنجی |
| 🎨 رابط کاربری | دکمه‌ها با گرادیانت و گوشه‌های گرد؛ نوار ابزار در یک ردیف |

---

## 🏗️ معماری (Clean Architecture)

```
TobiFund.sln
├── src/
│   ├── TobiFund.Domain/          # Entityها + Enumها (بدون وابستگی)
│   ├── TobiFund.Application/     # DTOها، Interfaceها، سرویس‌های منطق کسب‌وکار، Helperها
│   ├── TobiFund.Infrastructure/  # DbContext (EF Core + SQLite)، Migrations، Seeder، FileLogger
│   └── TobiFund.WPF/             # Presentation — Views, ViewModels (MVVM), Converters, Services
└── tests/
    └── TobiFund.Tests/           # ۴۰ Unit Test (xUnit)
```

- **الگوی معماری:** MVVM + Dependency Injection + Repository-less (سرویس‌ها مستقیم با `IApplicationDbContext` کار می‌کنند)
- **امنیت:** Password Hashing (PBKDF2-SHA256, 100k iteration, salt تصادفی)، SQL Injection Protection (EF Core parameterized)، Input Validation، Foreign Keys، Database Transactions برای عملیات مالی، Soft Delete، Audit Log
- **Solidity مالی:** تمام عملیات مالی داخل `BeginTransaction / Commit / Rollback` انجام می‌شوند

## 🗄️ جداول دیتابیس (SQLite)

`Users`, `Members`, `CapitalTransactions`, `Loans`, `LoanInstallments`, `InstallmentPayments`, `Subscriptions`, `SubscriptionPayments`, `DailyScores`, `Settings`, `AuditLogs`

- مسیر دیتابیس: `%AppData%\TobiFund\Data\Fund.db` (مستقل از محل EXE)
- Migrations: `src/TobiFund.Infrastructure/Data/Migrations/` (اجرای خودکار با `Database.Migrate()`)

## 🧮 قوانین مالی

| قانون | پیاده‌سازی |
|---|---|
| مبلغ قسط | `مبلغ وام / تعداد اقساط` (باقیمانده روی قسط آخر) |
| اولین سررسید | `LoanDate + 30 روز` (قابل تنظیم) — مثال: ۱۴۰۵/۰۱/۱۰ → ۱۴۰۵/۰۲/۰۹ |
| سررسیدهای بعدی | به فاصله یک ماه شمسی |
| اقساط سررسیدشده | `SUM(Amount − PaidAmount)` برای `DueDate <= امروز && مانده > 0` |
| آبونمان معوق | `SUM(Amount − PaidAmount)` برای `DueDate <= امروز && مانده > 0` |
| امتیاز روزانه | `Floor(سرمایه/واحد) * ضریب − Floor(مانده وام/واحد) * ضریب` — Snapshot تغییرناپذیر (immutable) برای روزهای گذشته |
| مانده وام | مبلغ وام منهای پرداخت‌های انجام‌شده (وام تسویه‌شده = ۰) |

## 🚀 Build / Run / Test

```powershell
# Build
dotnet build -c Release

# Run
dotnet run --project src/TobiFund.WPF -c Release

# Test (۴۹ تست)
dotnet test -c Release
```

## 📦 Publish و استقرار

راهنمای کامل استقرار (Self-Contained Single-File، Portable، Installer با Inno Setup) در **[deploy.md](deploy.md)** آمده است.

دستور ساخت نسخه نهایی (یک فایل exe مستقل، بدون نیاز به نصب .NET):

```powershell
dotnet publish src/TobiFund.WPF/TobiFund.WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

خروجی: `publish/TobiFund.exe` — اسکریپت `installer/TobiFundSetup.iss` برای ساخت `TobiFundSetup.exe` (معادل `FundManagementSetup.exe`).

## 🔑 ورود اولیه

```
Username: admin
Password: 123456789
```

## 🧪 پوشش تست‌ها (۴۹ تست)

- محاسبه امتیاز (سرمایه ۵۰۰هزار − وام ۲۰۰هزار = ۳۰۰)
- مبلغ قسط (وام ۱۰۰هزار / ۱۰ = ۱۰هزار) + باقیمانده روی قسط آخر
- اولین سررسید = تاریخ + ۳۰ روز (۱۴۰۵/۰۱/۱۰ → ۱۴۰۵/۰۲/۰۹)
- ۱۲ آبونمان در سال + عدم تکرار
- بدهی سررسیدشده (DueInstallments) و آبونمان معوق
- یکتا بودن Snapshot روزانه + Catch-up روزهای گذشته + تغییرناپذیری (immutability) روزهای گذشته
- هش/تأیید رمز عبور، Login/Logout، Audit Log
- Validationهای مالی (مبلغ منفی، اضافه‌پرداخت، شناسه تکراری)
- واردات اکسل (Excel Import): ثبت عضو + سرمایه/وام اولیه، رد ردیف‌های تکراری/نامعتبر

## 📥 واردات اکسل (Excel Import)

از صفحه **اعضا** دکمه‌ی **واردات اکسل** را بزنید و فایل `.xlsx` را انتخاب کنید. ستون‌ها (بر اساس سرتیتر، یا به‌صورت ترتیبی ۱ تا ۵):

| ستون | توضیح |
|---|---|
| شناسه | کد عضویت (یکتا، الزامی) |
| نام | نام عضو (الزامی) |
| نام خانوادگی | نام خانوادگی (الزامی) |
| سرمایه | سرمایه اولیه (≥ ۰؛ اگر > ۰ باشد به‌عنوان واریز ثبت می‌شود) |
| وام | مبلغ وام اولیه (≥ ۰؛ اگر > ۰ باشد به‌عنوان وام با تعداد اقساط پیش‌فرض ثبت می‌شود) |

- ردیف‌های نامعتبر (خالی / منفی / تکراری) رد می‌شوند و فهرست خطاها در پایان نمایش داده می‌شود.
- ثبت هر عضو (و سرمایه/وام اولیه) در Audit Log lưu می‌شود.

## 📅 انتخابگر تاریخ شمسی (Persian Date Picker)

همه‌ی فیلدهای تاریخ (تاریخ عضویت، تاریخ وام، تاریخ تراکنش، تاریخ پرداخت‌ها، فیلتر گزارش‌ها) **فقط‌خواندنی** هستند و قابل تایپ نیستند. با کلیک روی فیلد، یک **پاپ‌اپ** باز می‌شود که امکان انتخاب **روز / ماه / سال** به شمسی را می‌دهد (با دکمه‌های «امروز»، «تأیید» و «انصراف»). تبدیل شمسی ↔ میلادی در لایه Presentation انجام می‌شود و مقدار Gregorian در دیتابیس ذخیره می‌گردد.

---

## 📁 Assets

- `src/TobiFund.WPF/Assets/tobi-tree-logo.png` — تصویر درخت طوبی (صفحه Login و منوی کناری)

## 📄 مستندات

- [deploy.md](deploy.md) — راهنمای استقرار
- [installer/TobiFundSetup.iss](installer/TobiFundSetup.iss) — اسکریپت Inno Setup
- [publish.ps1](publish.ps1) — اسکریپت Publish خودکار
