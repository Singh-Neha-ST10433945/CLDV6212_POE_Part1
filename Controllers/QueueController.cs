// File: Controllers/QueueController.cs
using Microsoft.AspNetCore.Mvc;
using ABCretailStorageApp.Services;

namespace ABCretailStorageApp.Controllers
{
    public class QueueController : Controller
    {
        private readonly StorageService _storage;
        public QueueController(StorageService storage) => _storage = storage;

        // LIST (non-destructive)
        public async Task<IActionResult> Index()
        {
            var items = await _storage.PeekMessagesAsync(_storage.QueueName, 32);
            return View(items); // IEnumerable<StorageService.QueueMessageDto>
        }

        // SEND
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string message, string status)
        {
            if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrWhiteSpace(status))
            {
                // Append the status so it gets saved
                var fullMessage = $"{message} - {status}";
                await _storage.SendMessageAsync(_storage.QueueName, fullMessage);
            }
            return RedirectToAction(nameof(Index));
        }


        // DELETE (by id; service fetches pop-receipt)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string messageId)
        {
            await _storage.DeleteMessageByIdAsync(_storage.QueueName, messageId);
            return RedirectToAction(nameof(Index));
        }
    }
}
