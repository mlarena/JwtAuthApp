using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using JwtAuthApp.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace JwtAuthApp.Services
{
public class ControllerDiscoveryService
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public ControllerDiscoveryService(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        public string[] GetControllerNames()
        {
            var controllerActionDescriptors = _actionDescriptorCollectionProvider
                .ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(descriptor => descriptor.ControllerName)
                .Distinct()
                .ToArray();

            return controllerActionDescriptors;
        }
    }
}