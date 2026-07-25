using Microsoft.AspNetCore.Authorization;

namespace JwtAuthApp.Authorization
{
    public class ControllerAccessRequirement : IAuthorizationRequirement
    {
        public string ControllerName { get; }

        public ControllerAccessRequirement(string controllerName)
        {
            ControllerName = controllerName;
        }
    }
}
