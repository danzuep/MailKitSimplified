# syntax = docker/dockerfile:1.2
ARG NET_IMAGE=6.0-bullseye-slim
ARG PROJECT_NAME=WebApiExample

# Copy everything not in .dockerignore and restore
FROM mcr.microsoft.com/dotnet/sdk:${NET_IMAGE} AS source
WORKDIR "/src/"
COPY . .
WORKDIR "/src/${PROJECT_NAME}"
RUN dotnet restore "${PROJECT_NAME}.csproj"

# Build using the restored project files
FROM source AS build
RUN dotnet build "${PROJECT_NAME}.csproj" --configuration Release --no-restore

# Publish using the existing build files
FROM build AS publish
RUN dotnet publish "${PROJECT_NAME}.csproj" -c Release --output "/app" --no-build --no-self-contained

# Copy the publish/app folder to the Docker image/app working directory and set the startup entry point
FROM mcr.microsoft.com/dotnet/aspnet:${NET_IMAGE} AS image
WORKDIR "/app"
EXPOSE 80
EXPOSE 443
COPY --from=publish "/app" .
ENTRYPOINT ["dotnet", "${PROJECT_NAME}.dll"]
