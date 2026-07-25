# План: Управление доступом к контроллерам — РЕАЛИЗОВАНО

## Архитектура

### ControllerAccessMiddleware
Проверяет доступ к контроллерам на основе таблицы `ControllerAccess`:
1. Извлекает имя контроллера из RouteData
2. Ищет запись в `ControllerAccess`
3. Если AllowAllAuthenticated = true → доступ разрешён
4. Если есть роли → проверяет JWT claims пользователя
5. Если нет доступа → 403 Forbidden (JSON для API, redirect для браузера)

### MenuViewComponent
Динамически рендерит меню навигации:
- Запрашивает `ControllerAccess` + роли пользователя
- Показывает только те пункты, к которым есть доступ
- Admin section видна только для роли Admin

## Созданные файлы

| Файл | Описание |
|------|----------|
| `Models/ControllerAccess.cs` | Модель (Id, ControllerName, DisplayName, Description, AllowAllAuthenticated) |
| `Models/ControllerAccessRole.cs` | Связь many-to-many ControllerAccess↔Role |
| `Middleware/ControllerAccessMiddleware.cs` | Проверка доступа к контроллерам |
| `ViewComponents/MenuViewComponent.cs` | Динамическое меню |
| `Views/Shared/Components/Menu/Default.cshtml` | Шаблон меню |
| `Controllers/AccessController.cs` | CRUD правил доступа |
| `Views/Access/Index.cshtml` | Список контроллеров с правами |
| `Views/Access/Edit.cshtml` | Редактирование прав (с превью) |
| `ViewModels/AccessViewModels.cs` | ViewModel для Access |
| `PLAN_ACCESS_CONTROL.md` | Этот план |

## Изменённые файлы

| Файл | Изменения |
|------|-----------|
| `Models/Role.cs` | Добавлено `ICollection<ControllerAccessRole>` |
| `Data/ApplicationDbContext.cs` | DbSet ControllerAccesses/Roles, конфигурация |
| `Program.cs` | Регистрация middleware, seed ControllerAccess |
| `Views/Shared/_Layout.cshtml` | `@await Component.InvokeAsync("Menu")` |
| Controllers (все кроме Auth) | Убран `[Authorize(Roles = "Admin")]` → `[Authorize]` |

## Seed при старте

Контроллеры с ролью доступа:
| Controller | Display | AllowAll | Roles |
|-----------|---------|----------|-------|
| Secure | Secure | false | User, Admin |
| Test | Test | false | User, Admin |
| MonitoringPost | MonitoringPost | false | User, Admin |
| Sensor | Sensors | false | User, Admin |
| DataIWS | DataIWS | false | User, Admin |
| Admin | Users | true | — |
| Role | Roles | true | — |
| Access | Access Control | true | — |
| Audit | Audit Log | true | — |

## Как управлять

1. **Admin → Access Control** — список всех контроллеров
2. Кнопка ** globe ** — включить/выключить AllowAll
3. Кнопка ** + ** (dropdown) — добавить роль
4. Бейдж роли с ** x ** — удалить роль
5. Кнопка ** Edit ** — полное редактирование с превью
