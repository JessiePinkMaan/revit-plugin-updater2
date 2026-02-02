# Тестирование системы

Руководство по тестированию всех компонентов системы обновления плагинов Revit.

## Локальное тестирование

### Подготовка окружения

#### 1. Установка зависимостей

```bash
# .NET 8 SDK
# PostgreSQL (или Docker)
# Node.js (для фронтенда, опционально)
```

#### 2. Настройка базы данных

**Вариант A: Локальный PostgreSQL**
```bash
# Создание базы данных
createdb revit_updater
```

**Вариант B: Docker**
```bash
cd Deployment
docker-compose up postgres -d
```

#### 3. Настройка конфигурации

Отредактируйте `Server/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=revit_updater;Username=postgres;Password=password"
  },
  "FileStorage": {
    "PluginsPath": "C:\\Temp\\Plugins"
  }
}
```

### Запуск сервера

```bash
cd Server
dotnet restore
dotnet ef database update
dotnet run
```

Сервер будет доступен по адресу: `https://localhost:5001`

### Тестирование API

#### 1. Проверка здоровья сервера

```bash
curl https://localhost:5001/api/health
```

Ожидаемый ответ:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "database": "connected"
}
```

#### 2. Авторизация

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

Сохраните полученный токен для дальнейших запросов.

#### 3. Получение списка плагинов

```bash
curl https://localhost:5001/api/admin/plugins \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### 4. Создание плагина

```bash
curl -X POST https://localhost:5001/api/admin/plugins \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "name=Test Plugin" \
  -F "description=Тестовый плагин" \
  -F "uniqueId=test-plugin-001"
```

#### 5. Загрузка версии

```bash
curl -X POST https://localhost:5001/api/admin/plugins/1/versions \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "version=1.0.0" \
  -F "releaseNotes=Первая версия" \
  -F "file=@path/to/your/plugin.dll"
```

### Тестирование веб-админки

1. Откройте `https://localhost:5001` в браузере
2. Войдите с учетными данными `admin` / `admin123`
3. Проверьте все разделы:
   - Дашборд
   - Список плагинов
   - Загрузка версий

## Тестирование клиента Revit

### Подготовка тестового плагина

#### 1. Создание тестового проекта

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
  
  <!-- Ссылки на Revit API -->
</Project>
```

#### 2. Интеграция UpdateManager

```csharp
public class TestPlugin : IExternalApplication
{
    private UpdateManager _updateManager;

    public Result OnStartup(UIControlledApplication application)
    {
        var pluginDir = @"C:\TestPlugin";
        _updateManager = new UpdateManager(pluginDir);
        
        var config = _updateManager.GetConfig();
        config.ServerUrl = "https://localhost:5001";
        config.PluginUniqueId = "test-plugin-001";
        config.CurrentVersion = "1.0.0";
        config.MainPluginFile = "TestPlugin.dll";
        _updateManager.SaveConfig(config);

        return Result.Succeeded;
    }
}
```

#### 3. Тестирование обновлений

```csharp
// Ручная проверка обновлений
var latestVersion = await _updateManager.CheckForUpdatesAsync();
if (latestVersion != null)
{
    Console.WriteLine($"Найдено обновление: {latestVersion.Version}");
    
    // Скачивание
    var success = await _updateManager.DownloadAndInstallUpdateAsync(latestVersion, false);
    Console.WriteLine($"Скачивание: {(success ? "успешно" : "ошибка")}");
}
```

### Тестирование updater.exe

#### 1. Создание тестовых инструкций

```json
{
  "SourceFile": "C:\\Temp\\TestPlugin_v1.1.0.dll",
  "TargetDirectory": "C:\\TestPlugin",
  "MainPluginFile": "TestPlugin.dll",
  "NewVersion": "1.1.0",
  "BackupDirectory": "C:\\TestPlugin\\Backup",
  "LogFile": "C:\\TestPlugin\\Logs\\updater.log"
}
```

#### 2. Запуск updater

```bash
updater.exe "C:\TestPlugin\update_instructions.json"
```

#### 3. Проверка результата

- Проверьте, что файл обновлен
- Проверьте создание резервной копии
- Проверьте логи обновления

## Нагрузочное тестирование

### Тестирование API

#### 1. Установка инструментов

```bash
# Apache Bench
sudo apt-get install apache2-utils

# или Artillery
npm install -g artillery
```

#### 2. Тест авторизации

```bash
ab -n 100 -c 10 -p login.json -T application/json \
  https://localhost:5001/api/auth/login
```

#### 3. Тест скачивания файлов

```bash
ab -n 50 -c 5 https://localhost:5001/api/download/1/1.0.0
```

### Тестирование базы данных

#### 1. Множественные плагины

```sql
-- Создание тестовых данных
INSERT INTO "Plugins" ("Name", "Description", "UniqueId", "CreatedAt", "UpdatedAt")
SELECT 
  'Plugin ' || generate_series,
  'Test plugin ' || generate_series,
  'plugin-' || generate_series,
  NOW(),
  NOW()
FROM generate_series(1, 1000);
```

#### 2. Множественные версии

```sql
-- Создание версий для каждого плагина
INSERT INTO "PluginVersions" ("PluginId", "Version", "ReleaseNotes", "FileName", "FilePath", "FileSize", "FileHash", "CreatedAt")
SELECT 
  p."Id",
  '1.' || (random() * 10)::int || '.' || (random() * 10)::int,
  'Test version',
  'plugin_' || p."Id" || '.dll',
  '/fake/path/plugin_' || p."Id" || '.dll',
  1024 * 1024,
  md5(random()::text),
  NOW()
FROM "Plugins" p;
```

## Автоматизированное тестирование

### Unit тесты для сервера

#### 1. Создание тестового проекта

```bash
cd Server
dotnet new xunit -n RevitPluginUpdater.Server.Tests
cd RevitPluginUpdater.Server.Tests
dotnet add reference ../RevitPluginUpdater.Server.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

#### 2. Пример теста контроллера

```csharp
[Fact]
public async Task GetPlugins_ReturnsPluginsList()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Получаем токен авторизации
    var token = await GetAuthTokenAsync(client);
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/admin/plugins");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var plugins = JsonConvert.DeserializeObject<List<PluginDto>>(content);
    
    Assert.NotNull(plugins);
}
```

#### 3. Тест сервисов

```csharp
[Fact]
public async Task UpdateService_CheckForUpdates_ReturnsLatestVersion()
{
    // Arrange
    var config = new UpdateConfig
    {
        ServerUrl = "https://test-server.com",
        PluginUniqueId = "test-plugin",
        CurrentVersion = "1.0.0"
    };
    
    var mockHttpClient = new Mock<HttpClient>();
    // Настройка мока...
    
    var updateService = new UpdateService(config);

    // Act
    var result = await updateService.CheckForUpdatesAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal("1.1.0", result.Version);
}
```

### Integration тесты

#### 1. Тест полного цикла

```csharp
[Fact]
public async Task FullUpdateCycle_CreatesPluginAndDownloadsUpdate()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    var token = await GetAuthTokenAsync(client);

    // Act & Assert
    
    // 1. Создаем плагин
    var createResponse = await CreatePluginAsync(client, token, "test-plugin");
    Assert.True(createResponse.IsSuccessStatusCode);

    // 2. Загружаем версию
    var uploadResponse = await UploadVersionAsync(client, token, 1, "1.0.0");
    Assert.True(uploadResponse.IsSuccessStatusCode);

    // 3. Проверяем доступность для скачивания
    var downloadResponse = await client.GetAsync("/api/download/by-unique-id/test-plugin/1.0.0");
    Assert.True(downloadResponse.IsSuccessStatusCode);

    // 4. Проверяем API последней версии
    var latestResponse = await client.GetAsync("/api/plugins/by-unique-id/test-plugin/latest");
    Assert.True(latestResponse.IsSuccessStatusCode);
}
```

### Тестирование производительности

#### 1. Benchmark тесты

```csharp
[MemoryDiagnoser]
public class PerformanceBenchmarks
{
    [Benchmark]
    public async Task FileUpload_1MB()
    {
        // Тест загрузки файла 1MB
    }

    [Benchmark]
    public async Task FileUpload_10MB()
    {
        // Тест загрузки файла 10MB
    }

    [Benchmark]
    public async Task DatabaseQuery_1000Plugins()
    {
        // Тест запроса к базе с 1000 плагинов
    }
}
```

## Тестирование на Render.com

### Staging окружение

#### 1. Создание тестового сервиса

1. Создайте отдельную ветку `staging`
2. Настройте отдельный сервис на Render для этой ветки
3. Используйте отдельную базу данных для тестов

#### 2. Автоматическое тестирование

```yaml
# .github/workflows/test.yml
name: Test on Render

on:
  push:
    branches: [ staging ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Wait for deployment
      run: sleep 60
      
    - name: Test API endpoints
      run: |
        curl -f ${{ secrets.STAGING_URL }}/api/health
        # Дополнительные тесты...
```

### Мониторинг в продакшене

#### 1. Health checks

```csharp
// Настройка в Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("file-storage", () => 
    {
        var path = "/var/data/plugins";
        return Directory.Exists(path) ? 
            HealthCheckResult.Healthy() : 
            HealthCheckResult.Unhealthy();
    });

app.MapHealthChecks("/health");
```

#### 2. Логирование метрик

```csharp
// Метрики в контроллерах
[HttpPost("plugins/{id}/versions")]
public async Task<ActionResult> CreateVersion(int id, [FromForm] CreateVersionRequest request, IFormFile file)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        // Логика загрузки...
        
        _logger.LogInformation("Version uploaded in {ElapsedMs}ms, size: {FileSize}bytes", 
            stopwatch.ElapsedMilliseconds, file.Length);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Upload failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        throw;
    }
}
```

## Чек-лист тестирования

### Перед релизом

- [ ] Все unit тесты проходят
- [ ] Integration тесты проходят
- [ ] API документация актуальна
- [ ] Веб-админка работает во всех браузерах
- [ ] Клиент Revit корректно обновляется
- [ ] Updater.exe работает без ошибок
- [ ] Логи пишутся корректно
- [ ] Обработка ошибок работает
- [ ] Безопасность проверена
- [ ] Производительность приемлема

### После развертывания

- [ ] Health check возвращает OK
- [ ] База данных доступна
- [ ] Файловое хранилище работает
- [ ] SSL сертификат валиден
- [ ] Мониторинг настроен
- [ ] Резервное копирование работает
- [ ] Логи собираются
- [ ] Уведомления об ошибках настроены

### Регулярные проверки

- [ ] Еженедельная проверка логов
- [ ] Ежемесячная проверка производительности
- [ ] Квартальная проверка безопасности
- [ ] Полугодовое обновление зависимостей

## Устранение проблем

### Частые ошибки

#### 1. Ошибка подключения к базе данных

**Симптомы**: 500 ошибка при запросах к API

**Диагностика**:
```bash
# Проверка подключения
curl https://your-app.onrender.com/api/health

# Проверка логов
# В Render Dashboard -> Logs
```

**Решение**:
- Проверьте переменную `DATABASE_URL`
- Убедитесь, что база данных запущена
- Проверьте сетевые настройки

#### 2. Файлы не загружаются

**Симптомы**: Ошибка при загрузке файлов плагинов

**Диагностика**:
```bash
# Проверка дискового пространства
df -h /var/data

# Проверка прав доступа
ls -la /var/data/plugins
```

**Решение**:
- Проверьте подключение диска в Render
- Убедитесь в наличии свободного места
- Проверьте права доступа

#### 3. Медленная работа

**Симптомы**: Долгие ответы API, таймауты

**Диагностика**:
```bash
# Тест производительности
ab -n 10 -c 1 https://your-app.onrender.com/api/health

# Проверка метрик в Render Dashboard
```

**Решение**:
- Оптимизируйте запросы к базе данных
- Добавьте кеширование
- Рассмотрите обновление плана

Готово! Теперь у вас есть полная система тестирования для проверки всех компонентов системы обновления плагинов Revit.