# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY CohesionX.UserManagement/CohesionX.UserManagement.csproj ./CohesionX.UserManagement/
COPY Shared/SharedLibrary/SharedLibrary/SharedLibrary.csproj ./Shared/SharedLibrary/

# Restore
RUN dotnet restore CohesionX.UserManagement/CohesionX.UserManagement.csproj

# Copy full source
COPY . .

# Publish
WORKDIR /src/CohesionX.UserManagement
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 90
ENV ASPNETCORE_URLS=http://+:90
ENTRYPOINT ["dotnet", "CohesionX.UserManagement.dll"]
