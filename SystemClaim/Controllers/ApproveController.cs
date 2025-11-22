using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Data;
using SystemClaim.Models;

namespace SystemClaim.Controllers
{
    public class ApproveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApproveController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Approve()
        {
            var claims = await _context.Claims
                .Where(c => c.Status == "Verified")
                .ToListAsync();

            return View(claims);
        }

        // Manual approval
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> PmApprove(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "PM Approved";
            _context.Update(claim);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Approve));
        }

        // Manual rejection
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> PmReject(int id, string rejectReason)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = "PM Rejected";
            claim.RejectReason = rejectReason;
            _context.Update(claim);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Approve));
        }

        // Auto-approve claims that meet guidelines
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> AutoApprove()
        {
            var claims = await _context.Claims
                .Where(c => c.Status == "Verified")
                .ToListAsync();

            int approvedCount = 0;

            foreach (var claim in claims)
            {
                // Guidelines: name, surname, department not empty; rate >0; jobs >0; total correct
                if (!string.IsNullOrWhiteSpace(claim.Name)
                    && !string.IsNullOrWhiteSpace(claim.Surname)
                    && !string.IsNullOrWhiteSpace(claim.Department)
                    && claim.RatePerJob > 0
                    && claim.NumberOfJobs > 0
                    && claim.TotalAmount == claim.RatePerJob * claim.NumberOfJobs)
                {
                    claim.Status = "PM Approved";
                    _context.Update(claim);
                    approvedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { approvedCount });
        }
    }
}
