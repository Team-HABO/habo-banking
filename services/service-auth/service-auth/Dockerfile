FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["service-auth.csproj", "./"]
RUN dotnet restore "./service-auth.csproj"
COPY ["Program.cs", "./"]
COPY ["appsettings.json", "./"]
COPY ["appsettings.Development.json", "./"]
COPY ["Controllers/", "./Controllers/"]
COPY ["Services/", "./Services/"]
COPY ["Properties/", "./Properties/"]
WORKDIR "/src"
RUN dotnet build "./service-auth.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src"
RUN dotnet publish "./service-auth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD ["curl", "--fail", "--silent", "--show-error", "--output", "/dev/null", "http://127.0.0.1:8080/health"]
ENTRYPOINT ["dotnet", "service-auth.dll"]