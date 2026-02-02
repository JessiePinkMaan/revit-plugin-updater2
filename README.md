# Система автоматического обновления плагинов Revit

Полнофункциональная система для автоматического обновления плагинов Autodesk Revit с веб-админкой и развертыванием на Render.com.

## 🚀 Возможности

- **Автоматическая проверка обновлений** при запуске Revit
- **Веб-админка** для управления плагинами и версиями
- **Безопасное скачивание** с проверкой хешей файлов
- **Автоматическая установка** через updater.exe
- **Резервное копирование** перед обновлением
- **Подробное логирование** всех операций
- **JWT авторизация** для админки
- **Развертывание на Render.com** одной командой

## 🏗️ Архитектура

```
[Веб-админка] → [ASP.NET Core API] → [PostgreSQL]
                        ↓
[Revit плагины] ← [Система обновлений]
```

## 📁 Структура проекта

```
RevitPluginUpdater/
├── Server/                     # ASP.NET Core 8 API сервер
│   ├── Controllers/           # REST API контроллеры
│   ├── Services/              # Бизнес-логика
│   ├── Models/                # Модели данных
│   └── Data/                  # Entity Framework контекст
├── AdminPanel/                # Веб-админка (HTML/JS/Bootstrap)
│   ├── index.html            # Главная страница
│   └── js/app.js             # JavaScript логика
├── RevitPlugin/              # Клиент для Revit (.NET Framework 4.8)
│   ├── Services/             # Сервисы обновления
│   ├── Models/               # Модели данных
│   ├── UpdateManager.cs      # Основной класс управления
│   └── Updater/              # Утилита updater.exe
├── Deployment/               # Конфигурация для развертывания
│   ├── Dockerfile           # Docker образ
│   ├── render.yaml          # Конфигурация Render.com
│   └── docker-compose.yml   # Локальная разработка
└── Documentation/            # Документация
    ├── RenderDeployment.md  # Развертывание на Render.com
    ├── UserGuide.md         # Руководство пользователя
    └── Testing.md           # Тестирование системы
```

## 🛠️ Технологии

### Серверная часть
- **ASP.NET Core 8** - веб API
- **Entity Framework Core** - ORM
- **PostgreSQL** - база данных
- **JWT** - авторизация
- **Serilog** - логирование

### Клиентская часть
- **Vanilla JavaScript** - фронтенд админки
- **Bootstrap 5** - UI фреймворк
- **C# .NET Framework 4.8** - клиент Revit

### Развертывание
- **Docker** - контейнеризация
- **Render.com** - хостинг
- **GitHub Actions** - CI/CD (опционально)

## 🚀 Быстрый старт

### 1. Клонирование репозитория

```bash
git clone https://github.com/JessiePinkMaan/revit-plugin-updater.git
cd revit-plugin-updater
```

### 2. Локальная разработка

```bash
# Запуск через Docker Compose
cd Deployment
docker-compose up -d

# Или запуск сервера напрямую
cd Server
dotnet restore
dotnet ef database update
dotnet run
```

Сервер будет доступен по адресу: `https://localhost:5001`

### 3. Развертывание на Render.com

Следуйте подробной инструкции в [Documentation/RenderDeployment.md](Documentation/RenderDeployment.md)

## 📖 API Документация

### Авторизация
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

### Управление плагинами (требует авторизации)
```http
GET    /api/admin/plugins              # Список плагинов
POST   /api/admin/plugins              # Создать плагин
POST   /api/admin/plugins/{id}/versions # Загрузить версию
DELETE /api/admin/plugins/{id}/versions/{version} # Удалить версию
```

### Публичное API для плагинов
```http
GET /api/plugins/{id}                    # Информация о плагине
GET /api/plugins/{id}/latest             # Последняя версия
GET /api/download/{id}/{version}         # Скачать файл
GET /api/health                          # Состояние сервера
```

## 🔧 Интеграция в Revit плагин

### 1. Добавление зависимостей

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="System.Net.Http" Version="4.3.4" />
```

### 2. Инициализация системы обновлений

```csharp
public class YourRevitPlugin : IExternalApplication
{
    private UpdateManager _updateManager;

    public Result OnStartup(UIControlledApplication application)
    {
        // Инициализация
        var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _updateManager = new UpdateManager(pluginDir);

        // Настройка
        var config = _updateManager.GetConfig();
        config.ServerUrl = "https://your-app.onrender.com";
        config.PluginUniqueId = "your-plugin-unique-id";
        config.CurrentVersion = "1.0.0";
        config.MainPluginFile = "YourPlugin.dll";
        _updateManager.SaveConfig(config);

        // Проверка обновлений при запуске
        Task.Run(async () => {
            await _updateManager.CheckForUpdatesOnStartupAsync();
        });

        return Result.Succeeded;
    }
}
```

### 3. Ручная проверка обновлений

```csharp
var latestVersion = await _updateManager.CheckForUpdatesAsync();
if (latestVersion != null)
{
    await _updateManager.DownloadAndInstallUpdateAsync(latestVersion, false);
}
```

## 🔐 Безопасность

- **JWT токены** для авторизации админки
- **Проверка хешей файлов** при скачивании
- **HTTPS** для всех соединений
- **Резервное копирование** перед обновлением
- **Валидация входных данных**

## 📊 Мониторинг

- **Health checks** для проверки состояния
- **Подробные логи** всех операций
- **Метрики производительности**
- **Уведомления об ошибках**

## 🧪 Тестирование

```bash
# Unit тесты
cd Server.Tests
dotnet test

# Integration тесты
cd Tests
dotnet test

# Нагрузочное тестирование
ab -n 100 -c 10 https://your-app.onrender.com/api/health
```

Подробнее в [Documentation/Testing.md](Documentation/Testing.md)

## 📚 Документация

- [Развертывание на Render.com](Documentation/RenderDeployment.md)
- [Руководство пользователя](Documentation/UserGuide.md)
- [Тестирование системы](Documentation/Testing.md)

## 🤝 Вклад в проект

1. Форкните репозиторий
2. Создайте ветку для новой функции (`git checkout -b feature/amazing-feature`)
3. Зафиксируйте изменения (`git commit -m 'Add amazing feature'`)
4. Отправьте в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## 📄 Лицензия

Этот проект лицензирован под MIT License - см. файл [LICENSE](LICENSE) для деталей.

## 🆘 Поддержка

- **Issues**: [GitHub Issues](https://github.com/JessiePinkMaan/revit-plugin-updater/issues)
- **Документация**: [Wiki](https://github.com/JessiePinkMaan/revit-plugin-updater/wiki)
- **Email**: support@yourcompany.com

## 🎯 Roadmap

- [ ] Поддержка множественных версий Revit
- [ ] Интеграция с Autodesk App Store
- [ ] Система уведомлений по email
- [ ] Аналитика использования плагинов
- [ ] Автоматические тесты плагинов
- [ ] Поддержка плагинов для других CAD систем

---

**Создано с ❤️ для сообщества разработчиков Revit**