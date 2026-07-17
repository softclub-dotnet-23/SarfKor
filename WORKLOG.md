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
