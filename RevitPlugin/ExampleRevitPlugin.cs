using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitPluginUpdater.Client;
using RevitPluginUpdater.Client.Models;

namespace RevitPluginUpdater.Client.Example
{
    /// <summary>
    /// Пример Revit плагина с системой автоматического обновления
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExampleRevitPlugin : IExternalApplication
    {
        private UpdateManager _updateManager;
        private static readonly string PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Инициализируем систему обновлений
                InitializeUpdateSystem();

                // Создаем UI элементы плагина
                CreateRibbonPanel(application);

                // Проверяем обновления при запуске (асинхронно)
                Task.Run(async () =>
                {
                    await _updateManager.CheckForUpdatesOnStartupAsync();
                });

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка при запуске плагина: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                // Освобождаем ресурсы
                _updateManager?.Dispose();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка при завершении работы плагина: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Инициализирует систему обновлений
        /// </summary>
        private void InitializeUpdateSystem()
        {
            _updateManager = new UpdateManager(PluginDirectory);

            // Подписываемся на события
            _updateManager.UpdateAvailable += OnUpdateAvailable;
            _updateManager.DownloadProgress += OnDownloadProgress;
            _updateManager.UpdateError += OnUpdateError;

            // Проверяем и настраиваем конфигурацию
            SetupUpdateConfiguration();
        }

        /// <summary>
        /// Настраивает конфигурацию обновлений
        /// </summary>
        private void SetupUpdateConfiguration()
        {
            var config = _updateManager.GetConfig();

            // Устанавливаем параметры для вашего плагина
            config.PluginUniqueId = "example-revit-plugin"; // ЗАМЕНИТЕ НА ВАШ УНИКАЛЬНЫЙ ID
            config.MainPluginFile = "ExampleRevitPlugin.dll"; // ЗАМЕНИТЕ НА ИМЯ ВАШЕГО ФАЙЛА
            config.ServerUrl = "https://your-app.onrender.com"; // ЗАМЕНИТЕ НА ВАШ URL
            config.CurrentVersion = "1.0.0"; // ЗАМЕНИТЕ НА ТЕКУЩУЮ ВЕРСИЮ

            // Настройки поведения
            config.CheckUpdatesOnStartup = true;
            config.ShowNotifications = true;
            config.AutoDownload = false; // Рекомендуется false для безопасности
            config.AutoInstall = false;  // Рекомендуется false для безопасности
            config.CheckIntervalHours = 24;

            // Сохраняем конфигурацию
            _updateManager.SaveConfig(config);

            // Проверяем корректность настройки
            if (!_updateManager.IsConfigurationValid())
            {
                var issues = _updateManager.GetConfigurationIssues();
                TaskDialog.Show("Конфигурация обновлений", 
                    $"Обнаружены проблемы в конфигурации обновлений:\n\n{issues}");
            }
        }

        /// <summary>
        /// Создает панель в ленте Revit
        /// </summary>
        private void CreateRibbonPanel(UIControlledApplication application)
        {
            // Создаем панель
            var panel = application.CreateRibbonPanel("Example Plugin");

            // Кнопка основной функции
            var mainButton = new PushButtonData("MainCommand", "Основная\nфункция", 
                Assembly.GetExecutingAssembly().Location, typeof(MainCommand).FullName);
            
            mainButton.ToolTip = "Основная функция плагина";
            mainButton.LargeImage = GetEmbeddedImage("icon32.png");
            
            panel.AddItem(mainButton);

            // Разделитель
            panel.AddSeparator();

            // Кнопка проверки обновлений
            var updateButton = new PushButtonData("CheckUpdates", "Проверить\nобновления", 
                Assembly.GetExecutingAssembly().Location, typeof(CheckUpdatesCommand).FullName);
            
            updateButton.ToolTip = "Проверить наличие обновлений плагина";
            updateButton.LargeImage = GetEmbeddedImage("update32.png");
            
            panel.AddItem(updateButton);

            // Кнопка настроек
            var settingsButton = new PushButtonData("UpdateSettings", "Настройки\nобновлений", 
                Assembly.GetExecutingAssembly().Location, typeof(UpdateSettingsCommand).FullName);
            
            settingsButton.ToolTip = "Настройки системы обновлений";
            settingsButton.LargeImage = GetEmbeddedImage("settings32.png");
            
            panel.AddItem(settingsButton);
        }

        /// <summary>
        /// Получает встроенное изображение
        /// </summary>
        private System.Windows.Media.ImageSource GetEmbeddedImage(string imageName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream($"RevitPluginUpdater.Client.Example.Resources.{imageName}");
                
                if (stream != null)
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    return bitmap;
                }
            }
            catch
            {
                // Игнорируем ошибки загрузки изображений
            }

            return null;
        }

        // Обработчики событий системы обновлений

        private void OnUpdateAvailable(object sender, UpdateAvailableEventArgs e)
        {
            // Логируем информацию о доступном обновлении
            var message = $"Доступно обновление: {e.VersionInfo.Version}";
            System.Diagnostics.Debug.WriteLine(message);
        }

        private void OnDownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            // Логируем прогресс скачивания
            var message = $"Прогресс скачивания: {e.ProgressPercentage}%";
            System.Diagnostics.Debug.WriteLine(message);
        }

        private void OnUpdateError(object sender, UpdateErrorEventArgs e)
        {
            // Логируем ошибки обновления
            var message = $"Ошибка обновления: {e.Exception.Message}";
            System.Diagnostics.Debug.WriteLine(message);
        }

        /// <summary>
        /// Предоставляет доступ к UpdateManager для команд
        /// </summary>
        public static UpdateManager GetUpdateManager()
        {
            // В реальном проекте лучше использовать DI контейнер
            return new UpdateManager(PluginDirectory);
        }
    }

    /// <summary>
    /// Основная команда плагина
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class MainCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                TaskDialog.Show("Example Plugin", "Основная функция плагина выполнена!");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Команда проверки обновлений
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CheckUpdatesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (var updateManager = ExampleRevitPlugin.GetUpdateManager())
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var latestVersion = await updateManager.CheckForUpdatesAsync();
                            
                            if (latestVersion != null)
                            {
                                var updateMessage = $"Найдено обновление до версии {latestVersion.Version}!\n\n" +
                                                   $"Описание изменений:\n{latestVersion.ReleaseNotes}\n\n" +
                                                   "Хотите скачать обновление?";

                                var result = TaskDialog.Show("Обновление найдено", updateMessage, 
                                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                                if (result == TaskDialogResult.Yes)
                                {
                                    await updateManager.DownloadAndInstallUpdateAsync(latestVersion, false);
                                }
                            }
                            else
                            {
                                TaskDialog.Show("Проверка обновлений", "У вас установлена последняя версия плагина.");
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Ошибка", $"Ошибка при проверке обновлений: {ex.Message}");
                        }
                    });
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Команда настроек обновлений
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class UpdateSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (var updateManager = ExampleRevitPlugin.GetUpdateManager())
                {
                    var config = updateManager.GetConfig();
                    
                    var settingsMessage = $"Текущие настройки обновлений:\n\n" +
                                         $"Сервер: {config.ServerUrl}\n" +
                                         $"ID плагина: {config.PluginUniqueId}\n" +
                                         $"Текущая версия: {config.CurrentVersion}\n" +
                                         $"Проверка при запуске: {(config.CheckUpdatesOnStartup ? "Да" : "Нет")}\n" +
                                         $"Показывать уведомления: {(config.ShowNotifications ? "Да" : "Нет")}\n" +
                                         $"Интервал проверки: {config.CheckIntervalHours} часов\n" +
                                         $"Последняя проверка: {(config.LastCheckTime == DateTime.MinValue ? "Никогда" : config.LastCheckTime.ToString())}";

                    if (!updateManager.IsConfigurationValid())
                    {
                        settingsMessage += "\n\nПроблемы конфигурации:\n" + updateManager.GetConfigurationIssues();
                    }

                    TaskDialog.Show("Настройки обновлений", settingsMessage);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}