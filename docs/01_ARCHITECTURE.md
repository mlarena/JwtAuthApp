# Архитектурное описание приложения JwtAuthApp

## 1. Обзор

ASP.NET Core 9.0 MVC-приложение с JWT-аутентификацией, role-based авторизацией и динамическим управлением доступом. Предназначено для работы в корпоративных средах и интернете.

## 2. Технологический стек

| Компонент | Технология |
|-----------|-----------|
| Фреймворк | ASP.NET Core 9.0 |
| ORM | Entity Framework Core 9.0 |
| База данных | PostgreSQL 15+ (Npgsql) |
| Аутентификация | JWT Bearer + Session |
| Авторизация | Role-based + ControllerAccess middleware |
| Фронтенд | Bootstrap 5, Bootstrap Icons, jQuery |
| Язык | C# 12 |

## 3. Структура проекта

```
JwtAuthApp/
├── Attributes/              # Кастомные атрибуты
│   └── SkipLoggingAttribute.cs
├── Authorization/           # Политики авторизации
│   ├── ControllerAccessHandler.cs
│   └── ControllerAccessRequirement.cs
├── Controllers/             # MVC-контроллеры (18 шт.)
│   ├── AuthController.cs          # Вход/регистрация/выход
│   ├── HomeController.cs          # Главная страница
│   ├── AdminController.cs         # Управление пользователями
│   ├── RoleController.cs          # Управление ролями
│   ├── AccessController.cs        # Управление доступом
│   ├── AuditController.cs         # Журнал аудита
│   ├── MonitoringPostController.cs # CRUD постов мониторинга
│   ├── SensorController.cs        # CRUD датчиков
│   ├── SensorTypeController.cs    # CRUD типов датчиков
│   ├── PollingSessionController.cs # Сессии опроса
│   ├── DOVDataController.cs       # Данные видимости
│   ├── DSPDDataController.cs      # Данные дороги
│   ├── DustDataController.cs      # Данные пыли
│   ├── IWSDataController.cs       # Метеоданные
│   ├── MUEKSDataController.cs     # Данные питания
│   ├── SensorResultsController.cs # Результаты опроса
│   ├── SecureController.cs        # Тестовая защищённая страница
│   └── TestController.cs          # Тестовая страница
├── Data/                    # DbContext и миграции
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Filters/                 # Фильтры MVC
│   └── UserActionLogFilter.cs
├── Middleware/               # Кастомные middleware
│   ├── AuthRedirectMiddleware.cs
│   ├── ControllerAccessMiddleware.cs
│   └── SessionTokenMiddleware.cs
├── Models/                  # Модели данных (14 шт.)
│   ├── User.cs
│   ├── Role.cs
│   ├── UserRole.cs
│   ├── ControllerAccess.cs
│   ├── ControllerAccessRole.cs
│   ├── MonitoringPost.cs
│   ├── Sensor.cs
│   ├── SensorType.cs
│   ├── PollingSession.cs
│   ├── DOVData.cs
│   ├── DSPDData.cs
│   ├── DustData.cs
│   ├── IWSData.cs
│   ├── MUEKSData.cs
│   ├── SensorResult.cs
│   └── AuditLog.cs
├── Services/                # Бизнес-логика
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   └── ControllerDiscoveryService.cs
├── ViewModels/              # Модели представлений
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   ├── EditUserViewModel.cs
│   ├── CreateUserViewModel.cs
│   ├── ChangePasswordViewModel.cs
│   ├── ManageUserRolesViewModel.cs
│   ├── AccessViewModels.cs
│   └── AuditLogViewModels.cs
├── ViewComponents/          # Компоненты представлений
│   └── MenuViewComponent.cs
├── Views/                   # Razor-представления (32+ файлов)
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── Components/Menu/Default.cshtml
│   │   └── ...
│   ├── Auth/
│   ├── Home/
│   ├── Admin/
│   ├── Role/
│   ├── Access/
│   ├── Audit/
│   ├── MonitoringPost/
│   ├── Sensor/
│   ├── SensorType/
│   ├── PollingSession/
│   ├── DOVData/
│   ├── DSPDData/
│   ├── DustData/
│   ├── IWSData/
│   ├── MUEKSData/
│   ├── SensorResults/
│   ├── Secure/
│   └── Test/
├── wwwroot/                 # Статические файлы
│   ├── css/site.css
│   └── js/
├── Program.cs               # Точка входа
└── appsettings.json         # Конфигурация
```

## 4. Пайплайн запросов (Middleware Pipeline)

```
HTTP Request
    │
    ▼
SessionTokenMiddleware          ── Извлекает JWT из Session, добавляет в Authorization header
    │
    ▼
Authentication (JWT Bearer)     ── Валидирует JWT токен, устанавливает User.Identity
    │
    ▼
Authorization                   ── Проверяет [Authorize] атрибуты на контроллерах
    │
    ▼
ControllerAccessMiddleware      ── Проверяет доступ к контроллеру через ControllerAccess table
    │
    ▼
AuthRedirectMiddleware          ── Редиректы: неавторизованные → /Auth/Login, авторизованные на Auth → /Home
    │
    ▼
MVC Router                      ── Маршрутизация к контроллеру
    │
    ▼
UserActionLogFilter             ── Глобальный фильтр: логирование действий в AuditLog
    │
    ▼
Controller → View               ── Обработка и рендер
```

## 5. Схема базы данных

### 5.1. Пользователи и роли

```
Users ──< UserRoles >── Roles
  │                       │
  │                       └── ControllerAccessRoles ──> ControllerAccess
  │
  └── AuditLogs (UserId FK)
```

### 5.2. Мониторинг и датчики

```
MonitoringPost ──< Sensor >── SensorType
      │                          │
      ├──< PollingSession        │
      │       │                  │
      │       ├──< DOVData       │
      │       ├──< DSPDData      │
      │       ├──< DustData      │
      │       ├──< IWSData       │
      │       ├──< MUEKSData     │
      │       └──< SensorResult  │
      │                          │
      └──< DOVData, DSPDData, ... (MonitoringPostId FK)
```

### 5.3. Таблицы данных (все привязаны к Sensor + PollingSession)

| Таблица | Назначение | Ключевые поля |
|---------|-----------|---------------|
| DOVData | Видимость (DOV) | VisibleRange, BrightFlag |
| DSPDData | Состояние дороги | Grip, RoadStatus, TemperatureRoad |
| DustData | Пыль | PM10Act, PM25Act, PM1Act |
| IWSData | Метеоданные | EnvTemperature, Humidity, WindSpeed |
| MUEKSData | Питание/система | UAkb, VisibleRange, DoorStatus |
| SensorResults | Результаты опроса | StatusCode, IsSuccess, ResponseTimeMs |

## 6. Механизм управления доступом

### 6.1. Три уровня контроля

```
Уровень 1: [Authorize] атрибуты
    └── Гарантирует, что пользователь аутентифицирован

Уровень 2: ControllerAccessMiddleware
    └── Проверяет таблицу ControllerAccess:
        - Нет записи → 403 Forbidden
        - AllowAllAuthenticated → доступ всем
        - Есть роли → проверка JWT claims

Уровень 3: ControllerAccessRoles (many-to-many)
    └── Гранулярный контроль: какие роли могут обращаться к контроллеру
```

### 6.2. Автообнаружение контроллеров

```
При старте приложения:
ControllerDiscoveryService.DiscoverAndRegister()
    → Рефлексия: находит все классы *Controller
    → Сравнивает с ControllerAccess table
    → Новые добавляются с AllowAllAuthenticated = false
    → Админ настраивает доступ через UI
```

### 6.3. Динамическое меню

```
MenuViewComponent.InvokeAsync()
    → Запрашивает ControllerAccess + роли пользователя
    → Фильтрует по CanAccess()
    → Группирует: основные пункты + "Администрирование" (dropdown)
    → Рендерит через Default.cshtml
```

## 7. Аутентификация

### 7.1. Поток JWT

```
1. Пользователь вводит логин/пароль
2. AuthController.Login() → AuthService.VerifyPassword()
3. AuthService.GenerateJwtToken() → JWT с claims:
   - sub: UserId
   - name: UserName
   - role: [Role1, Role2, ...]
   - jti: уникальный ID токена
   - iat: время создания
4. Токен сохраняется в Session ("JWToken")
5. SessionTokenMiddleware извлекает токен → добавляет в Authorization header
6. JWT Bearer middleware валидирует токен → устанавливает User.Identity
```

### 7.2. Хранение токена

Токен хранится в **Session** (не в cookie, не в localStorage). Это гибридный подход:
- Session защищена HttpOnly cookie
- JWT добавляется в каждый запрос через middleware

## 8. Логирование

### 8.1. Два типа логов

| Тип | Таблица | Источник |
|-----|---------|----------|
| Action | AuditLog (Type=Action) | UserActionLogFilter |
| Change | AuditLog (Type=Change) | ApplicationDbContext.SaveChangesAsync() |

### 8.2. Фильтр действий

```
UserActionLogFilter.OnActionExecutionAsync()
    → Замеряет время выполнения (Stopwatch)
    → После действия контроллера создаёт AuditLog
    → Добавляет в ChangeTracker (без отдельного SaveChanges)
    → Контроллер сохраняет всё за один round-trip
```

### 8.3. Перехват изменений

```
ApplicationDbContext.SaveChangesAsync()
    → Сканирует ChangeTracker (Added/Modified/Deleted)
    → Исключает AuditLog и ControllerAccess
    → Создаёт Change audit logs с OriginalValues/NewValues
    → Сохраняет всё за один вызов
```

## 9. Тема и интерфейс

### 9.1. CSS-переменные

```css
:root { /* Светлая тема */ }
[data-theme="dark"] { /* Тёмная тема */ }
```

### 9.2. Структура Layout

```
┌──────────────────────────────────────────┐
│  Topbar (фиксированная, 56px)            │
│  ☰ JwtAuthApp    [avatar] 🌙 [logout]   │
├────────────┬─────────────────────────────┤
│  Sidebar   │  Main Content               │
│  (260px)   │  (@RenderBody())            │
│  фикс.     │                             │
│  слева     │                             │
└────────────┴─────────────────────────────┘
```

## 10. Конфигурация

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=testjwt;Username=...;Password=..."
  },
  "Jwt": {
    "Key": "секретный_ключ_минимум_32_символа",
    "Issuer": "JwtAuthApp",
    "Audience": "JwtAuthAppUsers",
    "ExpireDays": 1
  }
}
```

## 11. Зависимости (NuGet)

| Пакет | Версия | Назначение |
|-------|--------|-----------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.2 | JWT аутентификация |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.2 | PostgreSQL провайдер |
| Microsoft.EntityFrameworkCore.Design | 9.0.2 | Миграции |
| System.IdentityModel.Tokens.Jwt | 8.6.1 | JWT операции |
