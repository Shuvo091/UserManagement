# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5090
EXPOSE 5091


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Nuget.Config", "."]
COPY ["CohesionX.UserManagement/CohesionX.UserManagement.csproj", "CohesionX.UserManagement/"]
COPY ["CohesionX.UserManagement.Application/CohesionX.UserManagement.Application.csproj", "CohesionX.UserManagement.Application/"]
COPY ["CohesionX.UserManagement.Abstractions/CohesionX.UserManagement.Abstractions.csproj", "CohesionX.UserManagement.Abstractions/"]
COPY ["CohesionX.UserManagement.Database.Abstractions/CohesionX.UserManagement.Database.Abstractions.csproj", "CohesionX.UserManagement.Database.Abstractions/"]
COPY ["CohesionX.UserManagement.Database/CohesionX.UserManagement.Database.csproj", "CohesionX.UserManagement.Database/"]
RUN dotnet restore "./CohesionX.UserManagement/CohesionX.UserManagement.csproj"
COPY . .
WORKDIR "/src/CohesionX.UserManagement"
RUN dotnet build "./CohesionX.UserManagement.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CohesionX.UserManagement.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CohesionX.UserManagement.dll"]