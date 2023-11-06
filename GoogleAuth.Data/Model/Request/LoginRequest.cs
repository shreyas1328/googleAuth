using System;
using System.ComponentModel.DataAnnotations;

namespace GoogleAuth.Data.Model.Request
{
	public class LoginRequest
	{
        [Required]
        public string? Email { get; set; }

        [Required]
        public string? Provider { get; set; }

        [Required]
        public string? AccessToken { get; set; }
    }
}

