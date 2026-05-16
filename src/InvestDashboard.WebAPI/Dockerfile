# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and restore projects
COPY src/InvestDashboard.slnx src/
COPY src/InvestDashboard.Domain/InvestDashboard.Domain.csproj src/InvestDashboard.Domain/
COPY src/InvestDashboard.Application/InvestDashboard.Application.csproj src/InvestDashboard.Application/
COPY src/InvestDashboard.Infrastructure/InvestDashboard.Infrastructure.csproj src/InvestDashboard.Infrastructure/
COPY src/InvestDashboard.WebAPI/InvestDashboard.WebAPI.csproj src/InvestDashboard.WebAPI/

RUN dotnet restore src/InvestDashboard.slnx

# Copy everything else and build
COPY src/ src/
RUN dotnet publish src/InvestDashboard.WebAPI/InvestDashboard.WebAPI.csproj -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InvestDashboard.WebAPI.dll"]
