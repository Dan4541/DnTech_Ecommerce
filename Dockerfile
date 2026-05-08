# ── Stage 1: build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos el .csproj directo en /src (sin subcarpeta)
COPY ["DnTech_Ecommerce.csproj", "."]
RUN dotnet restore "DnTech_Ecommerce.csproj"

# Copiamos el resto del código
COPY . .

# Publicamos — el .csproj está en /src directo
RUN dotnet publish "DnTech_Ecommerce.csproj" -c Release -o /app/publish \
    --no-restore

# ── Stage 2: runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DnTech_Ecommerce.dll"]