# Dockerfile для развертывания на Render.com
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Создаем директории для данных
RUN mkdir -p /tmp/plugins

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["Server/RevitPluginUpdater.Server.csproj", "Server/"]
RUN dotnet restore "Server/RevitPluginUpdater.Server.csproj"

# Копируем исходный код
COPY update/ .
WORKDIR "/src/Server"

# Собираем приложение
RUN dotnet build "RevitPluginUpdater.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RevitPluginUpdater.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Копируем собранное приложение
COPY --from=publish /app/publish .

# Копируем статические файлы админки
COPY AdminPanel/ ./wwwroot/

# Настраиваем переменные окружения
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV FileStorage__PluginsPath=/tmp/plugins

ENTRYPOINT ["dotnet", "RevitPluginUpdater.Server.dll"]