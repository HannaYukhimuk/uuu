using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace UserManagementApp.Models
{
    public class User : IdentityUser
    {        
        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        public DateTime LastLoginTime { get; set; } = DateTime.UtcNow;
        
        public bool IsBlocked { get; set; } = false;
    }
}