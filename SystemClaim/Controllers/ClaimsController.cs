using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Data;
using SystemClaim.Models;

namespace SystemClaim.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClaimsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Show all claims for current user
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Claims()
        {
            // Get the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch claims for this user
            var claims = await _context.Claims
                .Where(c => c.WorkerUserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(claims);
        }

        // GET: Create Claim
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> CreateClaim()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _context.Userss.FirstOrDefaultAsync(p => p.UserId == userId);

            var model = new Claims();

            if (profile != null)
            {
                model.Name = profile.Name;
                model.Surname = profile.Surname;
                model.Department = profile.Department;
                model.RatePerJob = profile.DefaultRatePerJob;
                model.WorkerUserId = userId;   // IMPORTANT
            }

            return View(model);
        }

        // POST: Create Claim 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(Claims model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Fetch profile to repopulate read-only fields
                var profile = await _context.Userss.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    model.Name = profile.Name;
                    model.Surname = profile.Surname;
                    model.Department = profile.Department;
                    model.RatePerJob = profile.DefaultRatePerJob;
                    model.WorkerUserId = userId;
                }

                // Recalculate total
                model.TotalAmount = model.RatePerJob * model.NumberOfJobs;

                // Set initial status
                if (string.IsNullOrEmpty(model.Status))
                {
                    model.Status = "Pending";
                }

                if (!ModelState.IsValid)
                {
                    return View(model); // now the view has all the read-only fields populated
                }

                // Save
                model.CreatedAt = DateTime.Now;
                _context.Claims.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Claim created successfully.";
                return RedirectToAction("Claims");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View(model);
            }
        }




        // View one claim
        public async Task<IActionResult> ViewClaims(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Documents)      // IMPORTANT: show files under that claim
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
                return NotFound();

            return View(claim);
        }

        // Approve claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "Approved";
            await _context.SaveChangesAsync();

            return RedirectToAction("Claims");
        }

        // Reject claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "Rejected";
            claim.RejectReason = reason;
            claim.ReasonRequired = !string.IsNullOrEmpty(reason);

            await _context.SaveChangesAsync();

            return RedirectToAction("Claims");
        }
    }
}
