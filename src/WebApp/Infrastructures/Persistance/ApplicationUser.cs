
 
using Microsoft.AspNetCore.Identity;

namespace BeasiswaIn.Web.Infrastructures.Persistance;

internal sealed class ApplicationUser : IdentityUser
{
    internal ApplicationUser()
    {
        Id = Ulid.NewUlid().ToString();
    }
}