# План: Role Management и Access Control

## Текущее состояние
- Роль хранится как строка `User.Role` ("Admin"/"User")
- Нет отдельной сущности Role
- Меню хардкодит `@if (User.IsInRole("Admin"))`
- JWT токен содержит один claim `ClaimTypes.Role`

## Что нужно сделать

### 1. Модели
- `Models/Role.cs` — Id, Name, Description
- `Models/UserRole.cs` —UserId (FK), RoleId (FK), составной PK
- Обновить `User.cs` — навигационное свойство `ICollection<UserRole>`
- Обновить `ApplicationDbContext` — DbSet<Role>, DbSet<UserRole>, OnModelCreating

### 2. Сервис
- Обновить `AuthService.GenerateJwtToken()` — добавить claims для каждой роли пользователя
- Обновить `AuthController.Login` — загружать роли пользователя перед генерацией токена

### 3. RoleController (CRUD)
- `Controllers/RoleController.cs` — Index, Create, Edit, Delete
- `Views/Role/` — Index, Create, Edit, Delete

### 4. Управление ролями пользователей
- `Controllers/AdminController.cs` — добавить actions: ManageRoles(int userId), AddRole, RemoveRole
- `Views/Admin/ManageRoles.cshtml`

### 5. Access Control
- Обновить атрибуты `[Authorize(Roles = "...")]` на контроллерах
- Добавить в seedRoles роль "Admin" и "User" при старте

### 6. Динамическое меню
- `_Layout.cshtml` — меню отображается на основе ролей пользователя из JWT claims

## Файлы для создания/изменения
### Новые
- Models/Role.cs
- Models/UserRole.cs
- Controllers/RoleController.cs
- Views/Role/Index.cshtml
- Views/Role/Create.cshtml
- Views/Role/Edit.cshtml
- Views/Role/Delete.cshtml
- Views/Admin/ManageRoles.cshtml

### Изменяемые
- Models/User.cs
- Data/ApplicationDbContext.cs
- Services/AuthService.cs
- Controllers/AuthController.cs
- Controllers/AdminController.cs
- ViewModels/EditUserViewModel.cs
- ViewModels/CreateUserViewModel.cs
- Views/Admin/Index.cshtml
- Views/Admin/Edit.cshtml
- Views/Admin/Create.cshtml
- Views/Shared/_Layout.cshtml
- Program.cs
