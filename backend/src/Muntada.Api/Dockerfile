# Stage 1: Build with SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (all dependencies in the graph)
COPY backend/Muntada.slnx backend/
COPY backend/src/Muntada.Api/Muntada.Api.csproj backend/src/Muntada.Api/
COPY backend/src/Muntada.SharedKernel/Muntada.SharedKernel.csproj backend/src/Muntada.SharedKernel/
COPY backend/src/Modules/Identity/Identity.csproj backend/src/Modules/Identity/
COPY aspire/Muntada.ServiceDefaults/Muntada.ServiceDefaults.csproj aspire/Muntada.ServiceDefaults/

RUN dotnet restore backend/src/Muntada.Api/Muntada.Api.csproj

# Copy all source and build
COPY backend/ backend/
COPY aspire/Muntada.ServiceDefaults/ aspire/Muntada.ServiceDefaults/
RUN dotnet publish backend/src/Muntada.Api/Muntada.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime (target < 800MB)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Muntada.Api.dll"]
