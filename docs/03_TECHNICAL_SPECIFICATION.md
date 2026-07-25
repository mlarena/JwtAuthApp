# Техническое задание (ТЗ)
# Полнофункциональное приложение с JWT-аутентификацией
# Версия: 2.0 — Суперстандарт

---

## 1. Общие положения

### 1.1. Назначение
Веб-приложение для управления пользователями, ролями, правами доступа и данными мониторинга. Предназначено для работы в корпоративных средах (интранет) и публичном интернете.

### 1.2. Требования к среде
- ASP.NET Core 9.0+
- PostgreSQL 15+
- HTTPS обязателен в продакшене
- Поддержка современных браузеров (Chrome, Firefox, Safari, Edge)

### 1.3. Целевая аудитория
- Системные администраторы (управление пользователями и доступом)
- Операторы мониторинга (просмотр данных датчиков)
- Разработчики (интеграция через API)

---

## 2. Аутентификация

### 2.1. Регистрация пользователя

**Сценарий:**
1. Пользователь переходит на /Auth/Register
2. Заполняет: UserName, Password, ConfirmPassword
3. Система проверяет:
   - Уникальность UserName
   - Минимальная длина пароля (8+ символов)
   - Совпадение паролей
4. Пароль хешируется (PBKDF2, 10000 итераций, SHA256, 32-байтная соль)
5. Пользователь создаётся с ролью "User"
6. Генерируется JWT токен
7. Токен сохраняется в Session
8. Редирект на главную страницу

**Дополнительно:**
- Подтверждение регистрации по email (опционально)
- Captcha при регистрации в публичном доступе
- Ограничение количества регистраций с одного IP

### 2.2. Вход в систему

**Сценарий:**
1. Пользователь вводит UserName + Password
2. Система ищет пользователя (с загрузкой ролей)
3. Проверка пароля (FixedTimeEquals — защита от timing attacks)
4. Проверка блокировки (IsBlocked)
5. Проверка количества неудачных попыток (Rate Limiting)
6. Генерация JWT с claims: sub, name, role[], jti, iat
7. Сохранение в Session
8. Редирект на главную

**Защита:**
- Rate limiting: максимум 5 попыток за 15 минут
- Блокировка на 30 минут после 5 неудачных попыток
- Логирование всех попыток входа (IP, User-Agent, время)

### 2.3. Выход из системы

**Сценарий:**
1. Пользователь нажимает "Выход"
2. Токен удаляется из Session
3. Редирект на /Auth/Login

**Дополнительно:**
- Инвалидация токена (чёрный список JTI)
- Выход из всех устройств (для админа)

### 2.4. Смена пароля

**Сценарий (самостоятельная):**
1. Пользователь вводит текущий пароль + новый пароль × 2
2. Проверка текущего пароля
3. Хеширование нового пароля
4. Обновление в БД
5. Инвалидация текущего токена (релогин)

**Сценарий (админом):**
1. Админ переходит в /Admin/ChangePassword/{id}
2. Вводит новый пароль × 2
3. Пароль обновляется без запроса текущего
4. Пользователь должен перелогиниться

### 2.5. Refresh Token

**Реализация:**
- Access Token: 15 минут
- Refresh Token: 7 дней, одноразовый
- Хранение: httpOnly secure cookie + БД (RefreshToken table)
- Обновление: POST /Auth/Refresh
- Ротация: при каждом использовании refresh token создаётся новый

**Таблица RefreshTokens:**
```
Id (Guid), UserId (FK), Token (hash), ExpiresAt, CreatedAt, IsRevoked, ReplacedByToken
```

### 2.6. Двухфакторная аутентификация (2FA)

**Варианты:**
- TOTP (Google Authenticator, Authy)
- Email код
- SMS код

**Реализация:**
- Настройка: GET /Auth/Setup2FA
- Активация: POST /Auth/Enable2FA (ввод кода из приложения)
- Вход: после пароля → страница ввода TOTP-кода
- Backup коды: 10 одноразовых кодов для восстановления

---

## 3. Авторизация

### 3.1. Role-Based Access Control (RBAC)

**Роли по умолчанию:**

| Роль | Описание | Права |
|------|----------|-------|
| SuperAdmin | Суперадминистратор | Всё + управление админами |
| Admin | Администратор | Управление пользователями, ролями, доступом, аудит |
| Operator | Оператор | Просмотр данных мониторинга, CRUD постов и датчиков |
| Viewer | Наблюдатель | Только просмотр данных |
| User | Обычный пользователь | Базовый доступ |

**Кастомные роли:**
- Админ создаёт роли через /Role/Index
- Каждая роль привязывается к контроллерам через ControllerAccess
- Поддержка иерархических ролей (роль наследует права вышестоящей)

### 3.2. Controller-Based Access Control

**Механизм:**
```
ControllerAccess table:
    ControllerName (уникальное)
    DisplayName
    AllowAllAuthenticated (bool)
    ControllerAccessRoles[] (many-to-many с Role)
```

**Правила:**
- Нет записи → доступ запрещён (403)
- AllowAllAuthenticated = true → доступ всем авторизованным
- Есть роли → проверка JWT claims

**Автообнаружение:**
- Новые контроллеры автоматически добавляются при старте
- Админ настраивает доступ через UI

### 3.3. Record-Level Access Control (RLAC)

**Для будущей реализации:**

```csharp
// Атрибут на контроллере:
[RecordAccess(ResourceType = "MonitoringPost")]
public class MonitoringPostController : Controller

// Проверка:
// 1. Пользователь имеет роль, дающую доступ к контроллеру
// 2. Пользователь является владельцем записи ИЛИ имеет глобальную роль
```

**Таблица RecordPermissions:**
```
Id, UserId, ResourceType, ResourceId, Permission (Read/Write/Delete), GrantedBy, ExpiresAt
```

### 3.4. API Key Authentication (для интеграций)

**Реализация:**
- Админ генерирует API ключ для пользователя
- Ключ хранится в хешированном виде
- Запросы с заголовком `X-Api-Key: ...`
- Rate limiting: 1000 запросов в час

**Таблица ApiKeys:**
```
Id, UserId (FK), KeyHash, Name, ExpiresAt, CreatedAt, LastUsedAt, IsActive
```

---

## 4. Управление пользователями

### 4.1. CRUD пользователей

| Операция | Доступ | Описание |
|----------|--------|----------|
| Создание | Admin | Имя, пароль, роль по умолчанию |
| Просмотр | Admin | Список с фильтрацией и сортировкой |
| Редактирование | Admin | Изменение имени |
| Удаление | Admin | С подтверждением |
| Блокировка | Admin | Временная/постоянная |
| Смена пароля | Admin | Без запроса текущего |

### 4.2. Профиль пользователя

**Страница /Account/Profile:**
- Просмотр своих данных
- Редактирование имени/фамилии/email
- Смена пароля (с текущим паролем)
- Настройка 2FA
- История входов
- Активные сессии

### 4.3. Управление сессиями

**Страница /Account/Sessions:**
- Список активных сессий (IP, Device, LastActive)
- Завершение отдельных сессий
- Завершение всех сессий кроме текущей

### 4.4. История действий пользователя

**Страница /Account/Activity:**
- Последние 50 действий
- Фильтр по типу, дате
- Экспорт в CSV

---

## 5. Управление ролями

### 5.1. CRUD ролей

| Операция | Описание |
|----------|----------|
| Создание | Имя, описание |
| Редактирование | Изменение имени/описания |
| Удаление | С предупреждением о пользователях |
| Назначение | Множественное назначение пользователю |

### 5.2. Назначение ролей

**Интерфейс:**
- Две колонки: "Назначенные роли" / "Доступные роли"
- Кнопки Add/Remove
- Визуальная индикация (цветные бейджи)
- Мгновенное обновление через JavaScript

### 5.3. Привязка ролей к контроллерам

**Страница /Access/Index:**
- Таблица: Контроллер | Имя | Режим | Роли | Действия
- Быстрое добавление/удаление ролей
- Переключение AllowAllAuthenticated
- Полное редактирование с превью

---

## 6. Управление доступом

### 6.1. Контроллеры

| Контроллер | Назначение | CRUD |
|-----------|-----------|------|
| Admin | Пользователи | Полный |
| Role | Роли | Полный |
| Access | Правила доступа | Чтение + редактирование |
| Audit | Журнал аудита | Только чтение |
| MonitoringPost | Посты мониторинга | Полный |
| Sensor | Датчики | Полный |
| SensorType | Типы датчиков | Полный |
| PollingSession | Сессии опроса | Чтение + удаление |
| DOVData | Данные видимости | Только чтение |
| DSPDData | Данные дороги | Только чтение |
| DustData | Данные пыли | Только чтение |
| IWSData | Метеоданные | Только чтение |
| MUEKSData | Данные питания | Только чтение |
| SensorResults | Результаты опроса | Только чтение |

### 6.2. Record-Level Permissions

**Для каждого типа записи:**

| Тип | Owner | Admin | Operator | Viewer |
|-----|-------|-------|----------|--------|
| MonitoringPost | CRUD | CRUD | CRU | R |
| Sensor | CRUD | CRUD | CRU | R |
| DOVData | R | CRUD | R | R |
| AuditLog | - | R | - | - |

**Схема:**
```csharp
// Проверка доступа к записи:
if (user.HasRole("Admin")) return true; // Админ — всё
if (record.CreatedBy == user.Id) return true; // Владелец — CRUD
if (user.HasRole("Operator")) return permission.Read; // Оператор — чтение
return false;
```

---

## 7. Аудит

### 7.1. Логирование действий

**Каждый запрос:**
- Кто: UserName, UserId
- Что: Controller.Action
- Когда: Timestamp (UTC)
- Откуда: IP, User-Agent
- Результат: IsSuccess
- Время: ExecutionTimeMs

### 7.2. Логирование изменений

**Каждое изменение данных:**
- Сущность: EntityType, EntityId
- Тип: Added/Modified/Deleted
- Старые значения: OriginalValues (JSON)
- Новые значения: NewValues (JSON)
- Изменённые поля: ChangedProperties (JSON)
- Кто изменил: UserName, UserId

### 7.3. Просмотр аудита

**Страница /Audit/Index:**
- Единая таблица: Action + Change логи
- Фильтры: тип, пользователь, IP, сущность, тип изменения, дата
- Пагинация: 50 записей на страницу
- Детали: полная информация о записи
- Экспорт в CSV/JSON

### 7.4. Хранение и ротация

- Логи хранятся 90 дней
- Ротация: архивирование старых логов в отдельную таблицу
- Очистка: автоматическая по расписанию

---

## 8. Данные мониторинга

### 8.1. Иерархия данных

```
MonitoringPost (пост мониторинга)
  ├── Адрес, координаты, интервал опроса
  ├── Sensor (датчик)
  │     ├── SensorType (тип: DOV/DSPD/Dust/IWS/MUEKS)
  │     ├── Серийный номер, URL, статус
  │     └── Данные (один к одному с типом):
  │           ├── DOVData: видимость, яркость
  │           ├── DSPDData: сцепление, температура дороги, статус
  │           ├── DustData: PM10, PM2.5, PM1
  │           ├── IWSData: температура, влажность, ветер, осадки
  │           └── MUEKSData: питание, дверь, видимость
  └── PollingSession (сессия опроса)
        ├── Статус, время начала/окончания
        ├── Количество датчиков (успех/неуспех)
        └── SensorResult: код ответа, время отклика, тело ответа
```

### 8.2. CRUD операции

**Посты мониторинга:**
- Создание: Name, Description, Address, Coordinates, IsMobile, PollingIntervalSeconds
- Редактирование: всех полей
- Удаление: каскадное (датчики, данные)
- Активация/деактивация

**Датчики:**
- Привязка к посту и типу
- Серийный номер, URL, EndPointsName
- Координаты (опционально)
- Активация/деактивация

**Типы датчиков:**
- Справочник: SensorTypeName, Description

### 8.3. Просмотр данных

**Для каждого типа данных:**
- Таблица с фильтрацией по SensorId
- Пагинация (50 записей)
- Детальная карточка записи
- Сортировка по времени

### 8.4. Агрегация и аналитика (будущее)

- Графики температуры за 24ч/7д/30д
- Средние значения по постам
- Оповещения при превышении порогов
- Экспорт данных в CSV/Excel

---

## 9. API (для интеграций)

### 9.1. Endpoints

```
POST   /api/auth/login          — вход, возвращает JWT
POST   /api/auth/register       — регистрация
POST   /api/auth/refresh        — обновление токена
POST   /api/auth/logout         — выход

GET    /api/users               — список пользователей (Admin)
POST   /api/users               — создание пользователя (Admin)
PUT    /api/users/{id}          — редактирование (Admin)
DELETE /api/users/{id}          — удаление (Admin)

GET    /api/roles               — список ролей (Admin)
POST   /api/roles               — создание роли (Admin)

GET    /api/monitoring-posts    — список постов
POST   /api/monitoring-posts    — создание поста (Operator+)
GET    /api/sensors             — список датчиков
GET    /api/data/dov            — данные DOV (фильтры)
GET    /api/data/dspd           — данные DSPD
GET    /api/data/dust           — данные пыли
GET    /api/data/iws            — метеоданные
GET    /api/data/mueks          — данные питания

GET    /api/audit               — журнал аудита (Admin)
```

### 9.2. Формат ответов

```json
// Успех:
{
  "success": true,
  "data": { ... },
  "pagination": { "page": 1, "pageSize": 50, "total": 100 }
}

// Ошибка:
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Имя пользователя обязательно",
    "details": [{ "field": "userName", "message": "Обязательное поле" }]
  }
}
```

### 9.3. Аутентификация API

```
Authorization: Bearer <jwt_token>
или
X-Api-Key: <api_key>
```

---

## 10. Безопасность

### 10.1. Защита паролей

- PBKDF2 с 10000 итерациями (рекомендуется 600000+)
- 32-байтная соль (RandomNumberGenerator)
- FixedTimeEquals для сравнения (защита от timing attacks)
- Минимальная длина: 8 символов
- Требования: заглавная, строчная, цифра, спецсимвол

### 10.2. Защита JWT

- Подпись HMACSHA256 (в продакшене — RSA/ECDSA)
- Access Token: 15 минут
- Refresh Token: 7 дней, одноразовый
- Чёрный список JTI при выходе
- Валидация: issuer, audience, lifetime, signing key

### 10.3. CSRF защита

- Antiforgery token в формах
- Header: X-CSRF-TOKEN
- Cookie: AntiForgeryCookie (HttpOnly, Secure)

### 10.4. Rate Limiting

```
/auth/login:     5 попыток / 15 минут
/auth/register:  3 регистрации / час / IP
/api/*:          1000 запросов / час (с API key)
default:         100 запросов / минуту
```

### 10.5. CORS

```csharp
// Development:
AllowAnyOrigin, AllowAnyMethod, AllowAnyHeader

// Production:
WithOrigins("https://yourdomain.com")
WithMethods("GET", "POST", "PUT", "DELETE")
WithHeaders("Authorization", "Content-Type")
```

### 10.6. Безопасность заголовков

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'
Strict-Transport-Security: max-age=31536000; includeSubDomains
```

### 10.7. Защита от атак

| Атака | Защита |
|-------|--------|
| Brute Force | Rate Limiting + Account Lockout |
| SQL Injection | EF Core (параметризованные запросы) |
| XSS | Razor auto-encoding + CSP |
| CSRF | Antiforgery tokens |
| Timing Attacks | FixedTimeEquals |
| Session Hijacking | HttpOnly, Secure cookies |
| Clickjacking | X-Frame-Options: DENY |

---

## 11. Интерфейс

### 11.1. Layout

```
┌──────────────────────────────────────────┐
│  Topbar (56px)                           │
│  ☰ Название    [Аватар Имя] 🌙 [Выход]  │
├────────────┬─────────────────────────────┤
│  Sidebar   │  Контент                    │
│  260px     │                             │
│  фикс.     │                             │
│  тёмный    │                             │
│            │                             │
│  🏠 Главная│                             │
│  📍 Посты  │                             │
│  🔧 Датчики│                             │
│  📊 DOV    │                             │
│  📊 DSPD   │                             │
│  📊 Dust   │                             │
│  📊 IWS    │                             │
│  📊 MUEKS  │                             │
│  ──────── │                             │
│  ⚙ Админ ▸│                             │
│    👥 Поль.│                             │
│    🛡 Роли │                             │
│    🔑 Дост.│                             │
│    📋 Аудит│                             │
└────────────┴─────────────────────────────┘
```

### 11.2. Темы

- Светлая (по умолчанию)
- Тёмная (переключатель в topbar)
- Сохранение в localStorage

### 11.3. Адаптивность

- Desktop: sidebar + content
- Tablet: sidebar сворачивается в иконки
- Mobile: sidebar выезжает сбоку (overlay)

### 11.4. Компоненты

- Таблицы: striped, hover, пагинация
- Формы: валидация, подсказки, autocomplete
- Карточки: info, warning, danger
- Бейджи: роли, статусы
- Модальные окна: подтверждение удаления
- Toast-уведомления: результат действий

---

## 12. Тестирование

### 12.1. Unit-тесты

- AuthService: хеширование, проверка, генерация JWT
- ControllerAccessMiddleware: все сценарии доступа
- UserActionLogFilter: логирование действий

### 12.2. Интеграционные тесты

- Auth flow: регистрация → вход → JWT → доступ → выход
- Role management: создание → назначение → проверка доступа
- CRUD: создание → чтение → обновление → удаление

### 12.3. E2E тесты

- Playwright/Cypress сценарии:
  - Полный цикл входа
  - Управление пользователями
  - Просмотр данных мониторинга
  - Переключение темы

---

## 13. Деплой

### 13.1. Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JwtAuthApp.dll"]
```

### 13.2. Docker Compose

```yaml
services:
  app:
    build: .
    ports: ["8080:8080"]
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Database=jwtapp;...
    depends_on: [db]

  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=jwtapp
      - POSTGRES_PASSWORD=secret
    volumes: ["pgdata:/var/lib/postgresql/data"]
```

### 13.3. CI/CD

```
GitHub Actions:
  1. Build → dotnet build
  2. Test → dotnet test
  3. Publish → dotnet publish
  4. Docker build + push
  5. Deploy to server
```

---

## 14. Мониторинг

### 14.1. Метрики

- Количество запросов в минуту
- Время ответа (p50, p95, p99)
- Количество ошибок (4xx, 5xx)
- Количество активных пользователей
- Использование CPU/RAM

### 14.2. Логирование

- Serilog → файл + Console
- Структурированные логи (JSON)
- Уровни: Debug, Information, Warning, Error, Critical

### 14.3. Health Checks

```
GET /health        — общий статус
GET /health/db     — статус БД
GET /health/redis  — статус кэша (если есть)
```

---

## 15. Roadmap

### Phase 1 (текущая)
- [x] JWT аутентификация
- [x] Role-based авторизация
- [x] ControllerAccess middleware
- [x] Управление пользователями
- [x] Управление ролями
- [x] Аудит-логирование
- [x] Данные мониторинга (CRUD)
- [x] Динамическое меню
- [x] Тёмная/светлая тема
- [x] Автообнаружение контроллеров

### Phase 2 (ближайшее)
- [ ] Refresh Token
- [ ] Rate Limiting
- [ ] Account Lockout
- [ ] Email подтверждение
- [ ] API Endpoints (REST)
- [ ] Swagger/OpenAPI документация
- [ ] Unit-тесты

### Phase 3 (среднее)
- [ ] Two-Factor Authentication (TOTP)
- [ ] Record-Level Access Control
- [ ] API Key Authentication
- [ ] Export данных (CSV/Excel)
- [ ] Графики и аналитика
- [ ] Docker + Docker Compose
- [ ] CI/CD pipeline

### Phase 4 (дальнее)
- [ ] WebSocket для обновления данных в реальном времени
- [ ] Push-уведомления при превышении порогов
- [ ] Оптимизация производительности (кэширование, индексы)
- [ ] GDPR compliance (право на удаление)
- [ ] Международизация (i18n)
- [ ] Мобильное приложение (MAUI/Flutter)
