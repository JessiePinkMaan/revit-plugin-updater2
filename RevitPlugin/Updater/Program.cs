using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Newtonsoft.Json;

namespace RevitPluginUpdater.Updater
{
    /// <summary>
    /// Утилита для обновления плагинов Revit
    /// Запускается отдельным процессом для замены файлов плагина
    /// </summary>
    class Program
    {
        private static string _logFile;

        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Revit Plugin Updater v1.0");
                Console.WriteLine("==========================");

                if (args.Length == 0)
                {
                    Console.WriteLine("Использование: updater.exe <путь_к_файлу_инструкций>");
                    Console.WriteLine("Пример: updater.exe \"C:\\Plugins\\MyPlugin\\update_instructions.json\"");
                    return 1;
                }

                var instructionsFile = args[0];
                if (!File.Exists(instructionsFile))
                {
                    Console.WriteLine($"Файл инструкций не найден: {instructionsFile}");
                    return 1;
                }

                // Загружаем инструкции
                var instructions = LoadInstructions(instructionsFile);
                if (instructions == null)
                {
                    Console.WriteLine("Не удалось загрузить инструкции");
                    return 1;
                }

                _logFile = instructions.LogFile;
                LogMessage("Запуск updater.exe");
                LogMessage($"Файл инструкций: {instructionsFile}");

                // Выполняем обновление
                var result = PerformUpdate(instructions);

                // Удаляем файл инструкций
                try
                {
                    File.Delete(instructionsFile);
                    LogMessage("Файл инструкций удален");
                }
                catch (Exception ex)
                {
                    LogMessage($"Не удалось удалить файл инструкций: {ex.Message}");
                }

                LogMessage($"Updater завершен с кодом: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                LogMessage($"Критическая ошибка: {ex}");
                return 1;
            }
        }

        /// <summary>
        /// Загружает инструкции из JSON файла
        /// </summary>
        static UpdateInstructions LoadInstructions(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<UpdateInstructions>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке инструкций: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Выполняет процесс обновления
        /// </summary>
        static int PerformUpdate(UpdateInstructions instructions)
        {
            try
            {
                LogMessage("Начало процесса обновления");
                LogMessage($"Исходный файл: {instructions.SourceFile}");
                LogMessage($"Целевая директория: {instructions.TargetDirectory}");
                LogMessage($"Новая версия: {instructions.NewVersion}");

                // Проверяем исходный файл
                if (!File.Exists(instructions.SourceFile))
                {
                    LogMessage($"Исходный файл не найден: {instructions.SourceFile}");
                    return 1;
                }

                // Проверяем целевую директорию
                if (!Directory.Exists(instructions.TargetDirectory))
                {
                    LogMessage($"Целевая директория не найдена: {instructions.TargetDirectory}");
                    return 1;
                }

                // Ждем завершения процессов Revit
                WaitForRevitToClose();

                // Создаем резервную копию
                if (!CreateBackup(instructions))
                {
                    LogMessage("Не удалось создать резервную копию");
                    return 1;
                }

                // Устанавливаем обновление
                if (!InstallUpdate(instructions))
                {
                    LogMessage("Не удалось установить обновление");
                    
                    // Пытаемся восстановить из резервной копии
                    RestoreFromBackup(instructions);
                    return 1;
                }

                // Очищаем временные файлы
                CleanupTempFiles(instructions);

                LogMessage("Обновление установлено успешно");
                return 0;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при выполнении обновления: {ex}");
                return 1;
            }
        }

        /// <summary>
        /// Ждет завершения всех процессов Revit
        /// </summary>
        static void WaitForRevitToClose()
        {
            LogMessage("Ожидание завершения процессов Revit...");
            
            var maxWaitTime = TimeSpan.FromMinutes(2);
            var startTime = DateTime.Now;
            
            while (DateTime.Now - startTime < maxWaitTime)
            {
                var revitProcesses = System.Diagnostics.Process.GetProcessesByName("Revit");
                if (revitProcesses.Length == 0)
                {
                    LogMessage("Процессы Revit завершены");
                    return;
                }

                LogMessage($"Найдено {revitProcesses.Length} процессов Revit, ожидание...");
                Thread.Sleep(2000);
            }

            LogMessage("Превышено время ожидания завершения Revit, продолжаем обновление");
        }

        /// <summary>
        /// Создает резервную копию текущих файлов
        /// </summary>
        static bool CreateBackup(UpdateInstructions instructions)
        {
            try
            {
                LogMessage("Создание резервной копии...");

                if (!Directory.Exists(instructions.BackupDirectory))
                {
                    Directory.CreateDirectory(instructions.BackupDirectory);
                }

                var backupPath = Path.Combine(instructions.BackupDirectory, 
                    $"backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                // Копируем все файлы из целевой директории
                CopyDirectory(instructions.TargetDirectory, backupPath);

                LogMessage($"Резервная копия создана: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при создании резервной копии: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Устанавливает обновление
        /// </summary>
        static bool InstallUpdate(UpdateInstructions instructions)
        {
            try
            {
                LogMessage("Установка обновления...");

                var sourceFile = instructions.SourceFile;
                var fileExtension = Path.GetExtension(sourceFile).ToLowerInvariant();

                if (fileExtension == ".zip" || fileExtension == ".rar")
                {
                    // Распаковываем архив
                    return ExtractArchive(sourceFile, instructions.TargetDirectory);
                }
                else
                {
                    // Копируем отдельный файл
                    var targetFile = Path.Combine(instructions.TargetDirectory, instructions.MainPluginFile);
                    
                    // Удаляем старый файл если существует
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                        LogMessage($"Удален старый файл: {targetFile}");
                    }

                    // Копируем новый файл
                    File.Copy(sourceFile, targetFile, true);
                    LogMessage($"Скопирован новый файл: {targetFile}");
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при установке обновления: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Распаковывает архив
        /// </summary>
        static bool ExtractArchive(string archivePath, string targetDirectory)
        {
            try
            {
                LogMessage($"Распаковка архива: {archivePath}");

                if (Path.GetExtension(archivePath).ToLowerInvariant() == ".zip")
                {
                    ZipFile.ExtractToDirectory(archivePath, targetDirectory, true);
                    LogMessage("Архив ZIP распакован успешно");
                    return true;
                }
                else
                {
                    LogMessage("Формат RAR не поддерживается встроенными средствами");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при распаковке архива: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Восстанавливает файлы из резервной копии
        /// </summary>
        static void RestoreFromBackup(UpdateInstructions instructions)
        {
            try
            {
                LogMessage("Восстановление из резервной копии...");

                if (!Directory.Exists(instructions.BackupDirectory))
                {
                    LogMessage("Директория резервных копий не найдена");
                    return;
                }

                // Находим последнюю резервную копию
                var backupDirs = Directory.GetDirectories(instructions.BackupDirectory, "backup_*");
                if (backupDirs.Length == 0)
                {
                    LogMessage("Резервные копии не найдены");
                    return;
                }

                Array.Sort(backupDirs);
                var latestBackup = backupDirs[backupDirs.Length - 1];

                LogMessage($"Восстановление из: {latestBackup}");

                // Копируем файлы обратно
                CopyDirectory(latestBackup, instructions.TargetDirectory);

                LogMessage("Восстановление завершено");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при восстановлении: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает временные файлы
        /// </summary>
        static void CleanupTempFiles(UpdateInstructions instructions)
        {
            try
            {
                LogMessage("Очистка временных файлов...");

                // Удаляем скачанный файл
                if (File.Exists(instructions.SourceFile))
                {
                    File.Delete(instructions.SourceFile);
                    LogMessage($"Удален временный файл: {instructions.SourceFile}");
                }

                // Удаляем старые резервные копии (оставляем только 5 последних)
                if (Directory.Exists(instructions.BackupDirectory))
                {
                    var backupDirs = Directory.GetDirectories(instructions.BackupDirectory, "backup_*");
                    if (backupDirs.Length > 5)
                    {
                        Array.Sort(backupDirs);
                        for (int i = 0; i < backupDirs.Length - 5; i++)
                        {
                            Directory.Delete(backupDirs[i], true);
                            LogMessage($"Удалена старая резервная копия: {backupDirs[i]}");
                        }
                    }
                }

                LogMessage("Очистка завершена");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при очистке: {ex.Message}");
            }
        }

        /// <summary>
        /// Копирует директорию рекурсивно
        /// </summary>
        static void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Копируем файлы
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetDir, fileName);
                File.Copy(file, targetFile, true);
            }

            // Копируем поддиректории
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(subDir, targetSubDir);
            }
        }

        /// <summary>
        /// Записывает сообщение в лог
        /// </summary>
        static void LogMessage(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            
            Console.WriteLine(logEntry);

            if (!string.IsNullOrEmpty(_logFile))
            {
                try
                {
                    var logDir = Path.GetDirectoryName(_logFile);
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    File.AppendAllText(_logFile, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Игнорируем ошибки логирования
                }
            }
        }
    }

    /// <summary>
    /// Инструкции для обновления
    /// </summary>
    public class UpdateInstructions
    {
        public string SourceFile { get; set; }
        public string TargetDirectory { get; set; }
        public string MainPluginFile { get; set; }
        public string NewVersion { get; set; }
        public string BackupDirectory { get; set; }
        public string LogFile { get; set; }
    }
}