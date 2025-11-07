using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using StudyPlannerApi.Data;
using StudyPlannerApi.Controllers;
using StudyPlannerApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Flag to track if we're using PostgreSQL
bool isPostgreSQL = false;



// Convert PostgreSQL URI format to standard format if needed
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
{
    connectionString = ConvertPostgresUriToConnectionString(connectionString);
    isPostgreSQL = true; // We know it's PostgreSQL if it started with postgresql://
}
else if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
{
    isPostgreSQL = true; // Also check for "postgres" in the string
}

// Configure database context based on the connection string
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured.");
}

if (isPostgreSQL)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Add Semantic Kernel services with OpenAI
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";

if (string.IsNullOrEmpty(openAiApiKey))
{
    throw new InvalidOperationException("OpenAI API Key is not configured. Set OpenAI:ApiKey in configuration.");
}

builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: openAiModel, 
        apiKey: openAiApiKey
    );
    return kernelBuilder.Build();
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Add API Key authentication to Swagger
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key authentication. Add 'X-API-Key' header with your API key.",
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS for production (if you have a frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Also enable Swagger in production for Render (optional - remove if you don't want it)
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors("AllowAll");

// Add API Key Authentication Middleware
app.UseApiKeyAuthentication();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.MapCourseEndpoints();

app.Run();

// Helper method to convert PostgreSQL URI to connection string
static string ConvertPostgresUriToConnectionString(string uri)
{
    var uriBuilder = new UriBuilder(uri);
    var host = uriBuilder.Host;
    var port = uriBuilder.Port > 0 ? uriBuilder.Port : 5432;
    var database = uriBuilder.Path.TrimStart('/');
    var username = uriBuilder.UserName;
    var password = uriBuilder.Password;
    
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
