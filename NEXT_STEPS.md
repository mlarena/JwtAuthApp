# План дальнейших улучшений — JwtAuthApp

> Файл-памятка: что уже сделано, что осталось и как не сломать проект при продолжении.
> Обновляй этот файл после каждой крупной итерации.

---

## 1. Контекст проекта (кратко)

- ASP.NET Core **net9.0** MVC, JWT-аутентификация, PostgreSQL (Npgsql).
- Роли: `User`/`Admin` (many-to-many `UserRoles` + строковое поле `User.Role` для обратной совместимости).
- Доступ к контроллерам управляется динамически (таблица `ControllerAccess` + `ControllerAccessRoles`) через middleware.
- Аудит: `UserActionLogFilter` (действия) + `ApplicationDbContext.SaveChangesAsync` (change-logs, jsonb).
- Прод-таргет: **Linux Debian**, reverse-proxy nginx. Разработка на **Windows** (PowerShell).
- **ВАЖНО**: `JwtAuthApp.csproj` лежит в корне репозитория → в него добавлено исключение `tests/**` из компиляции (иначе тестовый проект компилируется в app-проект).

---

## 2. Что уже сделано (две итерации)

### Итерация 1 — критические фиксы (выполнено, всё проверено сборкой)
1. **Секреты убраны из `appsettings.json`.** Конфиг теперь пустой; обязательные значения читаются из env:
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__Key` (≥32 символа)
   - `Security__SuperUserPassword` (пароль суперюзера `su`; если не задан — генерируется случайный и логируется)
   - при старте — валидация, при отсутствии — `InvalidOperationException`.
2. **Аудит не пишет `PasswordHash`/`Salt`/`Password`/`Token`** — `SensitiveAuditProperties` + `GetSafeValues()` в `ApplicationDbContext`.
3. **CSRF на `AuthController`**: `[ValidateAntiForgeryToken]` на Login/Register/Logout (формы уже генерили токены tag helper'ом).
4. **Deny-by-default** в `ControllerAccessMiddleware` (и в `ControllerAccessHandler`): пустой список ролей без `AllowAll` = запрет.
5. **Пароль `su` из env + rate limiting** (`AddRateLimiter`, политика `auth`, 10 req/мин/IP; `[EnableRateLimiting("auth")]` на AuthController).
6. **PBKDF2**: итерации настраиваются `Security:PasswordIterations` (по умолч. 210 000), fallback-проверка legacy-хешей (10k).
7. **`Jwt:ExpireDays` теперь используется** (вместо захардкоженного 1 дня).
8. **CORS AllowAll удалён**; session-cookie `SameSite=Lax`; `UseForwardedHeaders` (X-Forwarded-For/Proto) для nginx.
9. `SessionTokenMiddleware` не затирает входящий `Authorization`.

### Итерация 2 — второй эшелон (выполнено, тесты зелёные)
1. **Security headers** (`Middleware/SecurityHeadersMiddleware.cs`): X-Frame-Options DENY, nosniff, Referrer-Policy, Permissions-Policy, CSP (jsdelivr для стилей/шрифтов, `frame-ancestors 'none'`).
2. **Кэш правил доступа** (`Services/ControllerAccessCache.cs`): `IMemoryCache`, TTL 5 мин, негативное кэширование 30 сек; инвалидация из `AccessController` (все мутирующие методы).
3. **Отзыв JWT**: новое поле `User.TokenValidAfter` + миграция `AddTokenValidAfter`. Проверка в `OnTokenValidated` (iat < TokenValidAfter или IsBlocked → Fail). Отзыв при logout / смене пароля / блокировке.
   - Добавлена `Data/DesignTimeDbContextFactory.cs` — БЕЗ неё `dotnet ef migrations add` падает (два конструктора у контекста + валидация конфига в `Program.cs`).
4. **Обрезка полей аудита** (`UserActionLogFilter.Truncate`) — длинный User-Agent/Details/Url больше не роняет `SaveChanges`.
5. **Seed вынесен в `Services/DbSeeder.cs`** (`DbSeeder.Initialize(app, configuration, logger)`): миграции, роли, su, назначение ролей, ControllerAccess, автообнаружение контроллеров.
6. **Unit-тесты** (`tests/JwtAuthApp.Tests`, xunit, net9.0, EF InMemory):
   - `AuthServiceTests`: roundtrip хеша, неверный пароль, уникальность соли, legacy-хеши 10k принимаются, новые хеши с 210k не принимаются сервисом с 10k итераций, роли в токене, мусорный токен → null, чужой issuer → null.
   - `AuditTests`: change-logs создаются, PasswordHash/Salt отсутствуют в jsonb-снимках, смена пароля не палит хеши, аудит сам себя не логирует.

---

## 3. ОСТАВШИЕСЯ ПРЕДЛОЖЕНИЯ (backlog по приоритету)

### 🔴 Приоритет 1 (безопасность / эксплуатация)
1. **Ротация скомпрометированных секретов пользователем**: пароль postgres `12345678` и JWT-ключ были в git-истории. Нужно сменить пароль БД и сгенерировать новый ключ (`openssl rand -base64 48`). Кодом это не закрыть — нужны действия владельца.
2. **Пагинация аудита**: `Audit/Index` грузит все записи; таблица растёт бесконечно. Добавить `Skip/Take`, фильтры по дате/пользователю/действию.
3. **Архивирование/очистка AuditLogs**: задание/фоновый сервис для удаления или архивации записей старше N дней (например, `IHostedService`).
4. **Rate limiting доработать**: сейчас лимит только на AuthController. Рассмотреть общий лимит и на остальные эндпоинты (или хотя бы на Admin/Access).

### 🟠 Приоритет 2 (архитектура / код)
5. **Удалить строковое поле `User.Role`** — двойная модель ролей рассинхронизируется (fallback в `GenerateJwtToken`, `user.Role` в AdminController). Требует миграции: заполнить `UserRoles` из `Role`, затем drop. Аккуратно: seed/админка используют `Role`.
6. **Удалить/докинуть `Authorization/ControllerAccessHandler+Requirement`** — мёртвый код, дублирует middleware. Либо зарегистрировать как фолбэк к `[Authorize]`, либо удалить.
7. **`UseHttpsRedirection` + `CookieSecurePolicy`**: в dev по HTTP — cookie уходит открыто. При деплое за nginx с TLS сделать https-redirect корректным, антифоржери-cookie `SecurePolicy=Always` (или оставить SameAsRequest, но проверить, что nginx проксирует HTTPS).
8. **Кэш отзыва токена** (`token-revocation:{userId}`): сейчас TTL 30 сек. При logout пользователь «жив» до 30 сек — приемлемо, но можно вызывать инвалидацию ключа сразу из `Logout`/`ToggleBlock`.
9. **Health checks**: `AddHealthChecks` + эндпоинт `/health` (для nginx upstream и мониторинга; в деплой на debian).
10. **Сессия in-memory**: при множественных инстансах/редистрибуте теряется. Рассмотреть Redis (`AddStackExchangeRedisCache`), если будет горизонтальное масштабирование.
11. **`Database.Migrate()` при каждом старте**: при нескольких инстансах возможны гонки. Для прода — отдельный шаг деплоя (`dotnet ef database update`) или ручная установка миграций.

### 🟡 Приоритет 3 (качество / DX)
12. **Dockerfile + docker-compose** для деплоя на debian (nginx + app + postgres); CI (GitHub Actions): `dotnet build` + `dotnet test`.
13. **Структурированные логи**: Serilog (console + file) + request logging; сейчас только консоль.
14. **Парольная политика**: минимальная длина/сложность в RegisterViewModel/ChangePasswordViewModel (сейчас проверки слабые — надо посмотреть ViewModels).
15. **Мёртвый код/мелочи**: в `Program.cs` обработка корня `MapGet("/")` дублирует default route; `AuthRedirectMiddleware` не знает про JSON-запросы; `AdminController.Create` имеет два `SaveChangesAsync` (лучше транзакция).
16. **Известные warnings при сборке** (не критично, но стоит поправить):
    - `Services\AuthService.cs:151` — потенциально null в `Encoding.GetBytes` (в `ValidateToken`).
    - `Filters\UserActionLogFilter.cs:85` — потенциально null в `Truncate` (обрабатывается внутри, но warning).
    - `Views\Home\Index.cshtml` и `Views\Secure\Index.cshtml` — возможные null-разыменования.
    - `tests` — MSB3277 (конфликт версий EF Relational 9.0.0/9.0.2). Убрать: выровнять версии NuGet (например, добавить `Microsoft.EntityFrameworkCore` 9.0.2 в тестовый проект или Central Package Management).

---

## 4. Как запускать проект (напоминание)

```powershell
# Windows (PowerShell), dev
$env:ConnectionStrings__DefaultConnection = "Host=<host>;Database=<db>;Username=<user>;Password=<pass>"
$env:Jwt__Key = "<минимум 32 символа>"
$env:Security__SuperUserPassword = "<пароль для su>"
dotnet run
```

```bash
# Linux Debian (systemd: EnvironmentFile=)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=jwtapp;Username=jwtapp;Password=<пароль>"
export Jwt__Key="<случайный ключ минимум 32 символа>"
export Security__SuperUserPassword="<пароль>"
dotnet JwtAuthApp.dll
```

Миграции применяются автоматически при старте (`DbSeeder.Initialize`). Для создания новой миграции:
```powershell
dotnet ef migrations add <Name> -o Data\Migrations
```
**Обязательно**: `DesignTimeDbContextFactory` уже есть — без неё `dotnet ef` не работает (два конструктора контекста + валидация конфига при `builder.Build()`).

---

## 5. Ключевые предупреждения для будущего меня

1. **НЕ добавляй секреты в `appsettings.json`** — только env-переменные. Прогон: при старте падает, если нет строки подключения или `Jwt__Key` <32 симв.
2. **Пароль `su`**: если `Security__SuperUserPassword` не задан, приложение генерирует случайный пароль и логирует его с уровнем Warning. Это намеренное поведение (безопасно по умолчанию).
3. **Хеши паролей**: старые (10k итераций PBKDF2) принимаются только для верификации; новые создаются с `Security:PasswordIterations` (по умолчанию 210k). При смене пароля пользователь автоматически «пересоливается».
4. **`ControllerAccess` deny-by-default**: пустой список ролей при `AllowAllAuthenticated=false` = запрет. Новый контроллер без правила в БД = запрет (`access == null` → Deny). Не «чини» это обратно в allow.
5. **`tests/**` исключены из `JwtAuthApp.csproj`** — не убирай это исключение.
6. **`OnTokenValidated`** требует `JwtRegisteredClaimNames.Iat` в токене (он добавляется в `AuthService.GenerateJwtToken`). При изменении формата токена — правь проверку отзыва.
7. **При изменении модели `User`/таблиц** — обязательно создавать миграцию (`dotnet ef migrations add`), иначе старт приложения упадёт на `Migrate()`.
8. Кэш `ControllerAccessCache` и `token-revocation` — in-memory: при перезапуске или рестарте подсистемы сбрасываются, данные читаются из БД заново. Это норм.
9. Тесты используют EF **InMemory** — не проверяют валидность SQL/индексов. Для проверки Postgres-специфики (jsonb, timestamptz) — прогон на реальной БД.