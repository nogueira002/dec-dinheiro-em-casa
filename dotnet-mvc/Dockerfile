# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar o .csproj para restaurar dependências em cache separado
COPY dotnet-mvc/dotnet-mvc.csproj dotnet-mvc/
RUN dotnet restore dotnet-mvc/dotnet-mvc.csproj

# Copiar todo o código e publicar
COPY . .
RUN dotnet publish dotnet-mvc/dotnet-mvc.csproj \
    -c Release \
    -o /app/publish

# Runtime stage — imagem mínima sem SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl necessário para o health check (aspnet image não inclui wget/curl)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Criar utilizador não-root
RUN groupadd --system appgroup && useradd --system --gid appgroup --home /app appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "dotnet-mvc.dll"]
