# Git Setup Commands for MaintenancePlus Repository

## 🚀 Quick Setup for Existing Repository

Since you already have the repository at https://github.com/adriankd/MaintenancePlus, here are the commands to push your work:

### Option 1: Fresh Clone and Copy Files (Recommended)
```bash
# Clone your existing repository
git clone https://github.com/adriankd/MaintenancePlus.git
cd MaintenancePlus

# Copy your project files to the cloned repository
# (You'll need to manually copy files from c:\Training\fwmainplus to the cloned directory)

# Add all files to git
git add .

# Commit your work
git commit -m "Vehicle Maintenance Invoice Processing System

Features:
- ASP.NET Core 8.0 MVC application with Azure integration
- Azure Form Recognizer for OCR and invoice data extraction
- Azure Blob Storage for secure file storage
- Azure SQL Database with Entity Framework Core
- Complete invoice upload and processing workflow
- REST API with Swagger documentation
- Responsive web interface with Bootstrap
- Database schema fix for decimal casting issues

Technical highlights:
- Comprehensive error handling and logging
- Automated OCR processing with confidence scoring
- Secure file upload with validation
- Pagination and search functionality
- Professional documentation and deployment guides"

# Push to GitHub
git push origin main
```

### Option 2: Add Remote to Existing Directory
```bash
# Navigate to your project directory
cd c:\Training\fwmainplus

# Initialize git if not already done
git init

# Add your repository as remote
git remote add origin https://github.com/adriankd/MaintenancePlus.git

# Pull any existing content from your repository
git pull origin main --allow-unrelated-histories

# Add all your project files
git add .gitignore
git add README.md
git add LICENSE
git add src/appsettings.Example.json

# Add source code
git add src/Controllers/
git add src/Models/
git add src/Services/
git add src/Data/
git add src/Views/
git add src/wwwroot/
git add src/Program.cs
git add src/VehicleMaintenanceInvoiceSystem.csproj
git add src/Dockerfile

# Add documentation
git add *.md
git add *.sql
git add nuget.config

# Add test files
git add TestInvoices/

# Commit changes
git commit -m "Complete Vehicle Maintenance Invoice Processing System

This commit includes:
✅ Full ASP.NET Core 8.0 application
✅ Azure integration (SQL, Blob Storage, Form Recognizer)
✅ Invoice upload and OCR processing
✅ REST API with Swagger documentation
✅ Responsive web interface
✅ Comprehensive documentation
✅ Database schema with decimal fixes applied
✅ Sample test files and deployment guides

Ready for production deployment on Azure App Service."

# Push to GitHub
git push -u origin main
```

## 📁 Files Being Uploaded to GitHub

### ✅ Source Code (Safe to commit)
- `src/` - Complete application source code
- `TestInvoices/` - Sample PDF files for testing
- `*.md` - All documentation files
- `*.sql` - Database schema and migration scripts
- `.gitignore` - Git ignore rules
- `LICENSE` - MIT license
- `src/appsettings.Example.json` - Template configuration

### ❌ Excluded (Contains secrets)
- `src/appsettings.json` - Real connection strings and API keys
- `src/bin/`, `src/obj/` - Build artifacts
- `*.ps1` scripts - Personal development tools
- `src/logs/` - Runtime logs

## 🔒 Important Security Notes

1. **appsettings.json is NOT uploaded** - Contains your real Azure connection strings and API keys
2. **appsettings.Example.json IS uploaded** - Template for others to configure their own settings
3. **Personal scripts are excluded** - Your development and testing scripts stay local

## 🎯 Next Steps After Upload

1. **Update Repository Settings**:
   - Go to https://github.com/adriankd/MaintenancePlus/settings
   - Add description: "Vehicle Maintenance Invoice Processing System - ASP.NET Core 8.0 with Azure AI"
   - Add topics: `aspnet-core`, `azure`, `invoice-processing`, `ocr`, `csharp`, `form-recognizer`

2. **Enable Features**:
   - Enable Issues for bug tracking
   - Enable Discussions for Q&A
   - Consider enabling Wiki for additional documentation

3. **Create Release**:
   ```bash
   # Tag your first release
   git tag -a v1.0.0 -m "Initial release: Vehicle Maintenance Invoice Processing System"
   git push origin v1.0.0
   ```

Your repository will be professional, secure, and ready for collaboration!
