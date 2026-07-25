# Описание работы кода JwtAuthApp

## 1. Точка входа — Program.cs

### 1.1. Конфигурация сервисов

```csharp
// PostgreSQL контекст
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT аутентификация
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* валидация токена */ });

// Авторизация
builder.Services.AddAuthorization();

// MVC с глобальным фильтром логирования
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<UserActionLogFilter>());
```

### 1.2. Seed-данные при старте

```
1. Применение миграций (dbContext.Database.Migrate())
2. Создание ролей (Admin, User) если их нет
3. Создание суперпользователя "su/su" если его нет
4. Назначение ролей существующим пользователям без UserRole записей
5. Заполнение ControllerAccess (16 контроллеров) если таблица пуста
6. ControllerDiscoveryService — автообнаружение новых контроллеров
```

### 1.3. Пайплайн middleware

```
UseStaticFiles → UseRouting → UseCors → UseSession
→ SessionTokenMiddleware → UseAuthentication → UseAuthorization
→ ControllerAccessMiddleware → AuthRedirectMiddleware
→ MapControllerRoute (default: {controller=Home}/{action=Index}/{id?})
```

## 2. Аутентификация

### 2.1. AuthService.cs

**Генерация JWT:**
```csharp
GenerateJwtToken(User user)
    → Создаёт claims: sub, name, role (для каждой роли), jti, iat
    → Подписывает HMACSHA256
    → Возвращает строку токена
```

**Хеширование пароля:**
```csharp
HashPassword(string password)
    → Генерирует 32-байтную соль (RandomNumberGenerator)
    → PBKDF2 с 10000 итерациями, SHA256
    → Возвращает (hash, salt) в Base64
```

**Проверка пароля:**
```csharp
VerifyPassword(string password, string hash, string salt)
    → Воссоздаёт хеш из введённого пароля + соли
    → CryptographicOperations.FixedTimeEquals() — защита от timing attacks
```

### 2.2. AuthController.cs

**Login (POST):**
```
1. Поиск пользователя по UserName (с .Include(UserRoles.Role))
2. Проверка пароля через AuthService.VerifyPassword()
3. Проверка IsBlocked — если true, ошибка
4. Генерация JWT через AuthService.GenerateJwtToken()
5. Сохранение токена в Session ("JWToken")
6. Редирект на Home/Index
```

**Register (POST):**
```
1. Проверка уникальности UserName
2. Хеширование пароля
3. Создание User + UserRole (Role="User")
4. Загрузка ролей для генерации токена
5. Сохранение токена в Session
6. Редирект на Home/Index
```

### 2.3. SessionTokenMiddleware.cs

```csharp
// Каждый запрос:
var token = context.Session.GetString("JWToken");
if (!string.IsNullOrEmpty(token))
    context.Request.Headers["Authorization"] = "Bearer " + token;
```

Это позволяет JWT middleware работать с токеном из Session, а не из cookie/localStorage.

## 3. Авторизация и контроль доступа

### 3.1. ControllerAccessMiddleware.cs

```csharp
InvokeAsync(HttpContext context)
    → Извлекает имя контроллера из RouteData
    → Исключения: Auth, Home (всегда доступны)
    → Неаутентифицированные → пропуск (handled by AuthRedirectMiddleware)
    → Запрос в ControllerAccess table
    → Если записи нет → 403 Forbidden
    → Если AllowAllAuthenticated → доступ
    → Если есть roles → проверка JWT claims
    → Если нет совпадения → 403 Forbidden
```

### 3.2. AuthRedirectMiddleware.cs

```csharp
InvokeAsync(HttpContext context)
    → Статические файлы → пропуск
    → /Auth → редирект на /Auth/Login
    → Неаутентифицированный + не Auth → редирект на /Auth/Login
    → Аутентифицированный + Auth (кроме Logout) → редирект на /Home/Index
```

### 3.3. ControllerDiscoveryService.cs

```csharp
DiscoverAndRegister(IApplicationBuilder app)
    → Рефлексия: Assembly.GetExecutingAssembly()
    → Находит все классы *Controller
    → Сравнивает с ControllerAccess table
    → Новые: добавляет с AllowAllAuthenticated = false
    → Админ настраивает доступ через /Access/Edit
```

## 4. Логирование

### 4.1. UserActionLogFilter.cs (Action логи)

```csharp
OnActionExecutionAsync()
    → Stopwatch.Start()
    → await next() — выполнение контроллера
    → Stopwatch.Stop()
    → Если пользователь аутентифицирован:
        → Создаёт AuditLog (Type=Action)
        → Заполняет: Action, Details, HttpMethod, Url, IpAddress, ExecutionTimeMs
        → Добавляет в _context.AuditLogs (ChangeTracker)
    → Контроллер вызывает SaveChangesAsync() — лог сохраняется вместе с данными
```

### 4.2. ApplicationDbContext.SaveChangesAsync (Change логи)

```csharp
SaveChangesAsync()
    → Если _isSavingAuditLogs = true → пропуск (защита от рекурсии)
    → Сканирует ChangeTracker (Added/Modified/Deleted)
    → Исключает AuditLog и ControllerAccess
    → Для каждой записи создаёт AuditLog (Type=Change):
        - Added: NewValues
        - Deleted: OriginalValues
        - Modified: OriginalValues + NewValues + ChangedProperties
    → Сохраняет всё за один вызов base.SaveChangesAsync()
```

## 5. Управление пользователями

### 5.1. AdminController

| Action | Метод | Описание |
|--------|-------|----------|
| Index | GET | Список всех пользователей с ролями |
| Create | GET/POST | Создание пользователя (роль User по умолчанию) |
| Edit | GET/POST | Редактирование имени пользователя |
| Delete | POST | Удаление пользователя |
| ChangePassword | GET/POST | Смена пароля админом |
| ToggleBlock | POST | Блокировка/разблокировка |
| ManageRoles | GET/POST | Управление ролями (checkbox'ы) |
| QuickAddRole | POST | Быстрое добавление роли |
| QuickRemoveRole | POST | Быстрое удаление роли |

### 5.2. Блокировка

```csharp
// При входе:
if (user.IsBlocked)
    ModelState.AddModelError("", "Эта учётная запись заблокирована...");

// При ToggleBlock:
user.IsBlocked = !user.IsBlocked;
// Токен пользователя НЕ аннулируется — нужно разблокировать и перелогиниться
```

## 6. Управление доступом

### 6.1. AccessController

| Action | Описание |
|--------|----------|
| Index | Список контроллеров с правами доступа |
| Edit | Редактирование: DisplayName, AllowAllAuthenticated, Roles |
| QuickToggle | Быстрое добавление/удаление роли |
| ToggleAllowAll | Переключение "для всех авторизованных" |

### 6.2. Таблица ControllerAccess

```csharp
ControllerAccess
    ControllerName     — имя контроллера (уникальное)
    DisplayName        — отображаемое имя
    Description        — описание
    AllowAllAuthenticated — доступно всем
    ControllerAccessRoles[] — связи many-to-many с Role
```

## 7. Данные мониторинга

### 7.1. Иерархия

```
MonitoringPost (пост)
  └── Sensor (датчик)
        ├── SensorType (тип: DOV, DSPD, Dust, IWS, MUEKS)
        ├── DOVData (данные видимости)
        ├── DSPDData (данные дороги)
        ├── DustData (данные пыли)
        ├── IWSData (метеоданные)
        ├── MUEKSData (данные питания)
        └── SensorResult (результаты опроса)
```

### 7.2. CRUD контроллеры

**Полный CRUD** (Index, Create, Edit, Delete, Details, ToggleActive):
- MonitoringPost
- Sensor
- SensorType

**Read-only** (Index с фильтром + Details):
- DOVData, DSPDData, DustData, IWSData, MUEKSData, SensorResults
- Пагинация: 50 записей на страницу
- Фильтр по SensorId

## 8. Представления

### 8.1. Layout (_Layout.cshtml)

```
Если аутентифицирован:
    Topbar: hamburger, название, аватар, имя, тема, выход
    Sidebar: динамическое меню из MenuViewComponent
    Main: @RenderBody()

Если не аутентифицирован:
    Main: @RenderBody() (страница входа/регистрации)
```

### 8.2. MenuViewComponent

```csharp
InvokeAsync()
    → Запрашивает ControllerAccess + роли пользователя
    → Исключения: Auth, Home
    → Основные пункты: MonitoringPost, Sensor, SensorType, DOVData, ...
    → Admin раздел: Admin, Role, Access, Audit (dropdown)
    → Фильтр: CanAccess() проверяет AllowAllAuthenticated + роли
```

### 8.3. Тема

```javascript
// Переключение:
applyTheme('dark' | 'light')
    → html.setAttribute('data-theme', theme)
    → localStorage.setItem('theme', theme)
    → Меняет иконку: moon ↔ sun

// Восстановление:
const savedTheme = localStorage.getItem('theme') || 'light';
applyTheme(savedTheme);
```

## 9. Конфигурация JWT

```json
{
  "Jwt": {
    "Key": "ThisIsMyVeryLongSecretKey...",
    "Issuer": "JwtAuthApp",
    "Audience": "JwtAuthAppUsers",
    "ExpireDays": 1
  }
}
```

**Параметры валидации:**
```csharp
ValidateIssuer = true
ValidateAudience = true
ValidateLifetime = true
ValidateIssuerSigningKey = true
ClockSkew = TimeSpan.Zero (в AuthService.ValidateToken)
```

## 10. Антифоржери

```csharp
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AntiForgeryCookie";
});

// В формах:
@Html.AntiForgeryToken()

// В POST-действиях:
[ValidateAntiForgeryToken]
```
