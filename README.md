# ABC Retail Storage App
A .NET 8 MVC web application that demonstrates integration with Azure Storage Services. The app covers real-world use cases like product galleries, customer profiles, contracts/documents, and order tracking, all backed by Azure Blobs, Files, Queues, and Tables.

## 🚀 Features
- Blob Storage: Upload, list, delete, and rename product images or documents.
- File Storage: Manage contract documents with upload, list, delete, and rename functionality.
- Queue Storage: Send, view, and delete order-tracking messages (statuses like Processing, Packed, Shipped).
- Table Storage: Manage customer profiles (CRUD with loyalty tiers and favorite products).

## 🛠 Tech Stack
- Frontend: Razor Views (ASP.NET Core MVC), Bootstrap 5  
- Backend: ASP.NET Core 8 MVC  
- Cloud: Azure Storage SDKs (Blobs, Files, Queues, Tables)  

## 📂 Project Structure
ABCretailStorageApp/  
│── Controllers/        # MVC Controllers (Blob, Files, Queue, Table)  
│── Models/             # CustomerProfile model & DTOs  
│── Services/           # StorageService (Azure SDK integration)  
│── Views/              # Razor Views (UI for Blobs, Files, Queues, Tables)  
│── wwwroot/            # Static files (CSS, JS, Bootstrap)  
│── appsettings.json    # Config (secrets excluded via .gitignore)  
│── Program.cs          # Application startup  

## ⚙️ Setup & Installation
1. Clone the repo:  
   git clone https://github.com/YOUR-USERNAME/ABCretailStorageApp.git  
   cd ABCretailStorageApp  

2. Add your Azure Storage connection string into a local appsettings.Development.json:  
   {  
     "ConnectionStrings": {  
       "AzureStorage": "your-storage-connection-string"  
     }  
   }  

3. Run the app:  
   dotnet run  
   Then open → https://localhost:7262  

## 🔐 Security
- appsettings.json does not include real keys.  
- All secrets should be stored in local config and ignored by .gitignore.  
- GitHub Push Protection ensures secrets cannot be committed.  

## 📜 License
This project is for educational purposes (CLDV6212 coursework). No license applied.
