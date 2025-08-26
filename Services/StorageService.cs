// File: Services/StorageService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

using Microsoft.Extensions.Configuration;

using ABCretailStorageApp.Models;

namespace ABCretailStorageApp.Services
{
    /// <summary>
    /// Single service surfacing Table, Blob, File Share and Queue operations used by the MVC controllers.
    /// Reads names/connection string from appsettings.json:
    /// 
    ///   "AzureStorage": {
    ///     "ConnectionString": "...",
    ///     "TableName": "CustomerProfiles",
    ///     "BlobContainer": "product-images",
    ///     "QueueName": "order-events",
    ///     "FileShare": "contracts"
    ///   }
    /// </summary>
    public class StorageService
    {
        // ---------- config ----------
        public string ConnectionString { get; }
        public string TableName { get; }
        public string BlobContainer { get; }
        public string QueueName { get; }
        public string FileShare { get; }

        public StorageService(IConfiguration config)
        {
            // Prefer explicit section; fall back to ConnectionStrings if needed
            ConnectionString = config["AzureStorage:ConnectionString"]
                               ?? config.GetConnectionString("AzureStorage")
                               ?? throw new ArgumentNullException(nameof(ConnectionString), "AzureStorage connection string not found.");

            TableName = config["AzureStorage:TableName"] ?? "CustomerProfiles";
            BlobContainer = config["AzureStorage:BlobContainer"] ?? "product-images";
            QueueName = config["AzureStorage:QueueName"] ?? "order-events";
            FileShare = config["AzureStorage:FileShare"] ?? "contracts";
        }

        // =====================================================================
        // TABLES (Customer Profiles)
        // =====================================================================

        private TableClient GetTable(string tableName)
        {
            var svc = new TableServiceClient(ConnectionString);
            var table = svc.GetTableClient(tableName);
            table.CreateIfNotExists();
            return table;
        }

        private static CustomerProfile MapFromEntity(TableEntity e) => new()
        {
            PartitionKey = e.PartitionKey,
            RowKey = e.RowKey,
            FullName = e.GetString(nameof(CustomerProfile.FullName)) ?? "",
            Email = e.GetString(nameof(CustomerProfile.Email)) ?? "",
            FavoriteProduct = e.GetString(nameof(CustomerProfile.FavoriteProduct)) ?? "",
            LoyaltyTier = e.GetString(nameof(CustomerProfile.LoyaltyTier)) ?? ""
        };

        private static TableEntity MapToEntity(CustomerProfile c) => new TableEntity(c.PartitionKey, c.RowKey)
        {
            [nameof(CustomerProfile.FullName)] = c.FullName ?? "",
            [nameof(CustomerProfile.Email)] = c.Email ?? "",
            [nameof(CustomerProfile.FavoriteProduct)] = c.FavoriteProduct ?? "",
            [nameof(CustomerProfile.LoyaltyTier)] = c.LoyaltyTier ?? ""
        };

        public async Task<List<CustomerProfile>> GetCustomersAsync(string tableName)
        {
            var table = GetTable(tableName);
            var results = new List<CustomerProfile>();
            await foreach (var e in table.QueryAsync<TableEntity>(x => x.PartitionKey == "Customer"))
                results.Add(MapFromEntity(e));
            return results.OrderBy(c => c.FullName).ToList();
        }

        public async Task<CustomerProfile?> GetCustomerAsync(string tableName, string rowKey)
        {
            var table = GetTable(tableName);
            try
            {
                var resp = await table.GetEntityAsync<TableEntity>("Customer", rowKey);
                return MapFromEntity(resp.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddCustomerAsync(string tableName, CustomerProfile customer)
        {
            var table = GetTable(tableName);
            customer.PartitionKey = "Customer";
            customer.RowKey ??= Guid.NewGuid().ToString();
            await table.UpsertEntityAsync(MapToEntity(customer), TableUpdateMode.Replace);
        }

        public async Task UpdateCustomerAsync(string tableName, CustomerProfile customer)
        {
            var table = GetTable(tableName);
            await table.UpsertEntityAsync(MapToEntity(customer), TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string tableName, string rowKey)
        {
            var table = GetTable(tableName);
            try { await table.DeleteEntityAsync("Customer", rowKey); } catch { /* ignore 404 */ }
        }

        // =====================================================================
        // BLOBS (product images)
        // =====================================================================

        private BlobContainerClient GetContainer(string containerName)
        {
            var container = new BlobContainerClient(ConnectionString, containerName);
            container.CreateIfNotExists(PublicAccessType.None);
            return container;
        }

        public async Task<List<string>> ListBlobsAsync(string containerName)
        {
            var c = GetContainer(containerName);
            var list = new List<string>();
            await foreach (var item in c.GetBlobsAsync())
                list.Add(item.Name);
            return list.OrderBy(x => x).ToList();
        }

        public async Task UploadBlobAsync(string containerName, Stream stream, string blobName)
        {
            var c = GetContainer(containerName);
            var blob = c.GetBlobClient(blobName);
            await blob.UploadAsync(stream, overwrite: true);
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var c = GetContainer(containerName);
            var blob = c.GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync();
        }

        public async Task RenameBlobAsync(string containerName, string oldName, string newName)
        {
            if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

            var c = GetContainer(containerName);
            var src = c.GetBlobClient(oldName);
            var dst = c.GetBlobClient(newName);

            // copy then delete
            var op = await dst.StartCopyFromUriAsync(src.Uri);
            await op.WaitForCompletionAsync();
            await src.DeleteIfExistsAsync();
        }

        // =====================================================================
        // FILES (Azure File Share) – contracts & documents
        // =====================================================================

        private ShareDirectoryClient GetRootDirectory(string shareName)
        {
            var share = new ShareClient(ConnectionString, shareName);
            share.CreateIfNotExists();
            return share.GetRootDirectoryClient();
        }

        public async Task<List<string>> ListFilesAsync(string shareName)
        {
            var dir = GetRootDirectory(shareName);
            var names = new List<string>();
            await foreach (var item in dir.GetFilesAndDirectoriesAsync())
                if (!item.IsDirectory) names.Add(item.Name);
            return names.OrderBy(x => x).ToList();
        }

        public async Task UploadFileAsync(string shareName, string fileName, Stream stream)
        {
            var dir = GetRootDirectory(shareName);
            var file = dir.GetFileClient(fileName);
            await file.CreateAsync(stream.Length);
            await file.UploadRangeAsync(new HttpRange(0, stream.Length), stream);
        }

        public async Task DeleteFileAsync(string shareName, string fileName)
        {
            var dir = GetRootDirectory(shareName);
            var file = dir.GetFileClient(fileName);
            await file.DeleteIfExistsAsync();
        }

        public async Task RenameFileAsync(string shareName, string oldName, string newName)
        {
            if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

            var dir = GetRootDirectory(shareName);
            var file = dir.GetFileClient(oldName);

            // Azure Files supports native rename
            await file.RenameAsync(destinationPath: newName);
        }

        // =====================================================================
        // QUEUES (order events)
        // =====================================================================

        public record QueueMessageDto(
    string Id,
    string? PopReceipt,
    string Text,
    DateTimeOffset? InsertedOn
);


        private QueueClient GetQueue(string queueName)
        {
            var q = new QueueClient(ConnectionString, queueName);
            q.CreateIfNotExists();
            return q;
        }

        /// <summary>Peek (non-destructive) for UI display so messages remain visible after Send.</summary>
        public async Task<List<QueueMessageDto>> PeekMessagesAsync(string queueName, int max = 32)
        {
            var q = GetQueue(queueName);
            var peeked = await q.PeekMessagesAsync(max);
            return peeked.Value
                         .Select(m => new QueueMessageDto(m.MessageId, null, m.MessageText, m.InsertedOn))
                         .ToList();
        }


        public async Task SendMessageAsync(string queueName, string message)
        {
            var q = GetQueue(queueName);
            await q.SendMessageAsync(message ?? "");
        }

        /// <summary>
        /// Delete by id: fetch a temporary pop-receipt via a short Receive and delete the matching message.
        /// </summary>
        public async Task DeleteMessageByIdAsync(string queueName, string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return;

            var q = GetQueue(queueName);

            // try two short passes (helps when many messages are present)
            for (var i = 0; i < 2; i++)
            {
                var batch = await q.ReceiveMessagesAsync(maxMessages: 32, visibilityTimeout: TimeSpan.FromSeconds(5));
                var match = batch.Value.FirstOrDefault(m => m.MessageId == messageId);
                if (match != null)
                {
                    await q.DeleteMessageAsync(match.MessageId, match.PopReceipt);
                    return;
                }
            }
        }
    }
}
