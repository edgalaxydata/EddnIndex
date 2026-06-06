# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION
ARG SOURCE_REVISION
WORKDIR /src
COPY ["EddnIndexUpdate/EddnIndexUpdate.csproj", "EddnIndexUpdate/"]
COPY ["EddnIndexLookup/EddnIndexLookup.csproj", "EddnIndexLookup/"]
RUN dotnet restore "./EddnIndexUpdate/EddnIndexUpdate.csproj" --artifacts-path=/app/build
RUN dotnet restore "./EddnIndexLookup/EddnIndexLookup.csproj" --artifacts-path=/app/build
COPY . .
WORKDIR "/src/EddnIndexUpdate"
RUN dotnet build --no-restore "./EddnIndexUpdate.csproj" -c $BUILD_CONFIGURATION --artifacts-path=/app/build -p:Version=${VERSION:-$(date "+%Y.%m%d.%H%M")} -p:SourceRevisionId=${SOURCE_REVISION}
WORKDIR "/src/EddnIndexLookup"
RUN dotnet build --no-restore "./EddnIndexLookup.csproj" -c $BUILD_CONFIGURATION --artifacts-path=/app/build -p:Version=${VERSION:-$(date "+%Y.%m%d.%H%M")} -p:SourceRevisionId=${SOURCE_REVISION}

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish --no-restore --no-build "./EddnIndexLookup.csproj" -c $BUILD_CONFIGURATION --artifacts-path=/app/build -p:PublishDir=/app/publish -p:Version=${VERSION:-$(date "+%Y.%m%d.%H%M")} -p:SourceRevisionId=${SOURCE_REVISION} -p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EddnIndexLookup.dll"]