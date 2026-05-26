# --- Stage 1: Build Stage (Compiler) ---
# Using the .NET 10 SDK on Alpine Linux to minimize the builder's footprint and attack surface.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Optimization: Copy project files and restore dependencies separately.
# This leverages Docker's layer caching, preventing redundant downloads when only source code changes.
COPY ["Chronos.API/Chronos.API.csproj", "Chronos.API/"]
RUN dotnet restore "Chronos.API/Chronos.API.csproj"

# Copy the remaining source code and perform the compilation.
COPY . .
WORKDIR "/src/Chronos.API"
RUN dotnet build "Chronos.API.csproj" -c Release -o /app/build

# Publish the optimized binaries for the production runtime.
FROM build AS publish
RUN dotnet publish "Chronos.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 2: Final Runtime Stage (Runner) ---
# Using the ASP.NET Core 10 Runtime on Alpine for an ultra-low vulnerability surface area.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Dependency: Install krb5-libs to resolve the libgssapi dependency error required by Npgsql.
#icu-libs is also recommended for internationalization support in .NET.
RUN apk add --no-cache krb5-libs icu-libs

# Multi-stage isolation: Only copy the required binaries from the publish stage.
# This ensures build-time tools and source code are excluded from the production image.
COPY --from=publish /app/publish .

# Security: Explicitly declare the internal port.
EXPOSE 8080

# Privilege De-escalation: Switch to the built-in non-root 'app' user.
# This mitigates potential container breakout exploits by denying root access to the host kernel.
USER app

# Deterministic entry point for the application.
ENTRYPOINT ["dotnet", "Chronos.API.dll"]
