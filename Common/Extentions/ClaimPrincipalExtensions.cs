using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extentions
{
    public static class ClaimPrincipalExtensions
    {
        public static T? GetClaimValue<T>(this ClaimsPrincipal user, string claim)
        {
            var value = user.Claims.FirstOrDefault(x => x.Type == claim)?.Value;
            if (value != null)
                return Utils.Convert<T>(value);
            else
                return default;
        }
    }
}
