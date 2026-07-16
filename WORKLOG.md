# Sarfkor — журнал выполненных работ

Файл ведётся автоматически: после каждой выполненной задачи сюда добавляется запись — что сделано, где именно, и зачем.

---

## 2026-07-17

### Domain layer — создана доменная модель (Backend/Domain)

- Создано 29 entity-классов по разделу 5 CLAUDE.md (Product, Store, PriceEntry, CostPrice, Scan, Receipt, Review, Report, ContributorTrustScore, ExpiringOffer, ShoppingList, ShoppingListItem, SaleTransaction, SaleLineItem, StockLevel, StockMovement, Payment) плюс базовая инфраструктура (`Entity`) и value objects (`Money`, `Barcode`, `GeoLocation`, `PaymentToken`).
- По явному запросу пользователя упрощены до простых POCO-классов: `Id` — `int` (не `Guid`), без конструкторов с валидацией, без доменных методов — только `{ get; set; }`. Value objects превращены в позиционные record-ы без валидации.
- Удалены как неиспользуемые: `Guard`, `DomainException`, `SaleAlreadyVoidedException`, `InsufficientStockException`.
- **Важно**: этим решением нарушено собственное требование CLAUDE.md §2 ("доменная валидация внутри Entity/Value Object" — обязательное, не обсуждаемое). Проверки типа "нельзя voided дважды", "остаток не может стать отрицательным", "card-платёж требует токен" сейчас нигде не реализованы. Если понадобится — переносить в Application-layer (FluentValidation + логика хендлеров).
- По запросу пользователя добавлены дополнительные сущности сверх списка CLAUDE.md: `ReceiptLineItem`, `AuditLog`, `RefreshToken`, `Category`, `Brand`, `ProductImage`, `StoreEmployee` (+`StoreEmployeeRole`), `Supplier`, `Notification` (+`NotificationType`), `PriceAlert`, `Favorite` (+`FavoriteType`), `Promotion` (+`PromotionDiscountType`).
- `Product.Category`/`Product.Brand` (строки) заменены на `CategoryId`/`BrandId` (ссылки на новые lookup-сущности).
- `StockMovement` дополнен полем `SupplierId`.
- Проект `Backend/Domain` собирается без ошибок и предупреждений (`dotnet build`).

**Не сделано (следующий шаг)**: реальных таблиц в PostgreSQL ещё нет — нет `DbContext`, EF Core конфигурации и миграций (Infrastructure layer не тронут). Также не создан Application layer (use-case'ы из §6) и ASP.NET Identity/JWT.

### Domain layer — ещё 6 сущностей сверх исходного списка CLAUDE.md

По запросу пользователя добавлены сущности, обоснованные явными пунктами CLAUDE.md, но отсутствовавшие в разделе 5:

- `UserProfile` (Domain/Identity) — ASP.NET Identity хранит только данные для входа; отображаемое имя, аватар, предпочитаемый язык (tg/ru) нужно хранить отдельно.
- `DeviceToken` + `DevicePlatform` (Domain/Notifications) — без push-токена устройства `Notification` не может быть реально доставлен на телефон.
- `ReportDispute` + `ReportDisputeStatus` (Domain/Feedback) — CLAUDE.md §9 явно ставит вопрос "защита от сговора против рейтинга магазина"; сущность позволяет StorePartner оспорить ложный `Report`.
- `SecurityEvent` + `SecurityEventType` (Domain/Security) — CLAUDE.md ставит безопасность приоритетом №1 и требует отслеживать аномальные паттерны (§10); сущность логирует неудачные входы, вход с нового устройства и т.п.
- `ReviewReply` (Domain/Feedback) — ответ StorePartner на `Review` покупателя.
- `FiscalReceipt` (Domain/Sales) — CLAUDE.md §9 называет фискализацию чеков нерешённым юридическим блокером для Таджикистана; структура подготовлена заранее, но использоваться не будет, пока вопрос не решён на уровне бизнеса/права.

Итог: 35 entity-классов в Backend/Domain. Проект собирается без ошибок и предупреждений.

### Domain layer — исправлены 2 реальных пробела в модели

- `SaleReturn` + `ReturnLineItem` (Domain/Sales) — CLAUDE.md §4 буквально требует "отмена/возврат **позиции**" в чеке, а не только всей продажи целиком. До этого `SaleTransaction` умел быть voided только целиком — это не соответствовало спецификации.
- `ProductSubmission` + `ProductSubmissionStatus` (Domain/Products) — в §6 есть `ModerateNewProductCommand`, но у `Product` не было состояния "на модерации"; теперь пользовательские заявки на новый товар живут отдельно от уже одобренного каталога.

Итог: 37 entity-классов. Проект собирается без ошибок и предупреждений.

### Domain layer — ещё 2 сущности (последний раунд)

- `PriceEntryDispute` + `PriceEntryDisputeStatus` (Domain/Pricing) — CLAUDE.md §9: "конфликт при одновременных разных репортах цены" + "защита от сговора против рейтинга магазина".
- `UserConsent` + `ConsentType` (Domain/Identity) — CLAUDE.md §9: нерешённый вопрос юридических требований к персональным данным (чеки, геолокация, платежи) в Таджикистане.

Итог: 39 entity-классов в Backend/Domain. Проект собирается без ошибок и предупреждений.

**Рекомендация (зафиксировано для себя)**: дальнейший поиск сущностей без опоры на явный текст CLAUDE.md даёт всё более спекулятивные результаты. Следующий полезный шаг — не новые entity, а Infrastructure layer (DbContext, EF Core конфигурация, PostgreSQL миграции), иначе все 39 классов остаются кодом без единой реальной таблицы.

### Domain layer — 15 сущностей из практики реальных POS-систем (не из CLAUDE.md)

По прямому запросу пользователя ("не смотри в CLAUDE.md, что ещё можно сделать для POS-программы магазина") добавлены функции, типичные для промышленных POS (Square, Lightspeed, Odoo POS), которых не было в исходном техническом задании:

- **Касса/кассир**: `CashierShift` (открытие/закрытие смены, ожидаемая vs фактическая касса), `Commission` (Domain/Sales)
- **CRM/лояльность**: `Customer` (Domain/Customers), `LoyaltyProgram` + `LoyaltyAccount` + `LoyaltyTransaction` (Domain/Loyalty)
- **Финансовые инструменты**: `GiftCard`, `StoreCredit` (Domain/Payments)
- **Снабжение**: `PurchaseOrder` + `PurchaseOrderLineItem`, `StockTransfer` (между магазинами одного владельца), `ReorderRule` (авто-заказ при низком остатке) — все в Domain/Inventory
- **Ценообразование**: `TaxRate`, `ProductBundle` + `ProductBundleItem` (Domain/Catalog); `Product` дополнен полями `TaxRateId`, `IsSoldByWeight`, `UnitOfMeasure` (весовой товар)

Итог: 54 entity-класса в Backend/Domain. Проект собирается без ошибок и предупреждений.

## 2026-07-17 (продолжение) — Infrastructure: AppDbContext, EF Core, первая миграция

- Исправлен реальный баг в Domain: у 6 дочерних сущностей коллекций (`SaleLineItem`, `ShoppingListItem`, `ReceiptLineItem`, `PurchaseOrderLineItem`, `ProductBundleItem`, `ReturnLineItem`) отсутствовал FK на родителя (`SaleTransactionId`, `ShoppingListId`, `ReceiptId`, `PurchaseOrderId`, `ProductBundleId`, `SaleReturnId`). Без этого EF Core не смог бы связать коллекции `SaleTransaction.Lines`, `ShoppingList.Items` и т.д. Добавлены по conventional naming — EF подхватывает связь автоматически, без ручной Fluent-конфигурации.
- В `Backend/Infrastructure` добавлен пакет `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0-preview.7 — единственная версия, совместимая с net10.0 на момент разработки).
- В `Backend/WebApi` добавлен `Microsoft.EntityFrameworkCore.Design` (для инструментов `dotnet ef`). Есть предупреждения NU1903 (высокая severity) на транзитивные пакеты `Microsoft.Build.Tasks.Core`, `Microsoft.Build.Utilities.Core`, `Microsoft.OpenApi` — это preview-пакеты Microsoft, не наш код; стоит перепроверить при выходе стабильных версий .NET 10.
- Создан `Infrastructure/Persistence/AppDbContext.cs` — `DbSet<T>` для всех 55 entity (54 из Domain + учтён `FiscalReceipt`, который не был посчитан ранее).
- Создано 19 файлов `IEntityTypeConfiguration<T>` в `Infrastructure/Persistence/Configurations/` — только там, где нужна конфигурация сверх соглашений EF Core: `ComplexProperty` для value objects (`Money`, `Barcode`, `GeoLocation`, `PaymentToken` — они не entity, а встраиваемые типы) и уникальные индексы (`GiftCard.Code`, `RefreshToken.Token`, `SaleTransaction.(StoreId, IdempotencyKey)`).
- **Известное ограничение**: уникальный индекс на `Product.Barcode.Value` не настроен — текущая preview-версия EF Core (10.0.0-preview.7) не поддерживает `HasIndex` на вложенное свойство комплексного типа ни через лямбду, ни через строковый путь ("Barcode_Value" отклонён как "нет CLR-свойства"). Уникальность штрихкода нужно проверять на уровне Application layer (перед вставкой), пока EF Core не даст стабильный API для этого случая.
- `AppDbContext` зарегистрирован в `WebApi/Program.cs` через `AddDbContext` + `UseNpgsql`, строка подключения читается из конфигурации (`ConnectionStrings:DefaultConnection`).
- Строка подключения **не хранится в репозитории** — согласно CLAUDE.md §2 ("секреты через переменные окружения / Secret Manager"). Локально настроена через `dotnet user-secrets` (файл вне репозитория, в `%APPDATA%`); в проде должна приходить через переменную окружения `ConnectionStrings__DefaultConnection`.
- Сгенерирована первая миграция `InitialCreate` (`dotnet ef migrations add`) — создаёт все 55 таблиц в PostgreSQL. Миграция ещё не применена к реальной базе (`dotnet ef database update` не запускался — нет доступного PostgreSQL-сервера в этой сессии).
- Решение собирается целиком (`dotnet build Backend.slnx`) без ошибок.

**Не сделано (следующий шаг)**: применить миграцию к реальной БД, Application layer (use-case'ы §6 с FluentValidation), ASP.NET Identity + JWT.

### Правка по запросу пользователя — видимый placeholder connection string

В `WebApi/appsettings.json` добавлен ключ `ConnectionStrings:DefaultConnection` с заведомо нерабочим значением (`Password=CHANGE_ME`) — чтобы структура конфигурации была видна в репозитории. Реальное значение по-прежнему приходит из `dotnet user-secrets` в Development (перекрывает этот placeholder по стандартному порядку приоритета конфигурации ASP.NET Core) и должно приходить из переменной окружения `ConnectionStrings__DefaultConnection` в проде — сам placeholder никогда не подключится к реальной базе, что и есть цель.

### Комплексная проверка проекта — найдены и исправлены 2 проблемы репозитория

По запросу "проверь всё разом" выполнена сквозная проверка:
- `dotnet build Backend.slnx` — без ошибок.
- `dotnet ef migrations has-pending-model-changes` — модель и последняя миграция синхронны, расхождений нет.
- Количество `DbSet<T>` в `AppDbContext` (55) совпадает с количеством entity-классов в Domain (55); дубликатов имён классов нет.

Найдено при проверке:
1. **Пользователь вручную заменил placeholder в `appsettings.json` на реальный пароль** (`Password=12345`). Проверка `git status`/`git ls-files` показала, что `appsettings.json` отслеживается git с первого коммита — то есть при следующем `git commit` этот пароль навсегда попал бы в историю репозитория (нарушение CLAUDE.md §2).
2. **В репозитории вообще не было `.gitignore`** — 45 файлов сборки (`bin/`, `obj/`) уже отслеживались git.

Исправлено (по решению пользователя): создан корневой `.gitignore` (`bin/`, `obj/`, `.vs/`, `*.user`, `appsettings.*.json`); оба `appsettings*.json`-файла и все закоммиченные `obj/`-папки убраны из индекса git через `git rm --cached` (физически остались на диске, локальный пароль не пострадал). Ничего не закоммичено — изменения только застейджены, финальный коммит пользователь делает сам.
