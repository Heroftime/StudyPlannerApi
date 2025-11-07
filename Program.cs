using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using StudyPlannerApi.Data;
using StudyPlannerApi.Controllers;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Support both SQL Server and PostgreSQL based on connection string
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    throw new InvalidOperationException("Database connection string is not configured.");
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
builder.Services.AddSwaggerGen();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.MapCourseEndpoints();

app.Run();
