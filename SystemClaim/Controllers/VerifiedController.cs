using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Data;
using SystemClaim.Models;

namespace SystemClaim.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class VerifiedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VerifiedController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Home page (optional)
        public IActionResult Index()
        {
            return View();
        }

        // Page to see all pending claims
        public async Task<IActionResult> Verification()
        {
            var claims = await _context.Claims
                .Where(c => c.Status == "Submitted")
                .ToListAsync();

            return View(claims);
        }

        // Manual Verify a single claim
        public async Task<IActionResult> PcVerify(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "Verified";
            _context.Update(claim);
            await _context.SaveChangesAsync();

            return Ok(new { id, status = "Verified" });
        }

        // Manual Reject a single claim with a reason
        [HttpPost]
        public async Task<IActionResult> PcReject(int id, string reason)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "Rejected";
            claim.RejectReason = reason;
            _context.Update(claim);
            await _context.SaveChangesAsync();

            return Ok(new { id, status = "Rejected", reason });
        }

        // Auto verify all claims based on guidelines
        [HttpPost]
        public async Task<IActionResult> AutoVerify()
        {
            var claims = await _context.Claims
                .Where(c => c.Status == "Submitted")
                .ToListAsync();

            int verifiedCount = 0;

            foreach (var claim in claims)
            {
                // Guidelines: auto-verify if valid
                bool isValid = !string.IsNullOrEmpty(claim.Name) &&
                               !string.IsNullOrEmpty(claim.Surname) &&
                               !string.IsNullOrEmpty(claim.Department) &&
                               claim.RatePerJob > 0 &&
                               claim.NumberOfJobs > 0 &&
                               claim.TotalAmount == claim.RatePerJob * claim.NumberOfJobs;

                if (isValid)
                {
                    claim.Status = "Verified";
                    _context.Update(claim);
                    verifiedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { verifiedCount });
        }
    }
}
