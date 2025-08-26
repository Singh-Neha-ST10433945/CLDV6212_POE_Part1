using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ABCretailStorageApp.Models;
using ABCretailStorageApp.Services;

namespace ABCretailStorageApp.Controllers
{
    public class TableController : Controller
    {
        private readonly StorageService _storage;
        public TableController(StorageService storage) => _storage = storage;

        // LIST
        public async Task<IActionResult> Index()
        {
            var customers = await _storage.GetCustomersAsync(_storage.TableName);
            return View(customers);
        }

        // CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string email, string fav, string tier)
        {
            var customer = new CustomerProfile
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                FullName = fullName ?? "",
                Email = email ?? "",
                FavoriteProduct = fav ?? "",
                LoyaltyTier = string.IsNullOrWhiteSpace(tier) ? "Bronze" : tier
            };

            await _storage.AddCustomerAsync(_storage.TableName, customer);
            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey)) return NotFound();
            var customer = await _storage.GetCustomerAsync(_storage.TableName, rowKey);
            if (customer is null) return NotFound();
            return View(customer);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string rowKey, string fullName, string email, string fav, string tier)
        {
            if (string.IsNullOrWhiteSpace(rowKey)) return NotFound();

            var customer = await _storage.GetCustomerAsync(_storage.TableName, rowKey);
            if (customer is null) return NotFound();

            customer.FullName = fullName ?? "";
            customer.Email = email ?? "";
            customer.FavoriteProduct = fav ?? "";
            customer.LoyaltyTier = string.IsNullOrWhiteSpace(tier) ? "Bronze" : tier;

            await _storage.UpdateCustomerAsync(_storage.TableName, customer);
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (!string.IsNullOrWhiteSpace(rowKey))
                await _storage.DeleteCustomerAsync(_storage.TableName, rowKey);

            return RedirectToAction(nameof(Index));
        }
    }
}
