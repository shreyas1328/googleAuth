using Microsoft.AspNetCore.Identity;

namespace GoogleAuth.Data.Model.Entity
{
	public class User : IdentityUser
    {
        public string Provider { get; set; } = null!;
    }
}

