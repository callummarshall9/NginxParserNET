using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace NginxGUI.TagHelpers
{
    [HtmlTargetElement("td", Attributes = "i-role")]
    public class RoleUsersTH : TagHelper
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleUsersTH(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        
        [HtmlAttributeName("i-role")]
        public string Role { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            List<string> names = new List<string>();
            IdentityRole role = await _roleManager.FindByIdAsync(Role);
            names = _userManager.Users.AsEnumerable().Where(u => _userManager.IsInRoleAsync(u, role.Name).Result)
                .Select(u => u.UserName).ToList();
            output.Content.SetContent(names.Count > 0 ? string.Join(',', names) : "No users attached");
        }
    }
}