using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Data;
using SystemClaim.Models;

namespace SystemClaim.Controllers
{
    [Authorize(Roles = "HR")]

    public class HrController : Controller
    {

        private readonly ApplicationDbContext _context;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;


        public HrController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {

            _context = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        public async Task<IActionResult> HrDashboard()
        {
            var employees = await _context.Userss.ToListAsync();
            return View(employees);
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Userss.FirstOrDefaultAsync(u => u.Id == id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(HrDashboard));
            }
            return View(employee);
        }

        // GET: Hr/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Userss.FirstOrDefaultAsync(u => u.Id == id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(HrDashboard));
            }

            var model = new RegisterViewModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Surname = employee.Surname,
                Department = employee.Department,
                DefaultRatePerJob = employee.DefaultRatePerJob,
                RoleName = employee.RoleName,
                Email = employee.Email,
                Password = employee.password
            };

            return View(model);
        }


        // POST: Hr/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var employee = await _context.Userss.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (employee == null) return NotFound();

            // Update EF entity
            employee.Name = model.Name;
            employee.Surname = model.Surname;
            employee.Department = model.Department;
            employee.DefaultRatePerJob = model.DefaultRatePerJob;
            employee.RoleName = model.RoleName;
            employee.Email = model.Email;
            employee.password = model.Password;

            _context.Userss.Update(employee);
            await _context.SaveChangesAsync();

            // Update Identity role
            var identityUser = await _userManager.FindByIdAsync(employee.UserId);
            if (identityUser != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(identityUser);
                await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                await _userManager.AddToRoleAsync(identityUser, model.RoleName);
            }

            TempData["SuccessMessage"] = "Employee updated successfully.";
            return RedirectToAction(nameof(HrDashboard));
        }


        public async Task<IActionResult> ListEmployee()
        {

            var employees = await _context.Userss
                .ToListAsync();


            return View(employees);
        }


        public IActionResult CreateEmployee()
        {
            return View(new RegisterViewModel());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(RegisterViewModel model)
        {
            // Check if role exists
            var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
            if (!roleExists)
            {
                ModelState.AddModelError("", $"Role '{model.RoleName}' does not exist.");
            }

            if (ModelState.IsValid)
            {
                // Create IdentityUser first
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);

                if (createResult.Succeeded)
                {
                    // Assign role
                    await _userManager.AddToRoleAsync(user, model.RoleName);

                    // Create EF profile with Email included
                    var profile = new User
                    {
                        UserId = user.Id,      // IdentityUser Id
                        Name = model.Name,
                        Surname = model.Surname,
                        Department = model.Department,
                        DefaultRatePerJob = model.DefaultRatePerJob,
                        RoleName = model.RoleName,
                        Email = model.Email,
                        password = model.Password
                    };

                    _context.Userss.Add(profile);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Employee created successfully.";
                    return RedirectToAction(nameof(ListEmployee));
                }

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }


        // GET: Hr/Delete/5
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Userss.FirstOrDefaultAsync(u => u.Id == id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Hr/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Userss.FirstOrDefaultAsync(u => u.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            // Remove Identity user first
            var identityUser = await _userManager.FindByIdAsync(employee.UserId);
            if (identityUser != null)
            {
                await _userManager.DeleteAsync(identityUser);
            }

            // Remove EF user record
            _context.Userss.Remove(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(HrDashboard)); // or ListEmployee
        }


        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HrSummary()
        {
            var claims = await _context.Claims.ToListAsync();

            var totalCount = claims.Count;
            var totalAmount = claims.Sum(c => c.TotalAmount);
            var submittedCount = claims.Count(c => c.Status == "Submitted");
            var pmRejectedCount = claims.Count(c => c.Status == "PM Rejected");
            var cmRejectedCount = claims.Count(c => c.Status == "CM Rejected");
            var approvedCount = claims.Count(c => c.Status == "CM Approved");
            var paidCount = claims.Count(c => c.Status == "Paid");

            ViewBag.TotalCount = totalCount;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.SubmittedCount = submittedCount;
            ViewBag.PmRejectedCount = pmRejectedCount;
            ViewBag.CmRejectedCount = cmRejectedCount;
            ViewBag.ApprovedCount = approvedCount;
            ViewBag.PaidCount = paidCount;

            return View();
        }

        [Authorize(Roles = "HR")]
        public async Task<FileResult> HrExportCsv()
        {
            var claims = await _context.Claims
                .OrderBy(c => c.Id)
                .ToListAsync();

            var lines = new List<string>();
            lines.Add("Id,Date,Name,Surname,Department,RatePerJob,NumberOfJobs,TotalAmount,Status,RejectReason");

            foreach (var c in claims)
            {
                var line =
                    $"{c.Id}," +
                    $"{c.CreatedAt:yyyy-MM-dd}," +
                    $"{c.Name}," +
                    $"{c.Surname}," +
                    $"{c.Department}," +
                    $"{c.RatePerJob}," +
                    $"{c.NumberOfJobs}," +
                    $"{c.TotalAmount}," +
                    $"{c.Status}," +
                    $"{c.RejectReason?.Replace(",", " ")}";

                lines.Add(line);
            }

            var csv = string.Join("\n", lines);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", "claims_export.csv");
        }


    }
}

