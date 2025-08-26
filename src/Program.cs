using Microsoft.EntityFrameworkCore;
using Serilog;
using VehicleMaintenanceInvoiceSystem.Data;
using VehicleMaintenanceInvoiceSystem.Models;
using VehicleMaintenanceInvoiceSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Entity Framework
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure options
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection("BlobStorage"));
builder.Services.Configure<FormRecognizerOptions>(
    builder.Configuration.GetSection("FormRecognizer"));

// Register services
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IFormRecognizerService, FormRecognizerService>();
builder.Services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();

// Register new comprehensive processing services
builder.Services.AddScoped<IComprehensiveProcessingService, ComprehensiveProcessingService>();
builder.Services.AddScoped<IInvoiceFallbackService, InvoiceFallbackService>();

// Register Intelligence services
builder.Services.AddScoped<ILineItemClassifier, RuleBasedLineItemClassifier>();
builder.Services.AddScoped<IFieldNormalizer, DictionaryBasedFieldNormalizer>();
builder.Services.AddScoped<IInvoiceIntelligenceService, RuleBasedInvoiceIntelligenceService>();

// Register GPT-4o service with HttpClient
builder.Services.AddHttpClient<IGitHubModelsService, GitHubModelsService>();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Vehicle Maintenance Invoice API", 
        Version = "v1",
        Description = "API for processing and managing vehicle maintenance invoices",
        Contact = new() { Name = "Support Team" }
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS for API access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Enable Swagger in all environments for this demo
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle Maintenance Invoice API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Vehicle Maintenance Invoice API";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();

app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API routes
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    try
    {
        context.Database.Migrate();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error initializing database");
    }
}

Log.Information("Vehicle Maintenance Invoice System starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
