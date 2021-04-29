using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace NginxGUI.Models
{
    public class UpdateRoleModel
    {
        public string RoleName { get; set; }
        public string RoleId { get; set; }
        public List<UpdateUserEntry> Users { get; set; }
        
    }

    public class UpdateUserEntry
    {
        public bool InRole { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }
}