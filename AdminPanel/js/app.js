// Конфигурация API
const API_BASE_URL = window.location.origin + '/api';

// Глобальные переменные
let authToken = localStorage.getItem('authToken');
let currentUser = localStorage.getItem('currentUser');
let plugins = [];

// Инициализация приложения
document.addEventListener('DOMContentLoaded', function() {
    console.log('Инициализация админки...');
    
    // Проверяем авторизацию
    if (authToken && currentUser) {
        showAdminPage();
        loadDashboard();
    } else {
        showLoginPage();
    }
    
    // Настройка обработчиков событий
    setupEventHandlers();
});

// Настройка обработчиков событий
function setupEventHandlers() {
    // Форма авторизации
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
    
    // Навигация
    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', handleNavigation);
    });
    
    // Форма создания плагина
    document.getElementById('createPluginForm').addEventListener('submit', function(e) {
        e.preventDefault();
    });
    
    // Форма загрузки версии
    document.getElementById('uploadVersionForm').addEventListener('submit', handleVersionUpload);
}

// === АВТОРИЗАЦИЯ ===

function showLoginPage() {
    document.getElementById('loginPage').style.display = 'flex';
    document.getElementById('adminPage').style.display = 'none';
}

function showAdminPage() {
    document.getElementById('loginPage').style.display = 'none';
    document.getElementById('adminPage').style.display = 'block';
    document.getElementById('currentUser').textContent = currentUser;
}

async function handleLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const submitBtn = e.target.querySelector('button[type="submit"]');
    const loading = submitBtn.querySelector('.loading');
    const errorDiv = document.getElementById('loginError');
    
    // Показываем индикатор загрузки
    loading.classList.add('show');
    submitBtn.disabled = true;
    errorDiv.style.display = 'none';
    
    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });
        
        const data = await response.json();
        
        if (response.ok) {
            // Сохраняем токен и пользователя
            authToken = data.token;
            currentUser = data.username;
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('currentUser', currentUser);
            
            console.log('Авторизация успешна');
            showAdminPage();
            loadDashboard();
        } else {
            showError(errorDiv, data.message || 'Ошибка авторизации');
        }
    } catch (error) {
        console.error('Ошибка при авторизации:', error);
        showError(errorDiv, 'Ошибка подключения к серверу');
    } finally {
        loading.classList.remove('show');
        submitBtn.disabled = false;
    }
}

function logout() {
    authToken = null;
    currentUser = null;
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    showLoginPage();
}

// === НАВИГАЦИЯ ===

function handleNavigation(e) {
    e.preventDefault();
    
    const page = e.target.closest('.nav-link').dataset.page;
    
    // Обновляем активную ссылку
    document.querySelectorAll('.nav-link').forEach(link => {
        link.classList.remove('active');
    });
    e.target.closest('.nav-link').classList.add('active');
    
    // Показываем нужную страницу
    document.querySelectorAll('.page-content').forEach(content => {
        content.style.display = 'none';
    });
    
    switch (page) {
        case 'dashboard':
            document.getElementById('dashboardPage').style.display = 'block';
            loadDashboard();
            break;
        case 'plugins':
            document.getElementById('pluginsPage').style.display = 'block';
            loadPlugins();
            break;
        case 'upload':
            document.getElementById('uploadPage').style.display = 'block';
            loadPluginsForSelect();
            break;
    }
}

// === API ЗАПРОСЫ ===

async function apiRequest(url, options = {}) {
    const defaultOptions = {
        headers: {
            'Authorization': `Bearer ${authToken}`,
            'Content-Type': 'application/json'
        }
    };
    
    // Если это FormData, не устанавливаем Content-Type
    if (options.body instanceof FormData) {
        delete defaultOptions.headers['Content-Type'];
    }
    
    const finalOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...options.headers
        }
    };
    
    try {
        const response = await fetch(url, finalOptions);
        
        if (response.status === 401) {
            // Токен истек
            logout();
            return null;
        }
        
        return response;
    } catch (error) {
        console.error('Ошибка API запроса:', error);
        throw error;
    }
}

// === ДАШБОРД ===

async function loadDashboard() {
    console.log('Загрузка дашборда...');
    
    try {
        const response = await apiRequest(`${API_BASE_URL}/admin/plugins`);
        if (!response || !response.ok) return;
        
        plugins = await response.json();
        
        // Обновляем статистику
        updateDashboardStats();
        
        // Показываем последние плагины
        displayRecentPlugins();
        
    } catch (error) {
        console.error('Ошибка при загрузке дашборда:', error);
    }
}

function updateDashboardStats() {
    const totalPlugins = plugins.length;
    const totalVersions = plugins.reduce((sum, plugin) => sum + plugin.versions.length, 0);
    const totalSize = plugins.reduce((sum, plugin) => {
        return sum + plugin.versions.reduce((vSum, version) => vSum + version.fileSize, 0);
    }, 0);
    
    document.getElementById('totalPlugins').textContent = totalPlugins;
    document.getElementById('totalVersions').textContent = totalVersions;
    document.getElementById('totalSize').textContent = formatFileSize(totalSize);
}

function displayRecentPlugins() {
    const container = document.getElementById('recentPlugins');
    
    if (plugins.length === 0) {
        container.innerHTML = '<div class="col-12"><p class="text-muted text-center">Плагины не найдены</p></div>';
        return;
    }
    
    // Сортируем по дате обновления
    const recentPlugins = [...plugins]
        .sort((a, b) => new Date(b.updatedAt) - new Date(a.updatedAt))
        .slice(0, 6);
    
    container.innerHTML = recentPlugins.map(plugin => `
        <div class="col-md-4 mb-3">
            <div class="card plugin-card h-100">
                <div class="card-body">
                    <h6 class="card-title">${escapeHtml(plugin.name)}</h6>
                    <p class="card-text small text-muted">${escapeHtml(plugin.description)}</p>
                    ${plugin.latestVersion ? `
                        <span class="badge bg-primary version-badge">v${plugin.latestVersion.version}</span>
                        <div class="file-size mt-1">${formatFileSize(plugin.latestVersion.fileSize)}</div>
                    ` : '<span class="badge bg-secondary">Нет версий</span>'}
                </div>
            </div>
        </div>
    `).join('');
}

// === УПРАВЛЕНИЕ ПЛАГИНАМИ ===

async function loadPlugins() {
    console.log('Загрузка списка плагинов...');
    
    try {
        const response = await apiRequest(`${API_BASE_URL}/admin/plugins`);
        if (!response || !response.ok) return;
        
        plugins = await response.json();
        displayPlugins();
        
    } catch (error) {
        console.error('Ошибка при загрузке плагинов:', error);
    }
}

function displayPlugins() {
    const container = document.getElementById('pluginsList');
    
    if (plugins.length === 0) {
        container.innerHTML = '<div class="col-12"><p class="text-muted text-center">Плагины не найдены</p></div>';
        return;
    }
    
    container.innerHTML = plugins.map(plugin => `
        <div class="col-md-6 mb-4">
            <div class="card plugin-card h-100">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h6 class="mb-0">${escapeHtml(plugin.name)}</h6>
                    <span class="badge bg-info">${plugin.uniqueId}</span>
                </div>
                <div class="card-body">
                    <p class="card-text">${escapeHtml(plugin.description)}</p>
                    
                    <div class="mb-3">
                        <strong>Версии (${plugin.versions.length}):</strong>
                        ${plugin.versions.length > 0 ? `
                            <div class="mt-2">
                                ${plugin.versions.slice(0, 3).map(version => `
                                    <div class="d-flex justify-content-between align-items-center mb-1">
                                        <span class="badge bg-primary">v${version.version}</span>
                                        <small class="text-muted">${formatFileSize(version.fileSize)}</small>
                                        <button class="btn btn-sm btn-outline-danger" 
                                                onclick="deleteVersion(${plugin.id}, '${version.version}')">
                                            <i class="bi bi-trash"></i>
                                        </button>
                                    </div>
                                `).join('')}
                                ${plugin.versions.length > 3 ? `<small class="text-muted">и еще ${plugin.versions.length - 3}...</small>` : ''}
                            </div>
                        ` : '<span class="text-muted">Нет версий</span>'}
                    </div>
                    
                    <div class="d-flex gap-2">
                        <button class="btn btn-sm btn-primary" onclick="uploadVersionForPlugin(${plugin.id})">
                            <i class="bi bi-plus"></i> Версия
                        </button>
                        <button class="btn btn-sm btn-outline-info" onclick="viewPlugin(${plugin.id})">
                            <i class="bi bi-eye"></i> Просмотр
                        </button>
                    </div>
                </div>
                <div class="card-footer text-muted small">
                    Создан: ${formatDate(plugin.createdAt)}
                    ${plugin.updatedAt !== plugin.createdAt ? `<br>Обновлен: ${formatDate(plugin.updatedAt)}` : ''}
                </div>
            </div>
        </div>
    `).join('');
}

async function createPlugin() {
    const name = document.getElementById('pluginName').value;
    const description = document.getElementById('pluginDescription').value;
    const uniqueId = document.getElementById('pluginUniqueId').value;
    const file = document.getElementById('initialFile').files[0];
    
    const button = document.querySelector('#createPluginModal .btn-primary');
    const loading = button.querySelector('.loading');
    
    loading.classList.add('show');
    button.disabled = true;
    
    try {
        const formData = new FormData();
        formData.append('name', name);
        formData.append('description', description);
        formData.append('uniqueId', uniqueId);
        
        if (file) {
            formData.append('file', file);
        }
        
        const response = await apiRequest(`${API_BASE_URL}/admin/plugins`, {
            method: 'POST',
            body: formData
        });
        
        if (response && response.ok) {
            console.log('Плагин создан успешно');
            
            // Закрываем модальное окно
            const modal = bootstrap.Modal.getInstance(document.getElementById('createPluginModal'));
            modal.hide();
            
            // Очищаем форму
            document.getElementById('createPluginForm').reset();
            
            // Обновляем список плагинов
            loadPlugins();
            
            showSuccess('Плагин создан успешно!');
        } else {
            const error = await response.json();
            showError(null, error.message || 'Ошибка при создании плагина');
        }
    } catch (error) {
        console.error('Ошибка при создании плагина:', error);
        showError(null, 'Ошибка подключения к серверу');
    } finally {
        loading.classList.remove('show');
        button.disabled = false;
    }
}

// === ЗАГРУЗКА ВЕРСИЙ ===

async function loadPluginsForSelect() {
    try {
        const response = await apiRequest(`${API_BASE_URL}/admin/plugins`);
        if (!response || !response.ok) return;
        
        const plugins = await response.json();
        const select = document.getElementById('pluginSelect');
        
        select.innerHTML = '<option value="">Выберите плагин...</option>' +
            plugins.map(plugin => 
                `<option value="${plugin.id}">${escapeHtml(plugin.name)} (${plugin.uniqueId})</option>`
            ).join('');
            
    } catch (error) {
        console.error('Ошибка при загрузке плагинов для выбора:', error);
    }
}

async function handleVersionUpload(e) {
    e.preventDefault();
    
    const pluginId = document.getElementById('pluginSelect').value;
    const version = document.getElementById('versionInput').value;
    const releaseNotes = document.getElementById('releaseNotesInput').value;
    const file = document.getElementById('fileInput').files[0];
    
    if (!pluginId || !version || !file) {
        showError(null, 'Заполните все обязательные поля');
        return;
    }
    
    const submitBtn = e.target.querySelector('button[type="submit"]');
    const loading = submitBtn.querySelector('.loading');
    
    loading.classList.add('show');
    submitBtn.disabled = true;
    
    try {
        const formData = new FormData();
        formData.append('version', version);
        formData.append('releaseNotes', releaseNotes);
        formData.append('file', file);
        
        const response = await apiRequest(`${API_BASE_URL}/admin/plugins/${pluginId}/versions`, {
            method: 'POST',
            body: formData
        });
        
        if (response && response.ok) {
            console.log('Версия загружена успешно');
            
            // Очищаем форму
            document.getElementById('uploadVersionForm').reset();
            
            showSuccess('Версия загружена успешно!');
        } else {
            const error = await response.json();
            showError(null, error.message || 'Ошибка при загрузке версии');
        }
    } catch (error) {
        console.error('Ошибка при загрузке версии:', error);
        showError(null, 'Ошибка подключения к серверу');
    } finally {
        loading.classList.remove('show');
        submitBtn.disabled = false;
    }
}

async function deleteVersion(pluginId, version) {
    if (!confirm(`Удалить версию ${version}? Это действие нельзя отменить.`)) {
        return;
    }
    
    try {
        const response = await apiRequest(`${API_BASE_URL}/admin/plugins/${pluginId}/versions/${version}`, {
            method: 'DELETE'
        });
        
        if (response && response.ok) {
            console.log('Версия удалена успешно');
            loadPlugins(); // Обновляем список
            showSuccess('Версия удалена успешно!');
        } else {
            const error = await response.json();
            showError(null, error.message || 'Ошибка при удалении версии');
        }
    } catch (error) {
        console.error('Ошибка при удалении версии:', error);
        showError(null, 'Ошибка подключения к серверу');
    }
}

// === УТИЛИТЫ ===

function formatFileSize(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showError(container, message) {
    if (container) {
        container.textContent = message;
        container.style.display = 'block';
    } else {
        // Показываем toast уведомление
        showToast(message, 'danger');
    }
}

function showSuccess(message) {
    showToast(message, 'success');
}

function showToast(message, type = 'info') {
    // Создаем toast элемент
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">${escapeHtml(message)}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    // Добавляем контейнер для toast если его нет
    let toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    
    // Добавляем toast
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    // Показываем toast
    const toastElement = toastContainer.lastElementChild;
    const toast = new bootstrap.Toast(toastElement);
    toast.show();
    
    // Удаляем элемент после скрытия
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

function refreshData() {
    const activePage = document.querySelector('.nav-link.active').dataset.page;
    
    switch (activePage) {
        case 'dashboard':
            loadDashboard();
            break;
        case 'plugins':
            loadPlugins();
            break;
        case 'upload':
            loadPluginsForSelect();
            break;
    }
    
    showSuccess('Данные обновлены');
}

// Дополнительные функции для управления плагинами
function uploadVersionForPlugin(pluginId) {
    // Переключаемся на страницу загрузки
    document.querySelector('.nav-link[data-page="upload"]').click();
    
    // Выбираем плагин в селекте
    setTimeout(() => {
        document.getElementById('pluginSelect').value = pluginId;
    }, 100);
}

function viewPlugin(pluginId) {
    const plugin = plugins.find(p => p.id === pluginId);
    if (!plugin) return;
    
    alert(`Плагин: ${plugin.name}\nID: ${plugin.uniqueId}\nОписание: ${plugin.description}\nВерсий: ${plugin.versions.length}`);
}