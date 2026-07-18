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

### Merge в master + инцидент с потерей локального appsettings.json

- Изменения закоммичены и запушены в ветку `Ardasher`, затем смерджены (fast-forward) в `master` и запушены на `origin/master` по прямому запросу пользователя (team lead).
- **Инцидент**: последовательность `git checkout master` → `git merge Ardasher` → `git checkout Ardasher` привела к тому, что физически удалились локальные файлы `appsettings.json` и `appsettings.Development.json` (они стали untracked+ignored после `git rm --cached`, и при переключении веток/merge git убрал их с диска, так как в целевых коммитах их не было). Реальный пароль, который пользователь вручную вписал в `appsettings.json`, был потерян безвозвратно — но это не критично, так как он никогда не был закоммичен и не использовался приложением напрямую (значение из `user-secrets` имеет приоритет и осталось нетронутым, поскольку хранится вне рабочего дерева git). Оба файла пересозданы с прежним placeholder-содержимым.

### Первый успешный прогон против реальной локальной PostgreSQL

- На машине пользователя обнаружен установленный и запущенный `postgresql-x64-18` (Windows-служба).
- `dotnet ef database update` с паролем-плейсхолдером сначала завершился ошибкой аутентификации (28P01) — ожидаемо, плейсхолдер не совпадал с реальным паролем локального PostgreSQL.
- После того как пользователь сообщил реальный пароль, `user-secrets` обновлён, и `dotnet ef database update` выполнен **успешно** — все 55 таблиц реально созданы в базе `sarfkor` (внешние ключи, индексы, `CREATE UNIQUE INDEX` на `GiftCards.Code`, `RefreshTokens.Token`, `SaleTransactions(StoreId, IdempotencyKey)` — всё сработало как задумано).
- `dotnet run --project WebApi/WebApi.csproj` запущен и поднялся без ошибок (`Now listening on: http://localhost:5000`).

**Итог на данный момент**: весь стек Domain → Infrastructure → PostgreSQL подтверждён рабочим end-to-end. Следующий шаг — Application layer (use-case'ы §6) и реальные API-эндпоинты, иначе frontend-разработчику по-прежнему не с чем интегрироваться.

## 2026-07-18 — Application layer: первый реальный use-case (ScanBarcodeQuery)

По решению пользователя (не блокировать работу на друге-frontend'ере — работать над backend самостоятельно) реализован первый вертикальный срез по MVP-приоритету CLAUDE.md §8: `ScanBarcodeQuery`.

- **Application**: добавлены пакеты `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `Microsoft.Extensions.DependencyInjection.Abstractions`. Удалён `Class1.cs`-заглушка.
  - `Common/IQueryHandler.cs` — единственный небольшой интерфейс `Handle(TQuery, CancellationToken)`, который реализует каждый handler (ISP, как требует CLAUDE.md §6).
  - `Common/GeoDistance.cs` — Haversine-формула для расстояния между пользователем и магазином (internal, чистая функция, без зависимостей).
  - `Abstractions/IProductRepository.cs`, `IStoreRepository.cs`, `IPriceEntryRepository.cs` — репозитории как абстракции (DIP): Application ничего не знает про EF Core.
  - `Products/Queries/ScanBarcode/` — `ScanBarcodeQuery` (record), `ScanBarcodeResult`/`StorePriceDto` (records), `ScanBarcodeQueryValidator` (FluentValidation: штрихкод 8–13 цифр, широта/долгота в допустимом диапазоне), `ScanBarcodeQueryHandler` (находит товар по штрихкоду → последнюю цену по каждому магазину → расстояние до пользователя, если передана геолокация → сортировка по расстоянию, затем по цене).
  - `DependencyInjection.cs` — `AddApplication()`: регистрирует все `IQueryHandler<,>`-реализации через рефлексию по сборке + `AddValidatorsFromAssembly`.
- **Infrastructure**: добавлен пакет `Microsoft.Extensions.Configuration.Abstractions`.
  - `Repositories/ProductRepository.cs`, `StoreRepository.cs`, `PriceEntryRepository.cs` — EF Core реализации. `PriceEntryRepository.GetLatestPerStoreAsync` — классическая задача "последняя запись в каждой группе" реализована через `GroupBy` + `Max` + `Join` (не через `GroupBy().Select(g => g.First())`, так как такой паттерн часто не транслируется в SQL многими провайдерами EF Core).
  - `DependencyInjection.cs` — `AddInfrastructure(configuration)`: теперь и `AddDbContext`, и регистрация репозиториев живут здесь, а не в `Program.cs`.
- **WebApi**: `Program.cs` переписан — вызывает `AddApplication()` + `AddInfrastructure()`, добавлен первый реальный эндпоинт `GET /api/products/scan/{barcode}?lat=&lng=` (валидация через FluentValidation → 400 с деталями при ошибке, 404 если товар не найден, 200 с ценами по магазинам иначе). Демо-эндпоинт `/weatherforecast` из шаблона удалён.
- **Проверено вручную** (`dotnet run` + `curl`, не только билд):
  - `GET /api/products/scan/abc` → `400`, тело содержит понятные ошибки валидации (длина, формат).
  - `GET /api/products/scan/1234567890128` → `404` (валидный формат, товара с таким штрихкодом в пустой БД нет — ожидаемо).
  - Логи чистые, только безобидное предупреждение "Failed to determine the https port for redirect" (нет launch-профиля при ручном запуске).
- **Побочный урок**: `dotnet run --no-launch-profile` без явного `ASPNETCORE_ENVIRONMENT=Development` поднимается в `Production`, где `user-secrets` не подключаются автоматически — это дало ложный 500 (Postgres auth failed на placeholder-пароле) при первой попытке ручного теста. Исправлено явной переменной окружения при ручном запуске.

**Не сделано**: данных в БД по-прежнему нет (нет seed/тестовых данных, поэтому "счастливый путь" 200 с реальными ценами не проверен вручную) — таблицы пустые. Остальные use-case'ы §6, ASP.NET Identity/JWT, Application-слой валидации для команд (Sale/Stock) — впереди.

### Проверка "счастливого пути" — вставлены тестовые данные напрямую через psql

Чтобы закрыть последний пробел (200-ответ с реальными данными ни разу не проверялся), напрямую через `psql` (найден в `C:\Program Files\PostgreSQL\18\bin\psql.exe`, не был в PATH) вставлены одна тестовая запись `Brand`, `Category`, `Product` (штрихкод `1234567890128`), `Store` (координаты Душанбе) и `PriceEntry` (12.50 TJS).

`GET /api/products/scan/1234567890128?lat=38.56&lng=68.78` → **`200 OK`**:
```json
{"productId":1,"productName":"Тестовый товар","stores":[{"storeId":1,"storeName":"Тестовый магазин","price":12.50,"currency":"TJS","distanceKm":0.61}]}
```
Название на кириллице, цена, валюта и расстояние (формула Haversine) — всё корректно. Это подтверждает: полный путь Validation → Application handler → Infrastructure repositories (включая join-запрос "последняя цена по магазину" и запрос по `ComplexProperty` `Barcode.Value`) → PostgreSQL → JSON-ответ работает end-to-end без единой ручной правки после первой реализации.

**Важно**: эти тестовые строки остались в реальной таблице `sarfkor` (не откачены) — это осознанно, чтобы у друга-frontend'а сразу был хоть один рабочий пример для интеграции. При необходимости удалить: `DELETE FROM "PriceEntries"; DELETE FROM "Products"; DELETE FROM "Stores"; DELETE FROM "Categories"; DELETE FROM "Brands";`.

## 2026-07-19 — Application layer: первый Command (SubmitPriceUpdateCommand)

Второй use-case по MVP-приоритету CLAUDE.md §6, и первый **Command** (не Query) — "юзер обновляет цену (с валидацией и весом по репутации)".

- **Application/Common**: добавлен `ICommandHandler<TCommand, TResult>` — второй единственный-интерфейс наравне с `IQueryHandler`.
- **Application/Abstractions**: расширены `IProductRepository`/`IStoreRepository` методом `ExistsAsync` (для проверки, что продукт/магазин реально существуют, прежде чем создавать `PriceEntry`); `IPriceEntryRepository` дополнен методом `Add`; добавлен новый `IContributorTrustScoreRepository` (`GetByUserIdAsync` + `Add`); добавлен `IUnitOfWork` (`SaveChangesAsync`) — команды пишут через несколько репозиториев, но коммитят изменения одним вызовом.
- **Application/Pricing/Commands/SubmitPriceUpdate/**: `SubmitPriceUpdateCommand` (record: ProductId, StoreId, UserId, Price, Currency), `SubmitPriceUpdateResult` (PriceEntryId, RecordedAt), валидатор (все поля обязательны, Price > 0, Currency — 3 буквы), хендлер — проверяет существование Product/Store (иначе null → 404), создаёт новый `PriceEntry` (append-only, как и раньше — это исторический снимок цены, не апдейт существующей записи), и если у пользователя ещё нет `ContributorTrustScore` — создаёт с дефолтным значением 50.
- **Упрощение, о котором нужно знать**: "вес по репутации" (weighted trust score influencing the price) из CLAUDE.md §6 **не реализован** — это осталось как констатация факта (создание записи о репутации, если её нет), а не как влияющий на что-либо алгоритм взвешивания. Полноценное взвешивание/модерация конфликтующих цен — отдельная, более сложная задача, требующая дополнительного дизайна (сам CLAUDE.md §9 отмечает это как нерешённый вопрос).
- **Infrastructure**: `ProductRepository`/`StoreRepository` дополнены `ExistsAsync` (через `AnyAsync`), `PriceEntryRepository` — методом `Add`; новый `ContributorTrustScoreRepository`; новый `UnitOfWork` (тонкая обёртка над `AppDbContext.SaveChangesAsync`). Оба `DependencyInjection.cs` (Application и Infrastructure) обновлены: Application теперь сканирует сборку на оба типа хендлеров (`IQueryHandler<,>` и `ICommandHandler<,>`) одним циклом; Infrastructure регистрирует новые репозитории и `IUnitOfWork`.
- **WebApi**: добавлен `POST /api/prices`, тело запроса биндится прямо в `SubmitPriceUpdateCommand` (без отдельного DTO — request и command совпадают по форме, лишняя обёртка не нужна).
- **Известный временный пробел (не скрываю)**: `UserId` сейчас приходит от клиента в теле запроса как обычное поле — это нормально только потому, что ASP.NET Identity/JWT ещё не подключены. Как только появится аутентификация, `UserId` должен браться из токена (`ClaimsPrincipal`), а не из тела запроса — иначе любой может подставить чужой `UserId`.
- **Проверено вручную** (`dotnet run` + `curl`, три сценария):
  - `POST /api/prices` с `price: -5` → `400` с понятной ошибкой валидации.
  - `POST /api/prices` с валидными данными → `200 {"priceEntryId":2,"recordedAt":"..."}`.
  - `POST /api/prices` с несуществующим `productId: 9999` → `404`.
  - Контрольная проверка сквозной корректности: после создания второй (более новой) `PriceEntry` для того же товара/магазина, повторный `GET /api/products/scan/1234567890128` вернул **новую** цену (13.75, не старую 12.50) — подтверждает, что join-логика "последняя цена по магазину" в `PriceEntryRepository` реально выбирает самую свежую запись, а не случайную.

## 2026-07-19 (продолжение) — ASP.NET Identity + JWT (закрыт пробел §2 CLAUDE.md)

Реализована аутентификация/авторизация — заодно закрыт временный пробел из предыдущего шага (`UserId` теперь берётся из JWT, а не из тела запроса).

- **Пакеты**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Infrastructure), `Microsoft.AspNetCore.Authentication.JwtBearer` (WebApi), `System.IdentityModel.Tokens.Jwt` (Infrastructure).
- **Infrastructure/Identity**: `ApplicationUser : IdentityUser` — сознательно живёт в Infrastructure, а не в Domain (Domain не должен знать про ASP.NET Identity — не нарушать границы слоёв Clean Architecture, ради которых заведён `UserProfile` как отдельная доменная сущность). `AppDbContext` теперь наследуется от `IdentityDbContext<ApplicationUser>` вместо простого `DbContext` (добавлены таблицы `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` и т.д. — стандартная схема Identity); `OnModelCreating` теперь обязательно вызывает `base.OnModelCreating(modelBuilder)` — без этого EF Core не сконфигурировал бы таблицы Identity.
- **Application/Abstractions**: `IAuthService` (`RegisterAsync`/`LoginAsync` → `AuthResult?`) и `IRefreshTokenRepository` — Application по-прежнему ничего не знает о самом ASP.NET Identity/JWT, только об абстракции результата.
- **Application/Identity/Commands**: `RegisterCommand`/`LoginCommand` (+валидаторы: email формат, пароль от 8 символов) — оба хендлера — тонкие обёртки над `IAuthService`, без лишней бизнес-логики.
- **Infrastructure/Identity**: `JwtTokenGenerator` (создаёт access-токен на 15 минут со claim'ами sub/email/role), `AuthService` (реализует `IAuthService` через `UserManager<ApplicationUser>`: `RegisterAsync` создаёт пользователя, назначает роль `User` по умолчанию — самостоятельная регистрация никогда не даёт `StorePartner`/`Admin`; `LoginAsync` проверяет пароль через `CheckPasswordAsync`; оба выпускают JWT + `RefreshToken` (уже существовавшая доменная сущность, наконец используется) и сохраняют его через `IUnitOfWork`).
- **Роли** (`User`/`StorePartner`/`Admin` из CLAUDE.md §7) сеются при старте приложения (`RoleManager<IdentityRole>.CreateAsync`, если не существуют) — минимальный, но рабочий подход для MVP.
- **WebApi/Program.cs**: настроена JWT Bearer аутентификация (`AddAuthentication` + `AddJwtBearer` с валидацией issuer/audience/lifetime/signing key), `AddAuthorization`, `UseAuthentication`/`UseAuthorization` в конвейере; добавлены `POST /api/auth/register` и `POST /api/auth/login`.
- **Исправлена уязвимость из предыдущего шага**: `POST /api/prices` теперь помечен `.RequireAuthorization()`, тело запроса — отдельный `SubmitPriceUpdateRequest` (без `UserId`), а сам `UserId` достаётся из `ClaimsPrincipal` (`ClaimTypes.NameIdentifier`) — подделать чужой `UserId` через тело запроса больше нельзя.
- **Секреты**: `Jwt:Key` — случайный 256-битный ключ (сгенерирован через `RandomNumberGenerator`), хранится только в `user-secrets`, в `appsettings.json` — заведомо нерабочий placeholder (аналогично connection string).
- **Новая миграция** `AddIdentity` создана и применена к реальной БД (`dotnet ef database update`) — добавлены все стандартные таблицы Identity.
- **Проверено вручную end-to-end**:
  - `POST /api/auth/register` → `200`, выдан JWT + refresh-токен.
  - `POST /api/auth/login` с верным паролем → `200`; с неверным → `401`.
  - `POST /api/prices` без токена → `401`; с токеном → `200`, новая `PriceEntry` создана.
  - Прямая проверка в БД (`psql`): `UserId` в новой строке `ContributorTrustScores` **совпадает** с `Id` в `AspNetUsers` для зарегистрированного пользователя — подтверждает, что цепочка JWT-claim → `ClaimsPrincipal` → `UserId` в команде работает правильно, а не просто "не падает".

**Не сделано**: `[Authorize(Roles = "...")]` на уровне ролей ещё не используется ни на одном эндпоинте (только факт аутентификации через `.RequireAuthorization()`); refresh-токен endpoint (обмен `RefreshToken` на новый access-токен) не реализован — `RefreshToken` создаётся и сохраняется, но ещё не используется по назначению.

## 2026-07-20 — Модуль POS/Касса (по плану, через EnterPlanMode → ExitPlanMode → одобрение пользователя)

По собственному предложению ("спланируй большую работу и сделай") реализован модуль кассы — самая критичная часть CLAUDE.md §4 (продажа + списание склада) с явной проверкой всех трёх инвариантов §10 (идемпотентность, невозможность двойного void, невозможность отрицательного остатка). План был сначала записан в отдельный plan-файл и одобрен пользователем перед началом кода.

### Три инварианта — как решены технически

- **Отрицательный остаток**: `StockLevelRepository.TryDecrementAsync` использует EF Core `ExecuteUpdateAsync` с условием `WHERE Quantity >= requested` в одном атомарном SQL UPDATE — не "прочитать-потом-записать". Ноль затронутых строк → недостаточно остатка. Это устраняет race condition полностью на уровне БД, без пессимистичных блокировок.
- **Идемпотентность**: перед созданием `SaleTransaction` `ProcessSaleCommandHandler` ищет существующую запись по `(StoreId, IdempotencyKey)` (уникальный индекс уже существовал) — если найдена, возвращается результат **той же** транзакции, а не ошибка и не вторая продажа. Проверено вручную: повторный запрос с тем же ключом вернул тот же `SaleTransactionId`, остаток не изменился повторно.
- **Двойной void**: `VoidSaleCommandHandler` проверяет `Status == Voided` до изменения — если уже voided, возвращает исход `AlreadyVoided` (409), ничего не меняя.
- **Транзакционность** (CLAUDE.md §2: "продажа + списание склада — только в одной DB-транзакции"): `IUnitOfWork` дополнен `ExecuteInTransactionAsync` (оборачивает `dbContext.Database.BeginTransactionAsync`/commit/rollback). Внутри `ProcessSaleCommandHandler` используется приватное исключение-сигнал (`InsufficientStockSignal`), которое гарантированно откатывает уже списанные строки предыдущих позиций чека, если одна из последующих позиций не прошла по остатку — без этого приёма частичное списание могло бы закоммититься.

### Новое в Application

- `Application/Abstractions`: `ISaleTransactionRepository`, `IStockLevelRepository` (`TryDecrementAsync`/`IncrementAsync`/`GetByStoreAsync`), `IStockMovementRepository`; расширены `IStoreRepository.GetByIdAsync`, `IPriceEntryRepository.GetLatestForStoreAsync`, `IUnitOfWork.ExecuteInTransactionAsync`.
- `Application/Sales/Commands/ProcessSale/` — `ProcessSaleCommand` принимает только `ProductId`+`Quantity` по позициям; **цена разрешается на сервере** через `GetLatestForStoreAsync` — кассир/клиент не может подделать цену. Результат — `ProcessSaleOutcome` enum (Completed/StoreNotFound/Forbidden/ProductNotFound/PriceNotFound/InsufficientStock) вместо исключений для нормального потока управления.
- `Application/Sales/Commands/VoidSale/` — восстанавливает остаток по каждой позиции чека, пишет `StockMovement` типа `Correction` со ссылкой на исходную продажу.
- `Application/Inventory/Commands/RecordStockReceipt/` — оприходование поставки (апсерт остатка + `StockMovement` типа `Receipt`).
- `Application/Inventory/Queries/GetStockLevel/` — список остатков по магазину.
- **Авторизация в два слоя** (CLAUDE.md §2: "не полагаться только на атрибут"): на уровне endpoint — политика `StorePartner` (`RequireRole`); на уровне use-case — каждый handler сверяет `Store.OwnerUserId` с `UserId` из JWT, при несовпадении — исход `Forbidden` (403), а не просто полагается на роль.

### Infrastructure

- `StockLevelRepository` — `TryDecrementAsync` (условный `ExecuteUpdateAsync`), `IncrementAsync` (`ExecuteSqlInterpolatedAsync` с `INSERT ... ON CONFLICT ("ProductId","StoreId") DO UPDATE` — параметризовано через интерполяцию EF Core, не конкатенация строк, поэтому безопасно от SQL-инъекций).
- **Исправлен реальный пробел**: у `StockLevel` не было уникального индекса на `(ProductId, StoreId)` — потенциально могли существовать две строки остатка для одного товара в одном магазине. Добавлен `StockLevelConfiguration` + миграция `AddStockLevelUniqueIndex` — без этого индекса `ON CONFLICT` в `IncrementAsync` вообще не сработал бы (Postgres требует существующее уникальное ограничение для `ON CONFLICT`).
- `UnitOfWork.ExecuteInTransactionAsync` — оборачивает `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` (на исключении).

### WebApi

Новые эндпоинты, все под политикой `StorePartner`: `POST /api/sales`, `POST /api/sales/{id}/void`, `POST /api/stock/receipts`, `GET /api/stock?storeId=`. `Outcome` enum каждого хендлера маппится в HTTP-статус через `switch`-выражение (Completed/Received/Found/Voided → 200, NotFound → 404, Forbidden → 403, AlreadyVoided/InsufficientStock/PriceNotFound → 409).

### Проверено вручную end-to-end (все 9 шагов плана, не только билд)

Через `psql` пользователю `test@sarfkor.tj` назначена роль `StorePartner`, создан `Store` (Id=2) с ним как `OwnerUserId`, добавлен `PriceEntry` для товара в этом магазине.

1. `POST /api/stock/receipts` (10 шт) → `200`, `GET /api/stock` подтвердил `10`.
2. `POST /api/sales` с 15 шт (> остатка) → `409 "Insufficient stock..."`.
3. `POST /api/sales` с 3 шт → `200`, `saleTransactionId: 1`, `totalAmount: 60.00`; остаток стал `7`.
4. **Повтор того же запроса** (тот же `idempotencyKey: "sale-002"`) → вернул **тот же** `saleTransactionId: 1`, остаток остался `7` (не списался повторно до `4`).
5. `POST /api/sales/1/void` → `200`, остаток восстановился до `10`.
6. **Повторный void** того же `saleTransactionId` → `409 "This sale has already been voided."`.

Побочная деталь при тестировании: кириллический текст в `reason` через `curl` в этой Windows/Git-Bash среде ломал кодировку UTF-8 до того, как достигал сервера (не баг приложения — сервер корректно отверг невалидный UTF-8) — тест повторён с ASCII-текстом, прошёл успешно.

**Итог**: полный цикл кассы (приход → продажа → повторная отправка → возврат → повторный возврат) работает корректно, все три инварианта CLAUDE.md §10 подтверждены реальными запросами против реальной БД, а не только юнит-тестами на бумаге.

**Не сделано (сознательно, вне рамок этой задачи)**: `GetDailySalesReportQuery`/`GetProfitReportQuery`, `CashierShift`, офлайн-синхронизация кассы.

## 2026-07-21 — Дашборд StorePartner: GetStoreDashboardQuery, GetDailySalesReportQuery, GetProfitReportQuery

Снова через `EnterPlanMode` → план → одобрение пользователя → реализация. Read-only модуль отчётности, миграций не потребовалось — все нужные таблицы (`SaleTransaction`, `SaleLineItem`, `CostPrice`, `StockLevel`) уже существовали.

- **Application/Abstractions**: новый `ICostPriceRepository.GetLatestForStoreAsync` (тот же join-паттерн "последняя запись на товар", что и в `PriceEntryRepository` — `GroupBy`+`Max`+`Join`, не `.Select(g => g.First())`); `ISaleTransactionRepository` дополнен `GetCompletedInRangeAsync` (только `Status == Completed` — voided продажи не считаются ни в выручке, ни в прибыли).
- **Три query** (`Application/Sales/Queries/GetDailySalesReport`, `Application/Sales/Queries/GetProfitReport`, `Application/Stores/Queries/GetStoreDashboard`) — у всех одинаковый `Outcome`-паттерн (`Found`/`StoreNotFound`/`Forbidden`), и все три **ограничены строго владельцем магазина** (`Store.OwnerUserId == RequestedByUserId`) — это единственный сейчас механизм, гарантирующий, что `CostPrice`/прибыль не видны никому, кроме владельца (CLAUDE.md §4), пока в проекте нет отдельной суб-роли "кассир" (открытый вопрос §9).
- `GetProfitReportQueryHandler`: если для проданного товара нет `CostPrice` — себестоимость считается 0 для этой позиции (не падает, просто прибыль эквивалентна выручке для неё) — сознательное решение, чтобы владелец видел хоть какой-то отчёт, даже если забыл выставить себестоимость.
- **Infrastructure**: `CostPriceRepository` (join-паттерн), `SaleTransactionRepository.GetCompletedInRangeAsync`, регистрация в DI.
- **WebApi**: три новых `GET`-эндпоинта под политикой `StorePartner` — `/api/stores/{storeId}/dashboard`, `/api/stores/{storeId}/reports/daily-sales?date=`, `/api/stores/{storeId}/reports/profit?from=&to=`. `DateOnly` биндится из query string нативно (ASP.NET Core 7+, без доп. кода).
- **Проверено вручную end-to-end** (реальная продажа 5 шт по цене 20 TJS, себестоимость 8 TJS):
  - `GET /dashboard` → `{"todaySalesCount":1,"todayRevenue":100.00,"productsInStockCount":1}`.
  - `GET /reports/daily-sales?date=<сегодня>` → те же цифры, что и в dashboard (согласованность подтверждена).
  - `GET /reports/profit?from=<сегодня>&to=<сегодня>` → `{"revenue":100.00,"totalCost":40.00,"profit":60.00}` — арифметика проверена вручную: `5 × 20 − 5 × 8 = 60`, сошлось.
  - Зарегистрирован отдельный обычный пользователь (роль `User` по умолчанию, без `StorePartner`) → `GET /dashboard` от его имени вернул `403 Forbidden` — политика на уровне эндпоинта работает, а не только на уровне use-case.

**Не сделано (сознательно)**: детализация отчёта по товарам (сейчас только агрегаты), экспорт CSV/PDF (§4 упоминает, но не в MVP), суб-роль "кассир" для частичного доступа к dashboard без прибыли.

## 2026-07-22 — Оставшиеся use-case'ы CLAUDE.md §6: закрыт MVP + вторая волна

По прямому запросу пользователя ("продолжай, пока не закончишь") реализованы все оставшиеся use-case'ы из технического задания одним заходом — без формального `EnterPlanMode` (архитектурные паттерны уже устоялись за предыдущие итерации), но с тем же уровнем сквозной проверки через `dotnet run` + `curl` + `psql`.

### Реализовано (7 use-case'ов)

- **`GetTopSellingProductsQuery`** (Application/Products/Queries) — публичный, без аутентификации. Опциональный фильтр по магазину.
- **`CompareStoresForShoppingListQuery`** ("собери корзину дешевле") — публичный; для каждого магазина, где есть цена **на все** запрошенные товары (не частичное совпадение), считает сумму корзины, сортирует по возрастанию; расстояние через уже существующий `GeoDistance` (Application/Common).
- **`ReportOutOfStockCommand`** — создаёт `Report` с `Type = OutOfStock`; доступен любому аутентифицированному пользователю.
- **`VerifyReceiptCommand`** — сверяет каждую позицию `Receipt.Lines` с текущей ценой (`IPriceEntryRepository.GetLatestForStoreAsync`); чек помечается `Verified`, только если совпали **все** позиции, иначе `Mismatched`; доступ — только тому, кто загрузил чек; повторная проверка уже обработанного чека → `409 AlreadyProcessed`.
- **`PublishExpiringOfferCommand`** ("Скоро истекает") — создаёт `ExpiringOffer`; валидатор проверяет `DiscountedPrice < OriginalPrice` и `ExpiresAt` в будущем прямо через FluentValidation (`.LessThan`/`.GreaterThan` с кросс-полевым сравнением), без отдельных Outcome-веток для этого.
- **`ModerateNewProductCommand`** (Admin) — превращает `ProductSubmission` в реальный `Product` при одобрении; при повторной модерации уже обработанной заявки → `409 AlreadyModerated`.
- **`ModerateReportCommand`** (Admin) — резолвит/отклоняет `Report`.

### Новое переиспользование существующей инфраструктуры

Оба Admin-хендлера пишут в **уже существовавшую, но неиспользуемую** таблицу `AuditLogs` (`IAuditLogRepository`, добавлен только сейчас) — не пришлось добавлять новую сущность/миграцию для аудита модерации, она была предусмотрена ещё на этапе Domain layer.

### Пробел, найденный и исправленный по ходу: EF Core не смог перевести `GroupBy` после `Join` в SQL

`GetTopSellingProductsAsync` (Infrastructure/Repositories/SaleTransactionRepository.cs) изначально был написан как `SelectMany` по навигационному свойству `SaleTransaction.Lines`, затем `GroupBy(ProductId).Select(Sum(Quantity))` — упало с `InvalidOperationException` при первом ручном тесте (не при билде — LINQ-провайдер EF Core проверяет транслируемость только во время выполнения запроса). Три попытки исправления:
1. Замена `SelectMany` на явный `join` (query syntax) — тот же провал, другой вид того же дерева выражений.
2. Проекция в промежуточный анонимный тип `{ ProductId, Quantity }` перед `GroupBy` — всё ещё падало (EF Core разворачивает/переоптимизирует дерево до трансляции, вне зависимости от промежуточной проекции).
3. **Рабочее решение**: выполнить join+проекцию как отдельный SQL-запрос (`await (...).ToListAsync()`), затем `GroupBy`/`Sum`/`OrderByDescending`/`Take` — полностью в памяти (LINQ-to-Objects). Для отчёта уровня "топ товаров по продажам одного магазина/платформы" это приемлемо по объёму данных и полностью надёжно, в отличие от попыток заставить провайдера транслировать сложную комбинацию `Join`+`GroupBy`+`Sum` в один SQL-запрос.

Практический урок: `dotnet build` не ловит ошибки трансляции LINQ-в-SQL — они всплывают только при реальном выполнении запроса. Это ещё раз подтверждает необходимость сквозной ручной проверки (`dotnet run` + `curl`), а не только билда, для любого нетривиального запроса к БД.

### Проверено вручную end-to-end (все 7 use-case'ов, включая обе ошибки трансляции)

- `GET /api/products/top-selling` → `{"products":[{"productId":1,...,"totalQuantity":5}]}` — **корректно исключает voided-продажу** (5, а не 8 = 3+5, потому что sale-002 на 3 шт был void'нут в предыдущей сессии).
- `GET /api/products/compare-basket?productIds=1` → магазины отсортированы по возрастанию суммы корзины.
- `POST /api/reports/out-of-stock` → `200 {"reportId":1}`.
- `POST /api/receipts/1/verify` → `200`, `matches: true` (цена в чеке совпала с текущей ценой в магазине); повторный вызов → `409 AlreadyProcessed`.
- `POST /api/offers` → `200 {"offerId":1}`.
- `POST /api/admin/products/1/moderate` (approve) → `200 {"productId":2}` (новый `Product` реально создан); повторный вызов → `409 AlreadyModerated`.
- `POST /api/admin/reports/1/moderate` (resolve) → `200`.
- Прямая проверка в БД (`psql`): `AuditLogs` содержит обе записи модерации (`ProductSubmission.Approved`, `Report.Resolved`) с правильными `EntityId`/`Details`.

### Итог по проекту

Все use-case'ы из CLAUDE.md §6 (MVP + вторая волна, кроме `CashierShift` и офлайн-синхронизации, сознательно не включённых в область этой задачи) реализованы, задокументированы и проверены сквозным ручным тестированием против реальной PostgreSQL. Полный список HTTP-эндпоинтов: аутентификация (register/login), потребитель (scan, submit-price, top-selling, compare-basket, report-out-of-stock, verify-receipt), StorePartner (sales, void, stock receipts/level, dashboard, daily-sales/profit reports, offers), Admin (moderate product/report).

## 2026-07-23 — Закрыты 3 явных требования безопасности CLAUDE.md §2: refresh-токен, rate limiting, CORS

По запросу "продолжай" — следующий по значимости пробел был не новый use-case, а требования безопасности, явно перечисленные в CLAUDE.md §2 и №10 ("Rate limiting без конкретных цифр") как обязательные, но ещё не выполненные.

### Refresh-токен endpoint (закрыт прошлый пробел)

- `IRefreshTokenRepository` дополнен `GetByTokenAsync`; `IAuthService` — методом `RefreshAsync`.
- `AuthService` (Infrastructure/Identity) рефакторен: общая логика выпуска пары токенов вынесена в приватный `IssueTokenPairAsync` (переиспользуется в Register/Login/Refresh — раньше была продублирована).
- **Ротация refresh-токенов** (a не просто повторное использование): при `RefreshAsync` старый токен помечается `RevokedAt` + `ReplacedByToken`, выпускается новая пара. Повторное использование уже использованного (revoked) refresh-токена → `401`, а не тихий успех — стандартная защита от кражи токена.
- `POST /api/auth/refresh` — новый эндпоинт, `Application.Identity.Commands.RefreshToken`.
- **Проверено вручную**: `refresh` с валидным токеном → `200`, новая пара; повторный вызов с уже использованным (revoked) токеном → `401`.

### Rate limiting с конкретными цифрами (закрыт критический пробел §10)

Через встроенный `Microsoft.AspNetCore.RateLimiting` (без доп. NuGet-пакета — часть shared framework с .NET 7), пять именованных fixed-window политик:

| Политика | Лимит | Партиция | Эндпоинты |
|---|---|---|---|
| `registration` | 5 / час | IP | `POST /api/auth/register` |
| `login` | 10 / 15 мин | IP | `POST /api/auth/login`, `POST /api/auth/refresh` |
| `scan` | 30 / мин | IP | `GET /api/products/scan`, `GET /api/products/compare-basket` |
| `contributions` | 20 / час | UserId (или IP, если аноним) | `POST /api/prices`, `POST /api/reports/out-of-stock`, `POST /api/receipts/{id}/verify` |
| `sales` | 60 / мин | UserId | `POST /api/sales` (кассир может пробивать чеки часто, лимит щедрее остальных) |

`app.UseRateLimiter()` намеренно размещён **после** `UseAuthentication`/`UseAuthorization` в конвейере — иначе `httpContext.User` был бы пуст на момент выбора партиции для политик `contributions`/`sales`, которые партиционируют по `UserId`, а не по IP.

**Проверено вручную**: 11 подряд неудачных `POST /api/auth/login` → первые (в пределах лимита 10 на 15-минутное окно, с учётом уже сделанных ранее в этой сессии запросов) вернули `401`, остальные — `429 Too Many Requests`.

### CORS — строгий whitelist (CLAUDE.md §2: "не AllowAnyOrigin")

- `Cors:AllowedOrigins` — массив в `appsettings.json` (сейчас placeholder — `http://localhost:3000` и `http://localhost:5173`, стандартные порты React/Vite dev-серверов; **нужно обновить на реальный origin фронтенда**, как только он известен).
- Именованная политика `"Frontend"`: `WithOrigins(...).AllowAnyHeader().AllowAnyMethod().AllowCredentials()` — явный список, никогда `AllowAnyOrigin`.
- **Проверено вручную**: preflight (`OPTIONS`) с `Origin: http://localhost:3000` → `Access-Control-Allow-Origin` присутствует; тот же запрос с `Origin: http://evil.example.com` → заголовок отсутствует (браузер заблокирует чтение ответа на JS-стороне).

**Не сделано (сознательно, следующий шаг после подключения фронтенда)**: обновить `Cors:AllowedOrigins` на реальный домен/порт фронтенда — сейчас там только dev-заглушки.

## 2026-07-24 — Первые автоматические тесты (закрыт пробел CLAUDE.md §10 "нет тестового покрытия")

До этого момента вся проверка была ручной (`curl`+`psql` в текущей сессии) — работает, но ничего не защищает от регрессий при будущих изменениях кода. Добавлен `Backend/Application.Tests` (xUnit), реализующий **буквально** три сценария, которые CLAUDE.md §10 называет обязательными unit-тестами.

- **Новый проект** `Application.Tests` (добавлен в `Backend.slnx`), пакеты: `xunit`, `Moq` (для мокирования репозиториев без реальной БД), `Microsoft.Extensions.Configuration.UserSecrets`. `UserSecretsId` в csproj **специально совпадает** с `WebApi.csproj` — интеграционный тест переиспользует тот же локальный connection string, не хранит пароль в файле.

### `ProcessSaleCommandHandlerTests` (7 тестов, все на моках — без БД)
Проверяет: `StoreNotFound`, `Forbidden` (не владелец), **идемпотентность** (повторный `IdempotencyKey` → возвращает тот же `SaleTransactionId`, `IStockLevelRepository.TryDecrementAsync` и `ISaleTransactionRepository.Add` не вызываются повторно — проверено через `Mock.Verify(..., Times.Never)`), `ProductNotFound`, `PriceNotFound`, `InsufficientStock` (сделка не создаётся), успешный сценарий с правильной суммой.

### `VoidSaleCommandHandlerTests` (4 теста, на моках)
Проверяет: `NotFound`, `Forbidden`, **двойной void** (повторная попытка на уже `Voided`-транзакции → `AlreadyVoided`, `IStockLevelRepository.IncrementAsync` не вызывается — остаток не трогается дважды), успешный void с восстановлением остатка.

### `StockLevelConcurrencyTests` (1 интеграционный тест, **реальный PostgreSQL**)
Единственный сценарий, который нельзя проверить моками — реальная атомарность на уровне БД. Тест вставляет `StockLevel` с остатком 10, запускает **5 параллельных** `TryDecrementAsync` по 3 штуки (суммарно требуется 15 > 10 доступно) через `Task.WhenAll`, затем проверяет: итоговый остаток никогда не отрицательный, и равен ровно `10 − (число успешных попыток × 3)`. Помечен `[Trait("Category", "Integration")]` для будущей фильтрации в CI (когда БД может быть недоступна).

**Результат первого запуска**: `dotnet test` → **12/12 пройдено**, включая интеграционный тест против реальной локальной БД.

**Не сделано**: тесты для остальных 15+ command/query хендлеров (только два самых критичных модуля покрыты); тесты для `AuthService`/JWT; CI-конвейер, который бы запускал эти тесты автоматически при каждом коммите — тесты существуют, но пока выполняются только вручную (`dotnet test`).

## 2026-07-17 (продолжение) — CI-конвейер (закрыт пробел из предыдущей задачи)

Тесты из прошлой задачи выполнялись только вручную — без автоматического запуска они молча устаревают (никто не обязан помнить `dotnet test` перед каждым пушем). Добавлен GitHub Actions workflow, чтобы каждый push/PR сам проверял сборку и тесты.

- **`.github/workflows/backend-ci.yml`**: на каждый `push` в `master`/`Ardasher` и каждый `pull_request` в `master` — поднимает сервис-контейнер `postgres:16`, ставит .NET 10 SDK, `dotnet restore`/`build` всего `Backend.slnx` в конфигурации Release, применяет миграции (`dotnet ef database update`) к чистой CI-базе `sarfkor_ci`, затем `dotnet test Application.Tests` — то есть **включая** интеграционный тест `StockLevelConcurrencyTests` (не только моки), потому что в CI реальный Postgres уже поднят как service container.
- **Исправлен реальный пробел в тесте** для совместимости с CI: `StockLevelConcurrencyTests.CreateDbContext` был жёстко привязан к `dotnet user-secrets` (`AddUserSecrets`), которых в CI-окружении не существует. Добавлен пакет `Microsoft.Extensions.Configuration.EnvironmentVariables` и `.AddEnvironmentVariables()` в цепочку конфигурации (`AddUserSecrets(optional: true)` теперь не обязателен) — локально продолжает работать через user-secrets, в CI строка подключения приходит через переменную окружения `ConnectionStrings__DefaultConnection` (double-underscore — стандартная конвенция ASP.NET Core для вложенных ключей конфигурации).
- **Секреты CI** — connection string, Jwt:Issuer/Audience/Key заданы прямо в workflow как job-level `env` — это допустимо, потому что они существуют только внутри CI-контейнера на время прогона (эфемерная БД `sarfkor_ci`, создаётся и уничтожается вместе с job), не настоящие продакшн-секреты и не попадают в код приложения (появляются как переменные окружения, а не в `appsettings.json`).
- **Проверено локально перед пушем** (нет доступа к самому GitHub Actions из этой сессии, поэтому проверка — максимально приближенная к CI): `dotnet build Backend.slnx --configuration Release` — 0 ошибок; `dotnet test Application.Tests/Application.Tests.csproj --no-build --configuration Release` — **12/12 пройдено**, включая интеграционный тест (реальный локальный PostgreSQL сыграл роль CI-контейнера).

**Не сделано**: сам workflow не запускался на реальном GitHub Actions в рамках этой сессии (нет прямого доступа) — первая проверка произойдёт при следующем `git push`; стоит проверить результат первого прогона и поправить при необходимости (версии `dotnet-ef`/`setup-dotnet` могут повести себя иначе на `ubuntu-latest`, чем локально на Windows). Тесты для остальных 15+ хендлеров по-прежнему не написаны.

## 2026-07-17 (продолжение 2) — Реструктуризация каталогов: src/ и tests/

По запросу пользователя все проекты сгруппированы по назначению: `Backend/{Application,Domain,Infrastructure,WebApi}` → `Backend/src/{...}` (тестовый проект уже был вынесен отдельно на предыдущем шаге в `Backend/Application.Tests`, теперь перемещён в `Backend/tests/Application.Tests`).

- **Перемещение выполнено файл-за-файлом** (`git mv` на каждый отслеживаемый файл, не на директорию целиком) — прямой `git mv Application src/Application` падал с `Permission denied`: несколько фоновых `dotnet.exe`/MSBuild build-server процессов держали файл-хендлы внутри каталога (подтверждено через `tasklist`; `dotnet build-server shutdown` не полностью помогло). Файловая история Git сохранена для каждого файла.
- **Найден и исправлен реальный побочный эффект перемещения**: у `Infrastructure.csproj` и `WebApi.csproj` после перемещения пропал целый блок `<ItemGroup>` с `<ProjectReference>` (скорее всего фоновый процесс в IDE — OmniSharp/C# Dev Kit — во время самого перемещения детектировал временно нерезолвящиеся ссылки на переехавшие проекты и тихо "почистил" их). Обнаружено сравнением текущего содержимого с последним закоммиченным (`git show <commit>:path`), не с `diff` напрямую — обычный `diff` через Git Bash ложно показывал построчные различия из-за CRLF/LF несовпадения между блобом и рабочим деревом, что едва не привело к ложной тревоге по всем 5 csproj-файлов. Оба файла восстановлены вручную с правильными относительными путями.
- **Обновлены пути в трёх местах**, зависящих от структуры каталогов:
  - `Backend.slnx` — все `<Project Path="...">` указывают на `src/...`/`tests/...`.
  - `Backend/tests/Application.Tests/Application.Tests.csproj` — три `ProjectReference` изменены с `..\X\X.csproj` на `..\..\src\X\X.csproj` (тестовый проект теперь на один уровень глубже относительно `src/`, тогда как ссылки **между** `src`-проектами друг на друга не изменились — они переехали синхронно на одном уровне).
  - `.github/workflows/backend-ci.yml` — пути к `Infrastructure.csproj`/`WebApi.csproj` (миграции) и `Application.Tests.csproj` (тесты) обновлены на `src/`/`tests/`.
- **Проверено полностью после реструктуризации**: `dotnet build Backend.slnx` — 0 ошибок; `dotnet test tests/Application.Tests/Application.Tests.csproj` — **12/12 пройдено**; `dotnet ef migrations has-pending-model-changes` — модель и миграции по-прежнему синхронны (миграции физически переехали вместе с `Infrastructure`, EF Core их находит без проблем).

**Не сделано**: изменения ещё не закоммичены — ждут явного запроса пользователя на commit/push (135+ файлов пришлось переместить, стоит дать пользователю проверить diff перед тем, как это уйдёт в git).

## 2026-07-17 (продолжение 3) — Тестпокрытие расширено на все оставшиеся 17 handler'ов

После честной оценки готовности (по запросу "Is Aplication and Domain 100% ready and correct") был назван конкретный пробел: только 2 из 19 handler'ов (`ProcessSale`, `VoidSale`) имели automated-тесты, остальные 17 проверялись только вручную (`curl`/`psql`) в прошлых сессиях — без защиты от регрессий. Закрыт этим шагом полностью — на каждый handler, у которого его не было, добавлен тестовый файл (все на моках, без БД, кроме уже существовавшего `StockLevelConcurrencyTests`).

Добавлено 17 новых тестовых файлов в `Backend/tests/Application.Tests/` (плоско, без подпапок — согласно решению пользователя о структуре тестов):

- **Identity** (`RegisterCommandHandlerTests`, `LoginCommandHandlerTests`, `RefreshTokenCommandHandlerTests`) — тонкие обёртки над `IAuthService`, тесты проверяют делегирование и null-сценарий (занятый email/неверный пароль/отозванный токен).
- **Feedback** (`ModerateReportCommandHandlerTests`, `ReportOutOfStockCommandHandlerTests`) — NotFound/AlreadyModerated/Resolve/Reject с проверкой записи в `AuditLog` через `Mock.Verify`.
- **Products** (`ModerateNewProductCommandHandlerTests`) — Approve реально создаёт `Product` и возвращает его Id; Reject — не создаёт.
- **Receipts** (`VerifyReceiptCommandHandlerTests`) — 6 сценариев: NotFound/Forbidden/AlreadyProcessed/MissingStore/Verified (все позиции совпали)/Mismatched (хотя бы одна разошлась) — самый разветвлённый handler из всех непокрытых.
- **Offers** (`PublishExpiringOfferCommandHandlerTests`), **Inventory** (`RecordStockReceiptCommandHandlerTests`) — стандартный паттерн StoreNotFound/Forbidden/ProductNotFound/успех, с проверкой конкретных вызовов репозиториев (`IncrementAsync`, `StockMovementType.Receipt`).
- **Отчётность StorePartner** (`GetStockLevelQueryHandlerTests`, `GetDailySalesReportQueryHandlerTests`, `GetProfitReportQueryHandlerTests`, `GetStoreDashboardQueryHandlerTests`) — включая арифметику (`GetProfitReport`: явный тест на то, что отсутствующий `CostPrice` трактуется как 0, а не падает).
- **Products/Pricing** (`ScanBarcodeQueryHandlerTests`, `CompareStoresForShoppingListQueryHandlerTests`, `GetTopSellingProductsQueryHandlerTests`, `SubmitPriceUpdateCommandHandlerTests`) — включая проверку сортировки по расстоянию (Haversine через `GeoDistance`) и по цене, и то, что `CompareStoresForShoppingList` исключает магазины с **неполным** совпадением товаров.

Все 17 handler'ов и их зависимости (Domain entities, Abstractions-интерфейсы) прочитаны заново перед написанием тестов, а не по памяти из прошлых сессий — сигнатуры конструкторов/методов подтверждены чтением актуального кода.

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **65/65 пройдено** (12 старых + 53 новых), все тесты прошли с первого запуска, ни одной правки после написания не понадобилось.

**Не сделано**: тесты для `AuthService`/`JwtTokenGenerator` (Infrastructure layer, не Application — вне текущего проекта тестов, потребует отдельный `Infrastructure.Tests` с реальным Identity/EF Core); изменения ещё не закоммичены.

## 2026-07-18 — Загрузка чека (`UploadReceiptCommand`) + инцидент с потерей appsettings при реструктуризации

### Инцидент: `appsettings.json`/`appsettings.Development.json` удалены при реструктуризации src/tests

При зачистке старых пустых каталогов (`Application`, `Domain`, `Infrastructure`, `WebApi` — после того как все отслеживаемые git файлы уже были перемещены в `src/`) команда `rm -rf` затронула и `appsettings.json`/`appsettings.Development.json` — они не отслеживаются git (в `.gitignore`), поэтому цикл `git mv` по `git ls-files` их не перемещал, и они остались лежать в старой папке `WebApi/`, откуда `rm -rf` их безвозвратно удалил. Это повторение того же класса инцидента, что уже случался раньше в этом проекте (тогда — из-за `git checkout`/`merge`), на этот раз — из-за прямого `rm -rf` без предварительной проверки на нетрекаемые файлы.

**Ущерб оценён и оказался нулевым**: `dotnet user-secrets list` подтвердил, что реальные значения (`ConnectionStrings:DefaultConnection` с настоящим паролем, `Jwt:Key`) целы — они хранятся вне рабочего дерева git (`%APPDATA%`), физическое удаление файлов в репозитории их не затрагивает. Сами `appsettings.json`/`appsettings.Development.json` по документированной истории (WORKLOG за 2026-07-17) никогда не содержали ничего, кроме плейсхолдеров (`Password=CHANGE_ME`, `CHANGE_ME_INSECURE_PLACEHOLDER_KEY`). Оба файла восстановлены вручную (структура `Logging`/`AllowedHosts` — из последней закоммиченной версии в git-истории до `git rm --cached`; секции `ConnectionStrings`/`Jwt`/`Cors` — реконструированы по чтению `Program.cs` и документации в WORKLOG, так как в git их committed-версии никогда не существовало). Восстановление подтверждено реальным запуском сервера (`dotnet run` поднялся и корректно прочитал секреты из `user-secrets`).

**Урок на будущее**: перед любым `rm -rf` на каталог, который недавно был источником `git mv`, нужно явно проверить `find <каталог> -type f` (не полагаться на то, что "все файлы уже перемещены", если в `.gitignore` могут быть нетрекаемые, но при этом не мусорные файлы).

### `UploadReceiptCommand` — закрыт пробел CLAUDE.md §2 "загрузка файлов" для чеков

До этого момента `VerifyReceiptCommand` (реализован 2026-07-22) не имел входной точки — `Receipt` с реальным изображением ниоткуда не появлялся, кроме ручных вставок через `psql` в прошлых сессиях. Реализована загрузка: пользователь отправляет фото чека + вручную размеченные позиции (товар/количество/цена — OCR вне рамок этой задачи), сервер создаёт `Receipt` в статусе `Pending`, который затем можно проверить через уже существующий `VerifyReceiptCommand`.

- **`Application/Receipts/Commands/UploadReceipt/`** — `UploadReceiptCommand` (UserId, StoreId?, ImageReference, Lines), валидатор (Lines не пусты, каждая позиция: Quantity>0, Price>0, Currency — 3 буквы), хендлер (создаёт `Receipt` + `ReceiptLineItem[]`, статус `Pending`).
- **`IReceiptRepository`** дополнен методом `Add` (раньше был только `GetByIdAsync` — сверка чека была, а создания не было).
- **Безопасность загрузки файла — все три требования CLAUDE.md §2 буквально**:
  1. **Проверка типа файла по содержимому, а не по расширению/заголовку**: сервер читает первые 8 байт и сверяет с magic bytes JPEG (`FF D8 FF`) / PNG (`89 50 4E 47 0D 0A 1A 0A`) — `Content-Type`, присланный клиентом, полностью игнорируется как источник истины (его легко подделать).
  2. **Ограничение размера**: 5 МБ, проверяется до чтения файла в память.
  3. **Хранение вне веб-корня**: файл сохраняется в `ContentRootPath/App_Data/receipts/` (настраивается через `Storage:ReceiptsPath`), а не в `wwwroot` — у приложения даже нет `wwwroot` и не подключён `UseStaticFiles`, то есть загруженные файлы физически недоступны по HTTP ни при каких условиях.
  4. **Имя файла на диске генерируется сервером** (`Guid.NewGuid() + расширение_по_magic_bytes`), клиентское имя файла нигде не используется — исключает path traversal и перезапись чужих файлов.
- **Побочная находка при первом ручном тесте**: ASP.NET Core Minimal API начиная с .NET 8 автоматически требует antiforgery-middleware для любого эндпоинта, принимающего `IFormFile`/форму, даже если само приложение никогда не вызывало `AddAntiforgery()` — упало с `InvalidOperationException` при первом запросе. Исправлено явным `.DisableAntiforgery()` на эндпоинте — обоснованно, а не как обход защиты: сам CLAUDE.md §2 говорит "CSRF: antiforgery токены при cookie-сессиях; при JWT — строгая CORS-политика", а это API — чисто JWT Bearer без cookie-сессий, значит antiforgery здесь архитектурно не тот механизм защиты (CORS whitelist уже настроен отдельно, см. запись за 2026-07-23).
- **Эндпоинт**: `POST /api/receipts/upload` (multipart/form-data: `file`, `storeId`, `linesJson`), `RequireAuthorization()` + `RequireRateLimiting("contributions")` (тот же лимит 20/час, что у `SubmitPriceUpdate`/`VerifyReceipt`/`ReportOutOfStock` — все пользовательские contribution-эндпоинты используют одну политику).
- **Тест** (`UploadReceiptCommandHandlerTests`, на моках) — проверяет, что создаётся `Receipt` с правильными `UserId`/`StoreId`/`ImageReference`/статусом `Pending` и что позиции с `ProductId: null` (нераспознанный товар) и заполненным `RecognizedName` сохраняются корректно.
- **Проверено вручную end-to-end** (`dotnet run` + `curl`, 4 сценария, не только билд):
  1. Реальный JPEG (корректные magic bytes) → `200 {"receiptId":2}`; файл реально лежит на диске как `App_Data/receipts/<guid>.jpg` (проверено `find`), `wwwroot` у проекта не существует вовсе.
  2. Текстовый файл с расширением `.jpg` и заголовком `Content-Type: image/jpeg` (подделка) → `400 "Unsupported file type"` — magic-byte проверка сработала, несмотря на то что клиент врал и в расширении, и в заголовке.
  3. Файл 6 МБ → `400 "File is empty or exceeds the 5 MB limit"`.
  4. Запрос без JWT → `401`.
  5. Итоговая проверка целостности потока: `POST /api/receipts/2/verify` сразу после загрузки вернул `200` с корректным сравнением цены из чека (12.50) и текущей цены в БД (20.00) — `matches: false`, `outcome: Mismatched` — подтверждает, что весь путь Upload → Verify работает на реальных данных, а не только по отдельности.

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **66/66 пройдено** (65 + новый `UploadReceiptCommandHandlerTests`).

**Не сделано (сознательно)**: аналогичная загрузка фото товара (`ProductImage`) — тот же паттерн, но для Admin/StorePartner, не реализована в этом заходе; OCR распознавания позиций чека нет (пользователь размечает вручную); удаление/ротация старых файлов на диске (retention policy) не продумана.

## 2026-07-18 (продолжение) — Три реальных функциональных разрыва закрыты: CreateStore, SetCostPrice, GetExpiringOffers

Честная ревизия (по вопросу "did you finish all cruds?") показала: проект намеренно не строится как generic CRUD по всем 55 Domain-сущностям (CLAUDE.md §6 — конкретный список use-case'ов, не спецификация CRUD), и 39 из 55 сущностей вообще не задействованы в Application layer — это осознанно **не** стало задачей (пользователь согласился не трогать спекулятивные сущности вроде `LoyaltyProgram`/`GiftCard`/`Supplier`, добавленные в давних сессиях без опоры на CLAUDE.md). Но среди используемых сущностей нашлись три места, где реально построенная функциональность оказывалась бесполезной без парной операции:

1. **`Store` нельзя было создать через API** — все магазины во всех предыдущих сессиях заводились вручную через `psql`, у StorePartner не было самостоятельного онбординга.
2. **`CostPrice` нельзя было установить через API** — `GetProfitReportQuery` (прибыль) существовал и работал, но данные для него класть было некуда, кроме прямых вставок в БД.
3. **`ExpiringOffer` можно было только публиковать, но не прочитать** — `PublishExpiringOfferCommand` существовал, `GetExpiringOffersQuery` — нет; акция создавалась и была невидима навсегда.

### `CreateStoreCommand`

- `IStoreRepository` дополнен `Add`; `IAuthService` дополнен `AssignRoleAsync(userId, role, ct)` — реализован через `UserManager.IsInRoleAsync`/`AddToRoleAsync` (идемпотентно: повторный вызов не падает и не дублирует роль).
- Хендлер создаёт `Store` с `OwnerUserId` из JWT-claim'а и **тут же присваивает роль `StorePartner`** — самостоятельный онбординг в одно действие: любой аутентифицированный `User` может завести магазин и стать партнёром.
- **Известное и задокументированное ограничение JWT**: только что созданный магазин виден в БД сразу, но токен пользователя, которым он был создан, ещё содержит старый claim `role: User` — политика `RequireAuthorization("StorePartner")` на других эндпоинтах отклонит его с `403`, пока он не сделает `POST /api/auth/refresh` за новым токеном. Это не баг, а стандартное поведение JWT (роль в токене — снимок на момент выдачи), явно прокомментировано в коде хендлера.
- `POST /api/stores` — `RequireAuthorization()` (любой залогиненный, не только `StorePartner` — иначе никто не смог бы создать первый магазин) + `RequireRateLimiting("contributions")`.

### `SetCostPriceCommand`

- `ICostPriceRepository` дополнен `Add`. Паттерн — append-only история, как у `PriceEntry` (не update-in-place): новая запись с `EffectiveFrom = UtcNow`, уже существующий `GetLatestForStoreAsync` (join по `MAX(EffectiveFrom)`) сам подхватывает самую свежую.
- Тот же двухуровневый auth-паттерн, что и у `RecordStockReceiptCommand`: `Store.OwnerUserId == PerformedByUserId`, иначе `Forbidden` — себестоимость чужого магазина недоступна для записи (CLAUDE.md §4: `CostPrice` строго ограничена).
- `POST /api/stock/cost-price` — `RequireAuthorization("StorePartner")`.

### `GetExpiringOffersQuery`

- `IExpiringOfferRepository` дополнен `GetActiveAsync(storeId?, asOf, ct)` — фильтр `ExpiresAt > asOf`, опционально по магазину.
- Хендлер — тот же join-паттерн, что в `ScanBarcodeQuery`/`CompareStoresForShoppingListQuery`: подтягивает `Product`/`Store` по batch-`GetByIdsAsync`, считает расстояние через уже существующий `GeoDistance`, сортирует по расстоянию затем по сроку истечения. Публичный, без аутентификации — потребитель должен видеть акции без логина.
- `GET /api/offers/expiring?storeId=&lat=&lng=` — `RequireRateLimiting("scan")` (та же политика, что у других публичных read-эндпоинтов).

### Тесты и находка в процессе (не в проде — в собственном тесте)

Три новых файла тестов (`CreateStoreCommandHandlerTests`, `SetCostPriceCommandHandlerTests`, `GetExpiringOffersQueryHandlerTests`). При первом прогоне упал `Handle_NoActiveOffers_ReturnsEmptyList` — `ArgumentNullException` в `ToDictionary`. Причина: тест мокировал `GetActiveAsync` на пустой список, но не мокировал `GetByIdsAsync`, и Moq по умолчанию возвращает `null` для нестабленных вызовов (а не пустую коллекцию) — хендлер получал `null.ToDictionary()`. Проверено, что это исключительно артефакт мока: реальные `ProductRepository`/`StoreRepository.GetByIdsAsync` используют `.ToListAsync()`, который на пустом запросе физически не может вернуть `null`. Тест исправлен явным мокированием `GetByIdsAsync` → `[]`.

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **74/74 пройдено**. Миграции не потребовались (все три use-case'а используют уже существующие таблицы).

### Проверено вручную end-to-end (реальный `dotnet run` + `curl`, не только тесты)

1. `POST /api/auth/register` → новый пользователь, роль `User`.
2. `POST /api/stores` → `200 {"storeId":3}`.
3. `POST /api/stock/cost-price` со старым (дорефрешевым) токеном → `403` — подтверждает задокументированное ограничение JWT.
4. `POST /api/auth/refresh` → новый токен, JWT payload проверен вручную (base64-декод) — `"role":["User","StorePartner"]`, роль реально присвоилась в БД.
5. `POST /api/stock/cost-price` с новым токеном, для своего магазина (id=3) → `200 {"outcome":0,"costPriceId":2}`.
6. `POST /api/stock/cost-price` для **чужого** магазина (id=2, из прошлых сессий) → `403` — проверка владения сработала.
7. `GET /api/offers/expiring` без `Authorization` заголовка → `200` — подтверждён публичный доступ.
8. `POST /api/offers` (публикация акции для нового магазина) → `200 {"offerId":2}`.
9. Повторный `GET /api/offers/expiring` → в списке появилась и старая акция из прошлых сессий (id=1, ещё не истекла), и только что опубликованная (id=2) — разрыв "write-only" подтверждённо закрыт.

**Не сделано (сознательно)**: остальные 37 неиспользуемых Domain-сущностей (`LoyaltyProgram`, `GiftCard`, `Supplier`, `ShoppingList`, `Review` и т.д.) намеренно не тронуты — пользователь подтвердил, что строить для них CRUD без опоры на явную бизнес-потребность было бы повторением уже осознанной в этом проекте ошибки (спекулятивное расширение модели).

## 2026-07-18 (продолжение 2) — Три пункта из "честной оценки готовности": HSTS, /health, детект аномалий кассира

По запросу "do what need my project" — выбраны три конкретных, выполнимых в рамках кода пункта из ранее названного списка недоделанного (HTTPS/HSTS, мониторинг, мошенничество кассира), без затрагивания того, что требует бизнес-решения (платёжный провайдер, фискализация) или внешней инфраструктуры (полноценный APM/Grafana, нагрузочное тестирование).

### HSTS в проде (закрыт пробел CLAUDE.md §2 "HTTPS everywhere, HSTS в проде")

`app.UseHsts()` добавлен в `Program.cs` внутри `else`-ветки от `if (app.Environment.IsDevelopment())` — то есть строго вне Development, как того требует сам ASP.NET Core (HSTS на локальном HTTP-дев-сервере бессмыслен и мешает отладке). Без этого браузер не заставляли запоминать "только HTTPS для этого домена" — оставалась пусть узкая, но реальная возможность SSL-stripping между `UseHttpsRedirection()`-редиректами.

### `GET /health` — фундамент для мониторинга (CLAUDE.md §10 "нужен дашборд состояния системы")

Простой health-check без сторонних пакетов (сознательно не подключён `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` — лишняя зависимость на preview-стеке EF Core ради небольшого удобства не стоит риска несовместимости версий): эндпоинт напрямую вызывает `dbContext.Database.CanConnectAsync()`, возвращает `200 {"status":"healthy",...}` или `503`. Без аутентификации (стандартно для health-проб — их дёргают балансировщики/аптайм-мониторы, не люди) и без внутренних деталей в ответе (не течёт ничего, кроме факта "жив/не жив"). Сам полноценный дашборд (Grafana и т.п.) — по-прежнему вне охвата этой сессии, нужна внешняя инфраструктура, но теперь есть, что ему опрашивать.

### `GetCashierAnomalyReportQuery` — первая эвристика против мошенничества кассира (CLAUDE.md §10 "алерты на аномальные паттерны отмен")

- `ISaleTransactionRepository` дополнен `GetAllInRangeAsync` (в отличие от уже существующего `GetCompletedInRangeAsync` — берёт **и** `Completed`, **и** `Voided`, потому что для детекта аномалий важна именно доля отмен, а не только выручка).
- Хендлер группирует все продажи магазина за период по `CashierUserId`, считает `VoidRate = VoidedSales / TotalSales`, помечает `IsAnomalous = true`, если **одновременно**: `VoidRate > 20%` **и** `TotalSales >= 5` — вторая часть условия принципиальна и явно закомментирована в коде: без минимального размера выборки один отменённый чек кассира-новичка (100% void rate на выборке из 1) даёт заведомо бессмысленный "красный флаг".
- Явно закомментировано и в коде, и здесь: это **простейшая отправная эвристика**, а не откалиброванная модель — CLAUDE.md сам называет это направление нерешённым ("рассмотреть периодическую сверку... алерты на аномальные паттерны"), а не готовым к внедрению алгоритмом; порог 20%/выборка 5 не проверялись на реальных данных, потому что реальных данных о нормальном поведении кассиров пока не существует.
- `GET /api/stores/{storeId}/reports/cashier-anomalies?from=&to=` — `RequireAuthorization("StorePartner")`, тот же паттерн владения, что у `GetProfitReport`/`GetDailySalesReport`.
- **Тест на конкретно то, ради чего фича писалась** (`GetCashierAnomalyReportQueryHandlerTests`, 5 тестов): высокий void-rate с достаточной выборкой → `IsAnomalous: true`; высокий void-rate, но выборка меньше порога → `IsAnomalous: false` (не "плачет волк" на шуме); нормальный void-rate → не помечен.

### Проверено вручную end-to-end (реальный `dotnet run` + `curl`)

1. `GET /health` → `200 {"status":"healthy",...}`.
2. Полный цикл: регистрация → `CreateStore` (id=4) → `SubmitPriceUpdate` → `RecordStockReceipt` → 2 продажи → `VoidSale` на одну из них → `GET /reports/cashier-anomalies` вернул **реальные вычисленные цифры**: `totalSales: 2, voidedSales: 1, voidRate: 0.5, isAnomalous: false` — 50% void rate, но `isAnomalous: false`, потому что выборка (2) меньше минимальной (5) — эвристика сработала ровно так, как задумана, не только в тесте на моках, но и на реальных данных из PostgreSQL.
3. Запрос отчёта по чужому магазину (id=2) → `403` — проверка владения сработала.

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **79/79 пройдено**. Миграции не потребовались.

**Не сделано (сознательно, вне охвата кода)**: полноценный мониторинг/alerting (Grafana/Prometheus и т.п. — нужна внешняя инфраструктура), нагрузочное тестирование (нужны инструменты вроде k6/JMeter и стенд, не только код), токенизация платежей (нерешённый вопрос выбора провайдера, CLAUDE.md §9), фискализация и PDPA (юридические вопросы, не код), CI на реальном GitHub Actions по-прежнему не подтверждён (нет доступа к `gh`/Actions API в этой среде).

## 2026-07-18 (продолжение 3) — "Активируй все Entity" — Batch 1/9: справочники каталога

По прямому запросу пользователя ("Hammai Entitihoro faol kun" — активируй все сущности) начата многозаходная работа по подключению оставшихся 39 неиспользуемых Domain-сущностей к Application/WebApi. Учитывая масштаб и явную рекомендацию не строить бездумный generic CRUD (согласовано с пользователем через `AskUserQuestion` — выбран вариант "настоящие бизнес-операции для каждой, качественно, пусть и долго"), работа разбита на 9 тематических батчей; ниже — первый.

### Batch 1: `Brand`, `Category`, `TaxRate`, `Supplier`

Эти четыре — общеплатформенные справочники, на которые уже ссылается `Product` (`CategoryId`, `BrandId`, `TaxRateId` — по внешнему ключу) и `StockMovement` (`SupplierId`), но создать запись в них раньше можно было только вручную через `psql` — тот же класс проблемы, что был у `Store`/`CostPrice` до предыдущего шага.

- `Brand`/`Category`/`TaxRate` — создаются только `Admin` (курируемая централизованно таксономия, чтобы разные StorePartner не плодили дубликаты категорий); `Supplier` — создаётся любым `StorePartner` (поставщики — операционная договорённость конкретного партнёра, не платформенная таксономия).
- `Category` поддерживает иерархию (`ParentCategoryId`) — `CreateCategoryCommand` явно проверяет существование родителя (`CategoryNotFound`, если ссылка на несуществующий id), а не полагается на FK-ограничение БД молча упасть.
- Все четыре `Get*Query` — публичные списки без пагинации (объём справочников мал по своей природе), `GetSuppliers` — единственный из четырёх, что закрыт `RequireAuthorization("StorePartner")` (список поставщиков — операционная информация партнёров, не публичная витрина, в отличие от брендов/категорий/налоговых ставок, нужных фронтенду для дропдаунов при просмотре товаров).
- Добавлены 4 репозитория (`IBrandRepository`, `ICategoryRepository`, `ITaxRateRepository`, `ISupplierRepository`) — `DbSet<T>` для всех четырёх уже существовали в `AppDbContext` с самого начала (Domain-сущности были, просто не задействованы), миграций не потребовалось.

### Проверено вручную end-to-end

Через `psql` тестовому пользователю выдана роль `Admin` (тот же приём, что применялся для `StorePartner` в прошлых сессиях):
1. `POST /api/catalog/brands` (Admin) → `200 {"brandId":2}`; `GET /api/catalog/brands` (без токена) → оба бренда в списке.
2. `POST /api/catalog/categories` без родителя → `200`; с несуществующим `parentCategoryId: 999` → `404`.
3. `POST /api/catalog/tax-rates` (Admin) → `200`; `GET /api/catalog/tax-rates` (публично) → в списке.
4. `POST /api/suppliers` от имени `Admin` (без роли `StorePartner`) → `403` — разделение ролей подтверждено; тот же запрос от `StorePartner` (после создания магазина и `refresh` за новой ролью — тот же паттерн JWT, что и в `CreateStore`) → `200`; `GET /api/suppliers` без токена → `401`.

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **85/85 пройдено** (+6 новых тестов на 4 handler'а create-команд).

**Следующие батчи** (запланированы, не начаты): Consumer engagement (ShoppingList/Favorite/Review/PriceAlert/DeviceToken/Notification), Loyalty/CRM, Supply chain (PurchaseOrder/StockTransfer/ReorderRule/ProductBundle), POS extras (CashierShift/SaleReturn), Disputes, Identity/security (UserProfile/UserConsent/SecurityEvent/StoreEmployee), Promotion/Scan/Commission. `FiscalReceipt` сознательно остаётся нетронутым — CLAUDE.md §9 прямо называет фискализацию нерешённым юридическим блокером, реализация без ответа на этот вопрос была бы бесполезной или вредной (создала бы иллюзию готовности к реальным чекам).

## 2026-07-18 (продолжение 4) — "Активируй все Entity" — Batch 2/9: consumer engagement

### `ShoppingList`+`ShoppingListItem`, `Favorite`, `Review`+`ReviewReply`, `PriceAlert`, `DeviceToken`, `Notification`

16 use-case'ов на 6 сущностей — по возможности реальные операции, не generic CRUD:

- **ShoppingList**: `CreateShoppingListCommand`, `AddShoppingListItemCommand`/`RemoveShoppingListItemCommand` (оба с проверкой владения — список принадлежит только создавшему его пользователю), `GetShoppingListsQuery` (с вложенными позициями).
- **Favorite**: `AddFavoriteCommand` идемпотентен (повторное добавление того же `(UserId, Type, EntityId)` возвращает существующий `Id`, не плодит дубликаты), `RemoveFavoriteCommand`, `GetFavoritesQuery`.
- **Review/ReviewReply**: `SubmitReviewCommand` (рейтинг 1–5), `GetReviewsQuery` (публичный), `ReplyToReviewCommand` — единственный нетривиальный: ответить может только владелец магазина, к которому привязан отзыв (`Review.StoreId`); если у отзыва нет магазина (`StoreId == null`) — отвечать física некому, возвращается `Forbidden`, а не молчаливое разрешение.
- **PriceAlert**: `CreatePriceAlertCommand`, `GetPriceAlertsQuery`, `DeactivatePriceAlertCommand` (с владением). Сам механизм триггера алерта при падении цены (фоновая задача, сверяющая `PriceAlert.TargetPrice` с новыми `PriceEntry`) — вне охвата этого батча, реализован только жизненный цикл записи.
- **DeviceToken**: `RegisterDeviceTokenCommand` — идемпотентный upsert по значению токена (не по пользователю): один и тот же физический токен устройства при переустановке приложения/смене пользователя на одном устройстве обновляет существующую строку, а не накапливает дубликаты.
- **Notification**: `GetNotificationsQuery`, `MarkNotificationAsReadCommand` (с владением). Ничего в системе пока не создаёт `Notification` автоматически (низкий остаток, падение цены и т.д. — будущая интеграция с уже существующими use-case'ами типа `RecordStockReceipt`/`SubmitPriceUpdate`); в этом батче сделан только API для чтения/квитирования уже созданных записей.

### Два реальных бага, найденных ручной проверкой (не тестами и не билдом)

1. **`DELETE /api/favorites` ронял весь процесс на старте** (`InvalidOperationException: Body was inferred but the method does not allow inferred body parameters`) — Minimal API в ASP.NET Core не разрешает неявно выводимое тело запроса (JSON body) для `MapDelete`, в отличие от `MapPost`/`MapPut`. Эндпоинт получал сложный DTO (`FavoriteRequest`) как тело, что для `DELETE` запрещено фреймворком. **Это падение — на старте приложения**, не на конкретном запросе: `dotnet build` прошёл чисто (0 ошибок), юнит-тесты прошли (116/116, они не касаются регистрации маршрутов), и только реальный `dotnet run` вскрыл проблему, потому что ASP.NET Core строит дерево эндпоинтов лениво при первом обращении к `EndpointDataSource` (тут — при инициализации `AuthorizationPolicyCache`). Исправлено: `type`/`entityId` теперь читаются из query-string (`DELETE /api/favorites?type=Product&entityId=5`), а не из тела.
2. **Enum-поля в теле запроса принимали только числа, а не строки** (`{"type":"Product"}` падало с `JsonException`, потому что System.Text.Json по умолчанию сериализует enum как целое число) — это первый раз в проекте, когда клиент вообще передаёт enum-значение в теле запроса (`FavoriteRequest.Type`, `RegisterDeviceTokenRequest.Platform`), поэтому проблема не всплывала раньше. Исправлено глобально, а не точечно: `builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()))` — теперь **все** enum по всему API (включая уже существующие, если где-то появится похожий сценарий) сериализуются/десериализуются как читаемые строки (`"Product"`, `"Android"`), а не magic numbers.

**Практический урок, дополняющий запись от 2026-07-22** ("EF Core не ловит ошибки трансляции LINQ на этапе сборки"): здесь та же категория проблемы, но на уровне маршрутизации ASP.NET Core — `dotnet build` и unit-тесты не гарантируют, что приложение вообще способно **запуститься**. Только реальный `dotnet run` + ручной прогон каждого нового эндпоинта ловит такие ошибки, что ещё раз подтверждает необходимость сквозной проверки, а не только "зелёных" тестов.

### Проверено вручную end-to-end (все 16 эндпоинтов, включая оба найденных бага и их исправления)

Полный цикл через `dotnet run` + `curl`: `CreateShoppingList` → `AddShoppingListItem` → `GetShoppingLists` (позиция реально вложена); `AddFavorite` со строковым enum → `GetFavorites` → повторный `AddFavorite` (тот же id, не дубликат) → `RemoveFavorite` через query-string → `GetFavorites` пуст; `SubmitReview` → `GetReviews` публично; `ReplyToReview` от не-владельца магазина → `403`; `CreatePriceAlert` → `GetPriceAlerts` → `DeactivatePriceAlert` → повторный `GetPriceAlerts` подтверждает `isActive: false`; `RegisterDeviceToken` со строковым enum `"Android"`; `GetNotifications` — пусто (ожидаемо, ничего пока не генерирует уведомления).

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **116/116 пройдено** (+31 новый тест на 16 handler'ов). Миграции не потребовались — все `DbSet<T>` для этих 6 сущностей существовали в `AppDbContext` с самого начала.

**Не сделано (сознательно, вне охвата батча)**: автоматическое создание `Notification` при падении цены/низком остатке (нужна интеграция с существующими `SubmitPriceUpdate`/`RecordStockReceipt`, следующий логический шаг, но не часть "активации" самой сущности); удаление/список device-токенов (только регистрация); пагинация у `GetReviews`/`GetNotifications` (объём пока не требует).

## 2026-07-18 (продолжение 5) — "Активируй все Entity" — Batch 3/9: Loyalty/CRM

### `Customer`, `LoyaltyProgram`+`LoyaltyAccount`+`LoyaltyTransaction`, `GiftCard`, `StoreCredit`

14 use-case'ов, самый архитектурно-нетривиальный батч из всех активированных сущностей — три уровня владения на цепочке `LoyaltyAccount → LoyaltyProgram → Store.OwnerUserId`.

- **Customer**: `CreateCustomerCommand` идемпотентен по номеру телефона (общий реестр клиентов между магазинами — один и тот же человек может быть клиентом нескольких магазинов), `GetCustomerByPhoneQuery` — доступ только `StorePartner` (номер телефона — персональные данные, не публичный поиск).
- **LoyaltyProgram**: `CreateLoyaltyProgramCommand` — один магазин, одна программа (`AlreadyExists`, если уже создана), владение через `Store.OwnerUserId`. `GetLoyaltyProgramQuery` — публичный (клиент должен видеть условия программы до участия).
- **LoyaltyAccount/LoyaltyTransaction** — самое сложное место батча: `EarnLoyaltyPointsCommand`/`RedeemLoyaltyPointsCommand` проверяют владение не напрямую (у `LoyaltyAccount` нет `OwnerUserId`), а по цепочке `LoyaltyAccount.LoyaltyProgramId → LoyaltyProgram.StoreId → Store.OwnerUserId` — три последовательных запроса на чтение перед единственной записью. `RedeemLoyaltyPointsCommand` проверяет достаточность баланса **до** списания (`InsufficientPoints`, баланс не трогается) — тот же принцип, что у `TryDecrementAsync` для остатка склада, только без атомарного SQL UPDATE (здесь конкурентное списание баллов одним и тем же аккаунтом менее вероятно и не так критично, как продажа последней единицы товара, поэтому упрощённая read-then-write модель признана достаточной для этой сущности — в отличие от `StockLevel`, где race condition было ядром задачи).
- **GiftCard** — единственная сущность из этого батча вообще без привязки к магазину или пользователю (сама структура `Domain.Payments.GiftCard` не имеет ни `StoreId`, ни `OwnerUserId` — так была спроектирована ещё на этапе Domain layer). Это осознанно не переделывалось (не в рамках "активации" — менять форму существующей сущности значило бы редизайн, а не активацию): `IssueGiftCardCommand` доступен любому `StorePartner`, `RedeemGiftCardCommand` — тоже любому, код генерируется сервером (`Guid`, 12 символов, не последовательный ID — не угадать перебором). Если/когда бизнес решит, что подарочные карты должны быть привязаны к конкретному магазину — это отдельная задача на изменение модели, не эта.
- **StoreCredit** — в отличие от `GiftCard`, привязан к `(StoreId, CustomerId)` напрямую в самой сущности, поэтому владение проверяется напрямую через `Store.OwnerUserId`, без многоходовки. `IssueStoreCreditCommand` — upsert (первый выпуск создаёт строку, повторный увеличивает существующий баланс, а не создаёт вторую запись на того же клиента в том же магазине).

### Проверено вручную end-to-end (все 14 эндпоинтов, реальные вычисления, не только билд)

Полный сквозной сценарий через `dotnet run` + `curl`: `CreateStore` → `CreateCustomer` → `CreateLoyaltyProgram` → `EnrollCustomerInLoyalty` → `EarnLoyaltyPoints` (+50 → баланс 50) → `RedeemLoyaltyPoints` на 100 (больше баланса) → `409 "Insufficient points balance"` → `RedeemLoyaltyPoints` на 20 (валидно) → баланс 30 → `GetLoyaltyAccount` подтверждает 30. Отдельно: `IssueGiftCard` → `GetGiftCardBalance` (публично, без токена) → `RedeemGiftCard` 30 из 100 → остаток 70 → `RedeemGiftCard` с несуществующим кодом → `404`. Отдельно: `IssueStoreCredit` 15 → `GetStoreCreditBalance` подтверждает 15 → `RedeemStoreCredit` 5 → остаток 10.

**Результат**: `dotnet build` — 0 ошибок с первой попытки (впервые за все три батча — сказался накопленный опыт с паттерном многоходовой проверки владения); `dotnet test` — **148/148 пройдено** (+32 новых теста на 14 handler'ов, включая явные тесты на многоходовую проверку `Forbidden` через `LoyaltyAccount → LoyaltyProgram → Store`). Миграции не потребовались.

**Не сделано (сознательно)**: `LoyaltyTransaction`/история операций не выводится отдельным query (только текущий баланс через `GetLoyaltyAccountQuery`) — журнал операций как отдельная фича, не часть "активации" сущности; `GiftCard`/`StoreCredit` не интегрированы как способ оплаты в `ProcessSaleCommand` (это отдельная, более сложная задача — провести продажу частично гасить подарочной картой, требует изменения самого `ProcessSaleCommand`, не просто активации сущности).

## 2026-07-18 (продолжение 6) — "Активируй все Entity" — Batch 4/9: Supply chain

### `PurchaseOrder`+`PurchaseOrderLineItem`, `StockTransfer`, `ReorderRule`, `ProductBundle`+`ProductBundleItem`

11 use-case'ов — первый батч, где сборка прошла с 0 ошибок с первой попытки (накопленный за три батча опыт с паттерном "владение через цепочку связей" начал окупаться). Два use-case'а здесь — не просто CRUD-обвязка, а реальная интеграция с уже существующей складской механикой:

- **`ReceivePurchaseOrderCommand`** — приёмка заказа поставщику **реально увеличивает остаток**: для каждой строки заказа вызывается уже существующий `IStockLevelRepository.IncrementAsync` + пишется `StockMovement` (`Type = Receipt`, `SupplierId` заказа) — тот же паттерн, что у `RecordStockReceiptCommand`, но управляемый по строкам заказа, а не одной ручной операцией, и внутри `ExecuteInTransactionAsync` (несколько строк заказа = несколько складских операций, должны либо все пройти, либо ни одна). Жизненный цикл: `Draft → Submitted → Received` (`SubmitPurchaseOrderCommand`/`ReceivePurchaseOrderCommand` проверяют текущий статус перед переходом — попытка принять неотправленный заказ → `409 "not been submitted"`, проверено вручную).
- **`InitiateStockTransferCommand`/`CompleteStockTransferCommand`** — межмагазинное перемещение товара, **оба конца обязаны принадлежать одному владельцу** (`FromStore.OwnerUserId == ToStore.OwnerUserId == PerformedByUserId`) — иначе кто угодно мог бы инициировать "перемещение" чужого товара себе. `Initiate` атомарно списывает остаток у источника через уже существующий `TryDecrementAsync` (та же защита от отрицательного остатка, что и в `ProcessSaleCommand`) и создаёт `StockTransfer` в статусе `InTransit`; `Complete` (дергает уже владелец **магазина-получателя**, не источника) зачисляет остаток через `IncrementAsync`. `StockMovementType` не имеет отдельного значения "Transfer" (только `Receipt/Sale/WriteOff/Correction` — решение, принятое ещё на этапе Domain layer) — использован `Correction` с текстовым `Reason` ("Stock transfer out/in to/from store N"), не редизайн enum ради одной операции.
- **`GetReorderAlertsQuery`** — буквальная реализация CLAUDE.md §4 "алерт при низком остатке": пересекает активные `ReorderRule` магазина с текущими `StockLevel`, помечает товар, если `CurrentQuantity <= ThresholdQuantity` (включая случай, когда для товара вообще нет строки `StockLevel` — трактуется как 0, а не падает).
- **`ProductBundle`** — простой набор товаров по фиксированной цене, без интеграции с `ProcessSaleCommand` (продать бандл одним щелчком — отдельная более сложная задача, аналогично `GiftCard`/`StoreCredit` из прошлого батча).

### Проверено вручную end-to-end (полный цикл снабжения, реальные цифры, не только билд)

Через `dotnet run` + `curl`: `CreateSupplier` → `CreatePurchaseOrder` (20 шт) → попытка `ReceivePurchaseOrder` **до** `Submit` → `409 "not been submitted"` → `SubmitPurchaseOrder` → `ReceivePurchaseOrder` → `GetStockLevel` подтвердил **реальные +20** на складе. Затем `InitiateStockTransfer` 7 шт между двумя магазинами одного владельца → остаток источника **13** (20−7, атомарно) → `CompleteStockTransfer` → остаток получателя **7** — подтверждено `GetStockLevel` на обоих складах, не предположение. `CreateReorderRule` с порогом 15 при фактическом остатке 13 → `GetReorderAlerts` **корректно вернул алерт** (13 ≤ 15) — не заглушка, реальное пересечение данных. `CreateProductBundle` → `GetProductBundles` (публично) вернул созданный бандл с товарами.

**Результат**: `dotnet build` — 0 ошибок с первой попытки; `dotnet test` — **174/174 пройдено** (+26 новых тестов на 8 из 11 handler'ов — три тривиальных Get-list handler'а без ветвлений не тестировались отдельно, аналогично прошлым батчам). Миграции не потребовались.

**Не сделано (сознательно)**: `CancelPurchaseOrderCommand`/`CancelStockTransferCommand` (откат резервирования при отмене) — сознательно вне охвата, чтобы не раздувать батч; при реальной эксплуатации это следующий логичный шаг. `ProductBundle`/`GiftCard`/`StoreCredit` как способ оплаты в кассе — по-прежнему не подключены к `ProcessSaleCommand`.

## 2026-07-18 (продолжение 7) — "Активируй все Entity" — Batch 5/9: POS extras — и реальный баг, пойманный только ручной проверкой

### `CashierShift`, `SaleReturn`+`ReturnLineItem`

5 use-case'ов — самый маленький по числу файлов батч, но с двумя содержательно нетривиальными операциями:

- **`CloseCashierShiftCommand`** — прямая реализация CLAUDE.md §10 "рассмотреть периодическую сверку остатка с фактическим", применённая к наличным, а не к складу: `ExpectedCash` вычисляется сервером (`OpeningCash` + сумма выручки по завершённым продажам этого кассира за время смены через уже существующий `GetAllInRangeAsync`), **не принимается от клиента** — кассир не может просто вписать число, при котором смена "сходится". Результат содержит `Discrepancy = ClosingCash - ExpectedCash`, доступный владельцу магазина для проверки на недостачу.
- **`ProcessReturnCommand`** — частичный возврат (в отличие от `VoidSaleCommand`, который аннулирует продажу целиком): принимает конкретные позиции чека с количеством, восстанавливает остаток (`IncrementAsync` + `StockMovement` с `RelatedSaleTransactionId`), считает возврат по цене на момент продажи (`UnitPriceAtSale`, не текущей цене). **Защита от двойного возврата той же позиции** — перед проверкой суммируются уже сделанные возвраты по этой же строке чека (`ISaleReturnRepository.GetBySaleTransactionIdAsync`), и новый запрос не может превысить оставшееся количество (`ExceedsAvailableQuantity`, если превышает).

### Найден и исправлен реальный баг — не тестами, а только ручной сквозной проверкой

При проверке `CloseCashierShiftCommand` на реальном сервере: смена открыта с `OpeningCash = 100`, проведена продажа на `50`, ожидалось `ExpectedCash = 150` — сервер вернул **`100`** (продажа как будто не учлась). Причина: `SaleTransactionRepository.GetAllInRangeAsync` (добавлен в Batch 3 для `GetCashierAnomalyReportQuery`, которому позиции чека не нужны — только статус) **не делал `.Include(s => s.Lines)`**. `GetCashierAnomalyReportQuery` от этого не страдал (считает только count/void-статус), но `CloseCashierShiftCommand` суммирует `l.UnitPriceAtSale * l.Quantity` по `Lines` — пустая (не загруженная) коллекция навигации молча дала сумму `0`.

**Почему это не поймали 187 прошедших юнит-тестов**: тест `CloseCashierShiftCommandHandlerTests` мокирует `ISaleTransactionRepository.GetAllInRangeAsync` напрямую и **сам** кладёт в `SaleTransaction.Lines` нужные данные при подготовке мока — мок ничего не знает о `.Include()` в реальном EF Core запросе и не может обнаружить его отсутствие. Это тот же класс проблемы, что и запись от 2026-07-22 ("EF Core не ловит ошибки трансляции LINQ на этапе сборки") и от Batch 2 ("`dotnet build`/юнит-тесты не гарантируют, что маршрут вообще зарегистрируется") — третий отдельный пример за сессию, когда **только реальный `dotnet run` против реальной БД** вскрывает то, что билд и моки видят как исправное.

Исправлено: `.Include(s => s.Lines)` добавлен в `GetAllInRangeAsync` — безопасно для обоих потребителей (`GetCashierAnomalyReportQuery` игнорирует лишние загруженные строки, `CloseCashierShiftCommand` теперь их получает).

### Проверено вручную end-to-end (включая обнаружение и исправление бага, не только билд)

`OpenCashierShift` (100 TJS) → `ProcessSale` (5 шт × 10 = 50) → `CloseCashierShift` при заявленных 150 → **до исправления**: `expectedCash: 100, discrepancy: 50` (баг подтверждён на реальных данных) → **после исправления**: `expectedCash: 150, discrepancy: 0`. Отдельно: `ProcessReturn` 2 из 5 шт → остаток `+2` подтверждён; попытка вернуть ещё 4 (осталось только 3) → `409 "exceeds what's available"`; возврат оставшихся ровно 3 → `200`; `GetReturnsForSale` показал обе частичные записи с верным `refundAmount` каждая.

**Результат**: `dotnet build` — 0 ошибок; `dotnet test` — **187/187 пройдено** (+13 новых тестов). Миграции не потребовались.

**Не сделано (сознательно)**: `CashierShift` не привязана к отдельной роли "кассир" (её всё ещё нет — CLAUDE.md §9); фото/подпись при закрытии смены (реальные POS иногда это требуют) — вне охвата.
