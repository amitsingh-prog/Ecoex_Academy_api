# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project file
COPY ["Ecoex_Academy_Api.csproj", "."]

# Restore NuGet packages
RUN dotnet restore "Ecoex_Academy_Api.csproj"

# Copy the remaining source code
COPY . .

# Build and publish
RUN dotnet publish "Ecoex_Academy_Api.csproj" \
    -c Release \
    -o /app/publish

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Application listens on port 8080
EXPOSE 8080

# Start the .NET application
ENTRYPOINT ["dotnet", "Ecoex_Academy_Api.dll"]