using Microsoft.AspNetCore.Mvc;
using ABCretailStorageApp.Services;

namespace ABCretailStorageApp.Controllers
{
    public class FilesController : Controller
    {
        private readonly StorageService _storage;
        public FilesController(StorageService storage) => _storage = storage;

        // LIST
        public async Task<IActionResult> Index()
        {
            var files = await _storage.ListFilesAsync(_storage.FileShare);
            return View(files);
        }

        // UPLOAD
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is not null && file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                await _storage.UploadFileAsync(_storage.FileShare, file.FileName, stream);
            }
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _storage.DeleteFileAsync(_storage.FileShare, name);
            }
            return RedirectToAction(nameof(Index));
        }

        // RENAME (GET) – not used by inline form, but kept for completeness
        [HttpGet]
        public IActionResult Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return NotFound();
            ViewBag.OldName = name;
            return View();
        }

        // RENAME (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(string oldName, string newName)
        {
            if (!string.IsNullOrWhiteSpace(oldName) &&
                !string.IsNullOrWhiteSpace(newName) &&
                !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                await _storage.RenameFileAsync(_storage.FileShare, oldName, newName);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
