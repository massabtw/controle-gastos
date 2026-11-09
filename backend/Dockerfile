FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution and csproj files for layer caching
COPY FinanceTracker.sln ./
COPY src/FinanceTracker.Domain/*.csproj ./src/FinanceTracker.Domain/
COPY src/FinanceTracker.Application/*.csproj ./src/FinanceTracker.Application/
COPY src/FinanceTracker.Infrastructure/*.csproj ./src/FinanceTracker.Infrastructure/
COPY src/FinanceTracker.Api/*.csproj ./src/FinanceTracker.Api/
COPY tests/FinanceTracker.Tests/*.csproj ./tests/FinanceTracker.Tests/

# Restore dependencies
RUN dotnet restore src/FinanceTracker.Api/FinanceTracker.Api.csproj

# Copy remaining source files
COPY . .

# Publish production release build
WORKDIR /source/src/FinanceTracker.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=10000
ENV PORT=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "FinanceTracker.Api.dll"]
