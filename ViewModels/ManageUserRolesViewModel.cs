namespace JwtAuthApp.ViewModels
{
    public class ManageUserRolesViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<RoleCheckBoxViewModel> AllRoles { get; set; } = new();
    }

    public class RoleCheckBoxViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
