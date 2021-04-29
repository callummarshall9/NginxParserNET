using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using NginxGUI.Dtos;

namespace NginxGUI.Data
{
    public class RoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public List<IdentityRole> GetRoles() => _roleManager.Roles.ToList();

        public async Task<UpdateRoleDto> GetRoleUsers(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            var users = _userManager.Users.AsEnumerable();
            return new UpdateRoleDto
            {
                RoleName = role.Name,
                RoleId = role.Id,
                Users = users.Select(async u => new UpdateUserEntry
                {
                    InRole = await _userManager.IsInRoleAsync(u, role.Name),
                    UserName = u.UserName,
                    UserId = u.Id
                }).Select(r => r.Result).ToList()
            };
        }

        public async Task<IdentityResult> CreateRole(string roleName) => await _roleManager.CreateAsync(new IdentityRole(roleName));

        public async Task<IEnumerable<IdentityResult>> UpdateRole(UpdateRoleDto model)
        {
            var databaseUsersInRole = await _userManager.GetUsersInRoleAsync(model.RoleId);
            var usersSent = model.Users.ToList();
            var usersToRemove = databaseUsersInRole
                .Where(du => usersSent.All(us => du.UserName != us.UserName))
                .ToList();
            var usersToAdd = usersSent
                .Where(us => databaseUsersInRole.All(du => du.UserName != us.UserName))
                .ToList();
            var results = usersToRemove
                .Select(async u => await _userManager.RemoveFromRoleAsync(u, model.RoleName))
                .Select(r => r.Result)
                .Union(
                    usersToAdd
                        .Select(async u => await _userManager.AddToRoleAsync(await _userManager.FindByIdAsync(u.UserId), model.RoleName))
                        .Select(r => r.Result)
                );
            return results;
        } 
        public async Task<IdentityResult> DeleteRole(string roleId) => await _roleManager.DeleteAsync(await _roleManager.FindByIdAsync(roleId));
        
    }
}