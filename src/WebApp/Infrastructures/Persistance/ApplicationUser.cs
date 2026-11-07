
 
using Microsoft.AspNetCore.Identity;

namespace BeasiswaIn.Web.Infrastructures.Persistance;

public sealed class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        Id = Ulid.NewUlid().ToString();
    }
}