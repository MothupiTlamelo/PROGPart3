using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Data;
using SystemClaim.Models;

namespace SystemClaim.Controllers
{
    [Authorize(Roles = "HR,Lecturer")] // Adjust roles as needed
    public class UploadDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UploadDocumentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: UploadDocument/List/5  (list documents for a specific claim)
        public async Task<IActionResult> List(int claimId)
        {
            var documents = await _context.UploadDocuments
                .Where(d => d.ClaimID == claimId)
                .Include(d => d.Claim)
                .ToListAsync();

            ViewBag.ClaimId = claimId;
            return View(documents);
        }

        // GET: UploadDocument/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var document = await _context.UploadDocuments
                .Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null) return NotFound();

            return View(document);
        }

        // GET: UploadDocument/Create/5
        public IActionResult Create(int claimId)
        {
            var model = new UploadDocument
            {
                ClaimID = claimId
            };
            return View(model);
        }

        // POST: UploadDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UploadDocument model, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Save file to wwwroot/uploads
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Save record to database
                model.FileName = file.FileName;
                model.FilePath = $"/uploads/{uniqueFileName}";
                model.UploadDate = DateTime.Now;

                _context.UploadDocuments.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Document uploaded successfully.";
                return RedirectToAction(nameof(List), new { claimId = model.ClaimID });
            }

            return View(model);
        }

        // GET: UploadDocument/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.UploadDocuments
                .Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null) return NotFound();

            return View(document);
        }

        // POST: UploadDocument/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.UploadDocuments.FindAsync(id);
            if (document != null)
            {
                // Delete physical file
                var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);

                _context.UploadDocuments.Remove(document);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Document deleted successfully.";
            }

            return RedirectToAction(nameof(List), new { claimId = document.ClaimID });
        }
    }
}
