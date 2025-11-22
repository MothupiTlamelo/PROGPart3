using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Data;
using SystemClaim.Models;

namespace SystemClaim.Controllers
{
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
        public async Task<IActionResult> Claims()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        // POST: Create Claim + Upload File
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(Claims model, IFormFile file)
        {
            // Save the claim first
            model.CreatedAt = DateTime.Now;
            _context.Claims.Add(model);
            await _context.SaveChangesAsync();

            // The claim now exists & has a real ID:
            int claimId = model.Id;

            // If a file was uploaded, save it
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFile = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFile);

                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }

                // Save document record
                var doc = new UploadDocument
                {
                    ClaimID = claimId,               // FIXED: must be Id, not ClaimID
                    FileName = file.FileName,
                    FilePath = "/uploads/" + uniqueFile,
                    UploadDate = DateTime.Now
                };

                _context.UploadDocuments.Add(doc);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Claim created successfully.";

            // Redirect to list of docs for this claim
            return RedirectToAction("List", "UploadDocument", new { claimId = claimId });
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
