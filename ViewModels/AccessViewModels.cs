namespace JwtAuthApp.ViewModels
{
    public class ControllerAccessEditViewModel
    {
        public int Id { get; set; }
        public string ControllerName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool AllowAllAuthenticated { get; set; }
        public List<RoleCheckViewModel> Roles { get; set; } = new();
    }

    public class RoleCheckViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
