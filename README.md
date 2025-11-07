# Study Planner API

A .NET 8 Web API for managing study planning with AI-powered features using OpenAI and Semantic Kernel.

## Features

- Course management
- School management
- Subject management with time tracking
- AI-powered study planning assistance
- PostgreSQL database support
- **🔐 API Key Authentication** - Secure access control
- Docker containerization
- Ready for deployment on Render

## Prerequisites

- .NET 8 SDK
- PostgreSQL database
- OpenAI API key
- API key for authentication

## Local Development Setup

### 1. Clone the repository

```bash
git clone https://github.com/Heroftime/StudyPlannerApi.git
cd StudyPlannerApi
```

### 2. Configure User Secrets

Store sensitive configuration using .NET User Secrets (never commit these values):

```bash
# Database connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "postgresql://user:password@host/database"

# OpenAI API Key
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"

# API Key for authentication (generate a secure random string)
dotnet user-secrets set "ApiKey" "your-secure-api-key-here"
```

**Generate a secure API key:**

PowerShell:
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

Bash/Linux:
```bash
openssl rand -base64 32
```

Or in standard PostgreSQL connection string format:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=studyplanner;Username=postgres;Password=yourpassword;SSL Mode=Prefer"
```

### 3. Run Migrations

```bash
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

The API will be available at `https://localhost:7xxx` (check console output for exact port).

### 5. Test with API Key

All API requests require the `X-API-Key` header:

```bash
curl -H "X-API-Key: your-api-key-here" https://localhost:7xxx/api/Courses
```

Or use Swagger UI:
1. Navigate to `https://localhost:7xxx/swagger`
2. Click the "Authorize" button (lock icon)
3. Enter your API key
4. Test endpoints

## Deployment on Render

### 1. Create a PostgreSQL Database on Render

1. Go to [Render Dashboard](https://dashboard.render.com/)
2. Click "New" → "PostgreSQL"
3. Configure your database and create it
4. Copy the **External Database URL** (format: `postgresql://user:password@host/database`)

### 2. Set Environment Variables on Render

When creating or configuring your Web Service on Render, set these environment variables:

| Variable Name | Value | Description |
|--------------|-------|-------------|
| `ConnectionStrings__DefaultConnection` | Your PostgreSQL External Database URL | Database connection string (either URI or standard format) |
| `ApiKey` | Your secure API key | **NEW:** Required for API authentication (32+ chars recommended) |
| `OpenAI__ApiKey` | Your OpenAI API key | Required for AI features |
| `OpenAI__Model` | `gpt-4o-mini` | Optional (default: gpt-4o-mini) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Already set in render.yaml |
| `ASPNETCORE_URLS` | `http://+:8080` | Already set in render.yaml |

**Generate API Key:**
```powershell
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```
```bash
# Bash/Linux
openssl rand -base64 32
```

**Note:** The connection string supports both formats:
- URI format: `postgresql://user:password@host/database`
- Standard format: `Host=host;Database=db;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true`

### 3. Deploy

Push your code to GitHub:

```bash
git add .
git commit -m "Add API key authentication"
git push origin master
```

Then either:
- Use the `render.yaml` file for automatic deployment
- Or manually connect your GitHub repository in Render Dashboard

### 4. Run Migrations on Render

After deployment, you may need to run migrations. You can do this by:

1. Using Render Shell:
   ```bash
   dotnet ef database update
   ```

2. Or enable automatic migrations by adding this to `Program.cs` (before `app.Run()`):
   ```csharp
   using (var scope = app.Services.CreateScope())
   {
       var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
       db.Database.Migrate();
   }
   ```

## 🔐 API Authentication

All API endpoints (except Swagger UI) require authentication using an API key.

### Request Header

Include this header in all API requests:

```
X-API-Key: your-api-key-here
```

### Examples

**cURL:**
```bash
curl -H "X-API-Key: jYqQ0fKhe6kwyuFz83ZiWnPUHb17gD2A" \
  https://your-service.onrender.com/api/Courses
```

**JavaScript/Fetch:**
```javascript
fetch('https://your-service.onrender.com/api/Courses', {
  headers: {
    'X-API-Key': 'jYqQ0fKhe6kwyuFz83ZiWnPUHb17gD2A'
  }
})
```

**C# HttpClient:**
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "jYqQ0fKhe6kwyuFz83ZiWnPUHb17gD2A");
var response = await client.GetAsync("https://your-service.onrender.com/api/Courses");
```

**Postman:**
1. Add a header with key: `X-API-Key`
2. Set value to your API key

### Testing with Swagger

1. Navigate to `/swagger`
2. Click the "Authorize" button (lock icon) at the top
3. Enter your API key
4. Click "Authorize"
5. All requests will now include your API key

## API Endpoints

All endpoints require the `X-API-Key` header.

### Courses
- `GET /api/Courses` - Get all courses
- `GET /api/Courses/{id}` - Get course by ID
- `POST /api/Courses` - Create a new course
- `PUT /api/Courses/{id}` - Update a course
- `DELETE /api/Courses/{id}` - Delete a course

### Schools
- `GET /api/School` - Get all schools
- `GET /api/School/{id}` - Get school by ID
- `POST /api/School` - Create a new school
- `PUT /api/School/{id}` - Update a school
- `DELETE /api/School/{id}` - Delete a school

### Subjects
- `GET /api/Subjects` - Get all subjects
- `GET /api/Subjects/{id}` - Get subject by ID
- `POST /api/Subjects` - Create a new subject
- `PUT /api/Subjects/{id}` - Update a subject
- `DELETE /api/Subjects/{id}` - Delete a subject

### AI Features
- `POST /api/Ai/generate-study-plan` - Generate AI-powered study plan

### Health Check
- `GET /api/Home` - Health check endpoint

## Configuration Files

- `appsettings.json` - Application configuration (no secrets)
- `secrets.json` - Local development secrets (git-ignored)
- `render.yaml` - Render deployment configuration
- `Dockerfile` - Docker container configuration

## Security Notes

⚠️ **Important:** Never commit sensitive information to Git:
- Database connection strings with credentials
- API keys (authentication and OpenAI)
- Passwords

Use:
- **Local Development:** .NET User Secrets
- **Production (Render):** Environment Variables

### API Key Security
- Generate strong, random API keys (32+ characters)
- Use different keys for development and production
- Rotate keys regularly
- Share keys only with authorized clients
- Never include keys in client-side code or public repositories

## Tech Stack

- .NET 8
- Entity Framework Core
- PostgreSQL (Npgsql)
- Microsoft Semantic Kernel
- OpenAI API
- API Key Authentication (Custom Middleware)
- Docker
- Swagger/OpenAPI