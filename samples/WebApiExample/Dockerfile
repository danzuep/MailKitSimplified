# syntax = docker/dockerfile:1.2
ARG NET_IMAGE=6.0-bullseye-slim

# Copy everything not in .dockerignore and restore
FROM mcr.microsoft.com/dotnet/sdk:${NET_IMAGE} AS source
WORKDIR "/src/"
COPY . .
WORKDIR "/src/WebApiExample"
RUN dotnet restore "WebApiExample.csproj"

# Build using the restored project files
FROM source AS build
RUN dotnet build "WebApiExample.csproj" --configuration Release --no-restore

# Publish using the existing build files
FROM build AS publish
RUN dotnet publish "WebApiExample.csproj" -c Release --output "/app" --no-build --no-self-contained

# Copy the publish/app folder to the Docker image/app working directory and set the startup entry point
FROM mcr.microsoft.com/dotnet/aspnet:${NET_IMAGE} AS image
WORKDIR "/app"
EXPOSE 80
EXPOSE 443
COPY --from=publish "/app" .
ENTRYPOINT ["dotnet", "WebApiExample.dll"]
