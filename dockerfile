# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# restore
COPY src/WebApp/WebApp.csproj /src/WebApp/
RUN dotnet restore /src/WebApp/WebApp.csproj

COPY src/WebApp/ /src/WebApp/
RUN dotnet publish /src/WebApp/WebApp.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# run
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WebApp.dll"]