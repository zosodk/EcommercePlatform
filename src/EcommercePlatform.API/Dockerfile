# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy solution file and project files first to leverage Docker layer caching
# Adjust paths based on your actual solution structure relative to the solution root
COPY ../../EcommercePlatform.sln ./
COPY src/EcommercePlatform.Domain/EcommercePlatform.Domain.csproj ./src/EcommercePlatform.Domain/
COPY src/EcommercePlatform.Application/EcommercePlatform.Application.csproj ./src/EcommercePlatform.Application/
COPY src/EcommercePlatform.Infrastructure/EcommercePlatform.Infrastructure.csproj ./src/EcommercePlatform.Infrastructure/
COPY src/EcommercePlatform.API/EcommercePlatform.API.csproj ./src/EcommercePlatform.API/
# If you have test projects that are part of the solution restore, copy their .csproj files too

# Restore dependencies for all projects in the solution
RUN dotnet restore EcommercePlatform.sln

# Copy the rest of the source code
COPY . ./

# Build and publish the API project
WORKDIR /app/src/EcommercePlatform.API
RUN dotnet publish EcommercePlatform.API.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 8 ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/publish .

# Set up a non-root user for security
RUN useradd -m appuser
USER appuser

# Expose the port the app will run on (should match ASPNETCORE_URLS or Kestrel config)
EXPOSE 8080

# Define the entry point for the application
ENTRYPOINT ["dotnet", "EcommercePlatform.API.dll"]