using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NginxGUI.Models;

namespace NginxGUI.Controllers
{
    [Authorize]
    public class RoleController: Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public IActionResult Index() => View(_roleManager.Roles);

        private void Errors(IdentityResult result) 
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        

        [HttpPost]
        public async Task<IActionResult> Create([Required]string name)
        {
            if (ModelState.IsValid)
            {
                IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(name));
                if (result.Succeeded)
                    return RedirectToAction("Index");
                Errors(result);
            }
            return View(name);
        }

        [HttpGet]
        public IActionResult Create() => View();

        public async Task<IActionResult> Update(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            var users = _userManager.Users.AsEnumerable();
            return View(new UpdateRoleModel
            {
                RoleName = role.Name,
                RoleId = role.Id,
                Users = users.Select(async u => new UpdateUserEntry
                {
                    InRole = await _userManager.IsInRoleAsync(u, role.Name),
                    UserName = u.UserName,
                    UserId = u.Id
                }).Select(r => r.Result).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(UpdateRoleModel model)
        {
            if (ModelState.IsValid)
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
                if (results.Any(r => !r.Succeeded))
                {
                    Errors(results.First(r => !r.Succeeded));
                }
                return RedirectToAction("Index");
            }
            return await Update(model.RoleId);
        }
        
        public async Task<IActionResult> Delete(string id)
        {
            IdentityRole role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    Errors(result);
            }
            else
                ModelState.AddModelError("", "No role found");
            return RedirectToAction("Index");
        }
    }
}