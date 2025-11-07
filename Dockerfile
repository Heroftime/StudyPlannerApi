# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["StudyPlannerApi.csproj", "./"]
RUN dotnet restore "StudyPlannerApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "StudyPlannerApi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "StudyPlannerApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET runtime image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy the published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "StudyPlannerApi.dll"]
