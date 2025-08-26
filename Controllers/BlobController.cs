using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ABCretailStorageApp.Services;

namespace ABCretailStorageApp.Controllers
{
    public class BlobController : Controller
    {
        private readonly StorageService _storage;
        public BlobController(StorageService storage) => _storage = storage;

        // LIST
        public async Task<IActionResult> Index()
        {
            var blobs = await _storage.ListBlobsAsync(_storage.BlobContainer);
            return View(blobs); // IEnumerable<string>
        }

        // UPLOAD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is not null && file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                await _storage.UploadBlobAsync(_storage.BlobContainer, stream, file.FileName);
            }
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                await _storage.DeleteBlobAsync(_storage.BlobContainer, name);

            return RedirectToAction(nameof(Index));
        }

        // RENAME (GET)
        [HttpGet]
        public IActionResult Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return NotFound();
            ViewBag.OldName = name;
            return View(); // Views/Blob/Rename.cshtml
        }

        // RENAME (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(string oldName, string newName)
        {
            if (!string.IsNullOrWhiteSpace(oldName) && !string.IsNullOrWhiteSpace(newName))
                await _storage.RenameBlobAsync(_storage.BlobContainer, oldName, newName);

            return RedirectToAction(nameof(Index));
        }
    }
}
