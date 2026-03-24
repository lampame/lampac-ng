# Lampac Next Generation

[![Build](https://github.com/lampac-nextgen/lampac/actions/workflows/build.yml/badge.svg)](https://github.com/lampac-nextgen/lampac/actions/workflows/build.yml)
[![Release](https://github.com/lampac-nextgen/lampac/actions/workflows/release.yml/badge.svg)](https://github.com/lampac-nextgen/lampac/actions/workflows/release.yml)
[![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/lampac-nextgen/lampac?label=version)](https://github.com/lampac-nextgen/lampac/releases)
[![GitHub tag (latest SemVer pre-release)](https://img.shields.io/github/v/tag/lampac-nextgen/lampac?include_prereleases&label=pre-release)](https://github.com/lampac-nextgen/lampac/tags)

> **Самохостируемый backend-сервер для расширения [Lampa](https://github.com/yumata/lampa)** — собирает ссылки на публично доступный контент с десятков российских, украинских, СНГ-сервисов, аниме-источников и западных платформ и передаёт их Lampa в виде плагинов. Построен на ASP.NET Core (.NET 10).

---

## AI Документация

[![DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/lampac-nextgen/lampac)

## Telegram Чат

[Lampac NextGen](https://t.me/LampacTalks/13998)

## Содержание

1. [AI Документация](#ai-документация)
2. [Telegram Чат](#telegram-чат)
3. [Обзор проекта](#обзор-проекта)
4. [Архитектура](#архитектура)
5. [Возможности](#возможности)
6. [Быстрый старт](#быстрый-старт)
   - [Docker](#docker)
   - [Нативная установка (Linux)](#нативная-установка-linux)
   - [Ручная сборка](#ручная-сборка)
7. [Конфигурация](#конфигурация)
   - [Основные параметры](#основные-параметры)
   - [Аутентификация (accsdb)](#аутентификация-accsdb)
   - [WAF и безопасность](#waf-и-безопасность)
   - [Провайдеры онлайн-кино](#провайдеры-онлайн-кино)
8. [Модули](#модули)
9. [Провайдеры контента](#провайдеры-контента)
   - [VOD (онлайн-кино)](#vod-онлайн-кино)
   - [Аниме](#аниме)
   - [Англоязычный контент](#англоязычный-контент)
   - [Украинские CDN](#украинские-cdn)
   - [SISI (контент 18+)](#sisi-контент-18)
10. [API-эндпоинты](#api-эндпоинты)
11. [Зависимости](#зависимости)
12. [Структура проекта](#структура-проекта)

---

## Обзор проекта

[Lampa](https://github.com/yumata/lampa) — бесплатное приложение для просмотра информации о фильмах, новинках и популярных релизах. Оно использует публичные ссылки и не распространяет контент через собственные серверы — вся информация отображается исключительно в познавательных целях.

**Lampac Next Generation** — самохостируемый backend-сервер для расширения [Lampa](https://github.com/yumata/lampa) через плагины. Он собирает ссылки на публично доступный контент с десятков российских, украинских, СНГ-сервисов, аниме-источников и западных платформ, отдаёт их Lampa в виде структурированных JSON API-ответов и дополнительно предоставляет: проксирование метаданных TMDB, встроенный TorrServer, DLNA/UPnP-медиасервер, транскодинг через FFmpeg, управление субтитрами и синхронизацию закладок между устройствами. Запускается на порту **9118**.

---

## Архитектура

```text
┌────────────────────────────────────────────────────────────────┐
│  Core  (ASP.NET Core Web Host, порт 9118)                      │
│  Program.cs → Startup.cs → Middleware Pipeline                 │
├────────────────────┬───────────────────────────────────────────┤
│  Shared (lib)      │  BaseController, CoreInit (конфиг),       │
│                    │  модели, сервисы, Playwright, HTTP-пулы   │
├────────────────────┴───────────────────────────────────────────┤
│  Динамически загружаемые модули                                │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌───────────────────┐    │
│  │ Online  │ │  SISI   │ │ Catalog  │ │    LampaWeb       │    │
│  │(VOD API)│ │ (18+)   │ │(каталог) │ │(Lampa UI)         │    │
│  └─────────┘ └─────────┘ └──────────┘ └───────────────────┘    │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌───────────────────┐    │
│  │TorrServr│ │  DLNA   │ │  JacRed  │ │   Transcoding     │    │
│  └─────────┘ └─────────┘ └──────────┘ └───────────────────┘    │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌───────────────────┐    │
│  │TmdbProxy│ │  Sync   │ │ TimeCode │ │     Tracks        │    │
│  └─────────┘ └─────────┘ └──────────┘ └───────────────────┘    │
│  ┌─────────┐                                                   │
│  │CubProxy │                                                   │
│  └─────────┘                                                   │
└────────────────────────────────────────────────────────────────┘
```

**Слои:**

| Слой | Описание |
| --- | --- |
| **Core** | Точка входа, Middleware Pipeline, основной `ApiController` |
| **Shared** | Общие модели, базовые контроллеры, конфигурация, HTTP-пулы |
| **Online** | Модуль VOD: агрегация потоков с ~60 провайдеров |
| **SISI** | Модуль контента 18+: ~15 платформ |
| **Modules/** | Дополнительные модули, загружаемые как ASP.NET Application Parts |

Каждый модуль — отдельная .NET-библиотека с исходным кодом, компилируемая при запуске сервера через Roslyn (`CSharpEval`). Папка `module/` содержит **базовые модули**, поставляемые вместе с сервером. Папка `mods/` предназначена для **пользовательских модулей** — пользователь самостоятельно размещает там папку модуля с исходниками.

---

## Возможности

- **~60 VOD-провайдеров** — Rezka, Filmix, KinoPub, HDVB, Collaps, CDNmovies, Kodik, VideoCDN и многие другие
- **~15 аниме-источников** — AniLibria, Kodik, AniLiberty, AnimeGo, AniMedia и другие
- **Англоязычный контент** — VidSrc, AutoEmbed, SmashyStream, TwoEmbed, VidLink и другие
- **SISI (18+)** — PornHub, XVideos, XHamster, Chaturbate, BongaCams и другие
- **TorrServer** — встроенный торрент-сервер, управляемый как подпроцесс
- **DLNA/UPnP** — медиасервер для локальных файлов
- **JacRed** — агрегатор торрент-индексаторов (совместим с Jackett)
- **Transcoding** — транскодинг через FFmpeg (до 5 одновременных заданий)
- **Tracks** — управление субтитрами и звуковыми дорожками, интеграция с FFprobe
- **Sync** — кросс-девайсная синхронизация закладок и истории (SQLite)
- **TimeCode** — сохранение позиции воспроизведения (продолжение с места)
- **TmdbProxy** — локальный кеш TMDB API для снижения нагрузки
- **LampaWeb** — встроенный хостинг Lampa UI (авто-обновление с GitHub)
- **Playwright** — автоматизация браузера (Chromium/Firefox) для обхода JS-защит
- **RCH (Remote Client Hub)** — WebSocket-реле для клиентов за NAT
- **Горячая перезагрузка конфига** — изменения в `init.conf` применяются без перезапуска
- **WAF** — встроенный брандмауэр с ограничением запросов, геоблокировкой и защитой от брутфорса
- **GeoIP** — определение страны и ASN (MaxMind GeoLite2, базы включены в поставку)
- **Многоплатформенность** — `linux/amd64`, `linux/arm64`

---

## Быстрый старт

### Docker

```bash
# Клонировать репозиторий
git clone https://github.com/lampac-nextgen/lampac.git
cd lampac

# Запустить через docker compose
docker compose -f docker/docker-compose.yaml up -d
```

Сервер будет доступен по адресу `http://<IP>:9118`.

Основные тома для монтирования:

| Путь в контейнере | Описание |
| --- | --- |
| `/lampac/config/passwd` | Файл с root-паролем |
| `/lampac/config/init.conf` | Основной файл конфигурации |
| `/lampac/module/` | Базовые модули (исходники, поставляются с сервером, компилируются при запуске) |
| `/lampac/mods/` | Пользовательские модули (исходники, подкладываются пользователем, компилируются при запуске) |

**Пример `docker-compose.yaml`:**

```yaml
services:
  lampac:
    image: ghcr.io/lampac-nextgen/lampac
    network_mode: host
    shm_size: 1024mb
    volumes:
      - ./config/passwd:/lampac/config/passwd
      - ./config/init.conf:/lampac/config/init.conf
    restart: unless-stopped
```

### Нативная установка (Linux)

Скрипт автоматически устанавливает .NET 10, создаёт системного пользователя и регистрирует systemd-сервис:

```bash
# Установка
curl -fsSL https://raw.githubusercontent.com/lampac-nextgen/lampac/main/install.sh | sudo bash

# Обновление
curl -fsSL https://raw.githubusercontent.com/lampac-nextgen/lampac/main/install.sh | sudo bash -s -- --update

# Предрелизная версия
curl -fsSL https://raw.githubusercontent.com/lampac-nextgen/lampac/main/install.sh | sudo bash -s -- --pre-release

# Удаление
curl -fsSL https://raw.githubusercontent.com/lampac-nextgen/lampac/main/install.sh | sudo bash -s -- --remove
```

Переменные окружения для кастомизации:

| Переменная | По умолчанию | Описание |
| --- | --- | --- |
| `LAMPAC_INSTALL_ROOT` | `/opt/lampac` | Директория установки |
| `LAMPAC_USER` | `lampac` | Системный пользователь |
| `LAMPAC_PORT` | `9118` | Порт прослушивания |

Управление сервисом:

```bash
systemctl start lampac
systemctl stop lampac
systemctl status lampac
journalctl -u lampac -f
```

### Ручная сборка

Требования: .NET SDK 10.0+

```bash
# Сборка
./build.sh

# Или напрямую
dotnet publish Core/Core.csproj -c Release -o publish

# Запуск
cd publish
dotnet Core.dll
```

Для кросс-компиляции укажите `RUNTIME_ID`:

```bash
RUNTIME_ID=linux-arm64 ./build.sh
```

---

## Конфигурация

Конфигурация хранится в файле `init.conf` (JSON) или `init.yaml`. Файл отслеживается каждую секунду и **перезагружается без перезапуска сервера** при изменении. Резервные копии сохраняются в `database/backup/init/`.

### Основные параметры

```jsonc
{
  // Порт и сетевые настройки
  "listen": {
    "port": 9118,
    "https": false,
    "compression": true,
    "ResponseCancelAfter": 15   // таймаут ответа, секунды
  },

  // Базовые настройки модуля
  "BaseModule": {
    "ValidateRequest": true,    // валидация входящих запросов
    "BlockedBots": true,        // блокировка ботов
    "SkipModules": []           // список отключённых модулей
  },

  // Кеш
  "cache": {
    "extend": 180               // продление TTL кеша, минуты
  },

  // Playwright (браузерная автоматизация)
  "chromium": {
    "enable": false,
    "count": 1,
    "restart": 3600             // перезапуск каждые N секунд
  },
  "firefox": {
    "enable": false,
    "count": 1
  },

  // Remote Client Hub (WebSocket-реле для клиентов за NAT)
  "rch": {
    "enable": false,
    "requiredConnected": 1
  },

  // Логирование
  "serilog": {
    "enable": false             // запись в файл logs/ (14 дней хранения)
  },

  // Управление памятью
  "GC": {
    "Concurrent": true,
    "ConserveMemory": 0,
    "HighMemoryPercent": 90,
    "RetainVM": false
  },

  // Ключ шифрования потоков
  "kit": {
    "aesgcmkeyName": ""
  }
}
```

### Аутентификация (accsdb)

```jsonc
{
  "accsdb": {
    "enable": true,
    "accounts": "user1:2026-12-31,user2:2027-06-01",
    // или подробный формат:
    "users": [
      { "login": "user1", "expired": "2026-12-31" },
      { "login": "user2", "expired": "2027-06-01" }
    ]
  }
}
```

### WAF и безопасность

```jsonc
{
  "WAF": {
    "enable": true,
    "countryAllow": ["RU", "UA", "BY"],   // геоблокировка (пустой — все)
    "whiteIps": ["192.168.1.0/24"],        // белый список IP/CIDR
    "bruteForceProtection": true,
    "limit_map": {                         // лимиты запросов по маршрутам
      "/lite/": 10,
      "/externalids": 10
    }
  }
}
```

### Провайдеры онлайн-кино

Каждый провайдер настраивается в своём разделе `init.conf`. Пример для Rezka:

```jsonc
{
  "Rezka": {
    "enable": true,
    "host": "https://rezka.ag",
    "priority": 1
  },
  "Filmix": {
    "enable": true,
    "host": "https://filmix.biz",
    "token": "ВАШ_ТОКЕН",
    "priority": 2
  },
  "KinoPub": {
    "enable": true,
    "token": "ВАШ_ТОКЕН"
  },
  "Kodik": {
    "enable": true,
    "token": "ВАШ_ТОКЕН"
  }
}
```

---

## Модули

**Примечание по статусу модулей по умолчанию:** согласно `base.conf`, модули **Catalog**, **DLNA**, **Tracks** и **Transcoding** отключены по умолчанию (`SkipModules`). Также по умолчанию отключены **WAF** и **accsdb** (аутентификация).

> [!WARNING]
> Модули **DLNA**, **Tracks**, **Transcoding** и **Catalog** не выполняют экранирование входящих запросов. **Не включайте их на публично доступном VPS** без ограничения доступа. Рекомендуется либо не активировать эти модули на публичном сервере, либо закрыть к ним доступ на уровне firewall/reverse proxy (разрешить только доверенные IP).

| Модуль | По умолч. | Описание |
| --- | :---: | --- |
| **Online** | ✅ вкл | Агрегация VOD-потоков с ~60 провайдеров. Плагин `/online.js` для Lampa. WAF: 10 req/s на `/lite/`. |
| **SISI** | ✅ вкл | Контент 18+ с ~15 платформ. Плагин `/sisi.js`. История и закладки в SQLite. |
| **Catalog** | ⛔ откл | Браузер каталогов сайтов на основе YAML-описаний из папки `sites/`. Эндпоинт `/catalog/`. Без экранирования запросов — только в доверенной сети. |
| **CubProxy** | ✅ вкл | HTTP/HTTPS прокси с файловым кешем (`cache/cub/`), FileSystemWatcher для инвалидации кеша. |
| **DLNA** | ⛔ откл | DLNA/UPnP медиасервер. Обслуживает локальные файлы, автозагрузка трекеров торрентов. Форматы: aac, flac, mp4, mkv, ts, webm, avi и другие. Без экранирования запросов — только в доверенной сети. |
| **JacRed** | ✅ вкл | Агрегатор торрент-индексаторов (совместимый с Jackett API). Источники: Rutor, Megapeer, Kinozal, Rutracker, NNMClub, Toloka, Bitru и другие. |
| **LampaWeb** | ✅ вкл | Встроенный хостинг Lampa Web UI. Автообновление с GitHub каждые 90 минут. |
| **Sync** | ✅ вкл | Синхронизация хранилища и закладок между устройствами. Эндпоинты `/storage/`, `/bookmark/`. SQLite-бэкенд. |
| **TimeCode** | ✅ вкл | Сохранение и восстановление позиции воспроизведения (`resume from`). SQLite. |
| **TmdbProxy** | ✅ вкл | Локальный кеш TMDB API (`cache/tmdb/`). Снижает нагрузку на TMDB и ускоряет ответы. |
| **TorrServer** | ✅ вкл | Управление внешним процессом TorrServer. Проксирует `/ts/`. Генерирует случайный пароль за сессию. |
| **Tracks** | ⛔ откл | Управление субтитрами и дорожками (`database/tracks/`). Интеграция с FFprobe (`/ffprobe`). Без экранирования запросов — только в доверенной сети. |
| **Transcoding** | ⛔ откл | HLS/DASH транскодинг через FFmpeg. Макс. 5 параллельных заданий. Таймаут простоя 5 мин. Кеш: `cache/transcoding/`. Без экранирования запросов — только в доверенной сети. |

### Подключение пользовательских модулей

**Пользовательский модуль** — создайте поддиректорию в `mods/` с файлом `manifest.json` и исходными `.cs`-файлами. Модуль будет скомпилирован при запуске сервера через Roslyn (`CSharpEval`).

Пример структуры (`mods/MyModule/`):

```json
{
  "name": "MyModule",
  "description": "Описание модуля",
  "version": "1.0"
}
```

Модуль будет скомпилирован через Roslyn (`CSharpEval`) при старте сервера.

---

## Провайдеры контента

### VOD (онлайн-кино)

| Провайдер | Сервис | Примечания |
| --- | --- | --- |
| `Alloha` | Alloha CDN | |
| `CDNmovies` | CDN Movies | Офлайн БД `data/cdnmovies.json` |
| `CDNvideohub` | CDN VideoHub | |
| `Collaps` | Collaps | Включая DASH-вариант |
| `FanCDN` | FanCDN | |
| `Filmix` | Filmix.my | FilmixPartner, FilmixTV варианты |
| `FlixCDN` | FlixCDN | |
| `Geosaitebi` / `AsiaGe` | Georgian CDN | |
| `GetsTV` | GetsTV | |
| `HDVB` | HDVB | |
| `IptvOnline` | IPTV Online | |
| `KinoPub` | KinoPub | Требует токен |
| `Kinobase` | KinoBase | |
| `Kinoflix` | Kinoflix | |
| `Kinogo` | Kinogo | |
| `Kinotochka` | Kinotochka | |
| `LeProduction` | Le Production | |
| `Lumex` | Lumex | Офлайн БД `data/lumex.json` (~130k записей) |
| `Mirage` | Mirage CDN | |
| `PiTor` | PidTor | Стриминг через торрент |
| `Plvideo` | Plvideo | |
| `Redheadsound` | RedheadSound | |
| `Rezka` / `RezkaPremium` | HDRezka | |
| `RutubeMovie` | Rutube | |
| `VDBmovies` | VDBmovies | |
| `VeoVeo` | VeoVeo | Офлайн БД `data/veoveo.json` |
| `Vibix` | Vibix | |
| `VideoCDN` / `VideoDB` / `Videoseed` | Video CDN | |
| `VkMovie` | VK Видео | |
| `VoKino` | VoKino | |
| `iRemux` | iRemux | |

### Аниме

| Провайдер | Сервис |
| --- | --- |
| `AniLiberty` | AniLiberty |
| `AniLibria` | AniLibria |
| `AniMedia` | AniMedia |
| `AnimeGo` | AnimeGo |
| `AnimeLib` | AnimeLib |
| `AnimeBesst` | AnimeBesst |
| `Animevost` | Animevost |
| `Dreamerscast` | Dreamerscast |
| `Kodik` | Kodik (универсальный, VOD + аниме) |
| `MoonAnime` | MoonAnime |

### Англоязычный контент

| Провайдер | Сервис |
| --- | --- |
| `AutoEmbed` | AutoEmbed |
| `HydraFlix` | HydraFlix |
| `MovPI` | MovPI |
| `PlayEmbed` | PlayEmbed |
| `RgShows` | RgShows |
| `SmashyStream` | SmashyStream |
| `TwoEmbed` | TwoEmbed |
| `VidLink` | VidLink |
| `VidSrc` | VidSrc |
| `Videasy` | Videasy |

### Украинские CDN

| Провайдер | Сервис |
| --- | --- |
| `Ashdi` | Ashdi |
| `Eneyida` | Eneyida |
| `Kinoukr` | KinoUkr (офлайн БД `data/kinoukr.json`, ~130k записей) |

### SISI (контент 18+)

| Провайдер | Сервис | Эндпоинт |
| --- | --- | --- |
| `BongaCams` | BongaCams | `/bgs` |
| `Chaturbate` | Chaturbate | `/chu` |
| `Ebalovo` | Ebalovo | `/elo` |
| `Eporner` | Eporner | `/epr` |
| `HQporner` | HQporner | `/hqr` |
| `PornHub` | PornHub | `/phub` |
| `PornHubPremium` | PornHub Premium | |
| `Porntrex` | Porntrex | `/ptx` |
| `Runetki` | Runetki | `/runetki` |
| `Spankbang` | Spankbang | `/sbg` |
| `Tizam` | Tizam | `/tizam` |
| `Xhamster` | XHamster | `/xmr` |
| `Xnxx` | XNXX | `/xnx` |
| `Xvideos` / `XvideosRED` | XVideos / XVideos RED | `/xds` |

---

## API-эндпоинты

### Core

| Метод | Путь | Описание |
| --- | --- | --- |
| `GET` | `/version` | Версия сервера |
| `GET` | `/api/headers` | Заголовки текущего запроса (JSON/text) |
| `GET` | `/api/geo[?ip=]` | GeoIP-локация IP-адреса |
| `GET` | `/api/myip` | IP-адрес клиента |
| `GET` | `/api/chromium/ping` | Пинг Playwright-браузера (ответ: `pong`) |
| `POST` | `/rch/result?id=` | RCH-реле: запись результата (макс. 10 МБ) |
| `POST` | `/rch/gzresult?id=` | RCH-реле: запись gzip-результата (макс. 10 МБ) |
| `WS` | `/ws` | NativeWebSocket для RCH push |

### Online (VOD)

| Метод | Путь | Описание |
| --- | --- | --- |
| `GET` | `/online.js` | Lampa VOD-плагин (JavaScript) |
| `GET` | `/online/js/{token}` | Плагин с авторизацией по токену |
| `GET` | `/lite/{provider}` | Список источников от провайдера (напр. `/lite/rezka`) |
| `GET` | `/externalids` | Маппинг внешних ID (TMDB ↔ KinoPoisk и т.д.) |
| `GET` | `/lifeevents` | SSE-поток событий здоровья провайдеров |

### SISI (18+)

| Метод | Путь | Описание |
| --- | --- | --- |
| `GET` | `/sisi.js` | Lampa SISI-плагин (JavaScript) |
| `GET` | `/sisi/js/{token}` | Плагин с авторизацией по токену |
| `GET` | `/{provider}` | Контент платформы (напр. `/phub`, `/xnx`) |
| `GET` | `/sisi/bookmark` | Управление закладками |
| `GET` | `/sisi/history` | История просмотров |

### Маршруты модулей

| Метод | Путь | Описание |
| --- | --- | --- |
| `GET` | `/catalog/{site}/…` | Просмотр каталога сайтов |
| `GET` | `/dlna/…` | DLNA медиасервер |
| `GET` | `/storage/…` | Синхронизация хранилища |
| `GET` | `/bookmark/…` | Синхронизация закладок |
| `GET` | `/timecode/…` | Позиции воспроизведения |
| `GET` | `/tmdb/…` | TMDB прокси/кеш |
| `GET` | `/transcoding/…` | HLS/DASH транскодинг |
| `GET` | `/ffprobe` | Метаданные дорожек (FFprobe) |
| `GET` | `/nexthub` | SISI NextHUB браузер |
| `GET` | `/ts/…` | TorrServer |

---

## Зависимости

**Платформа:** .NET 10.0

| Пакет | Версия | Назначение |
| --- | --- | --- |
| `Microsoft.CodeAnalysis.CSharp` + `.Scripting` | 5.0.0 | Roslyn: компиляция модулей на лету |
| `Microsoft.Playwright` | 1.50.0 | Браузерная автоматизация (Chromium/Firefox) |
| `HtmlAgilityPack` | 1.12.4 | Парсинг HTML |
| `MaxMind.GeoIP2` | 5.4.1 | GeoIP (базы `GeoLite2-*.mmdb` включены в поставку) |
| `Newtonsoft.Json` | 13.0.4 | JSON-сериализация |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.2 | ORM для SQLite (Sync, TimeCode, SISI, ExternalIds) |
| `Microsoft.IO.RecyclableMemoryStream` | 3.0.1 | Пул памяти для потоков |
| `NetVips` / `NetVips.Native` | 3.2.0 / 8.18.0 | Обработка изображений (libvips) |
| `YamlDotNet` | 16.3.0 | Парсинг YAML-конфигурации |
| `Serilog.AspNetCore` + `.Sinks.File` | 9.0.0 / 7.0.0 | Структурное логирование |
| `HtmlKit` | 1.2.0 | Парсинг HTML |
| `System.Management` | 10.0.2 | Информация об ОС и железе |

---

## Структура проекта

```text
lampac/
├── Core/                       # Точка входа, Middleware Pipeline
│   ├── Program.cs              # Запуск приложения, инициализация
│   ├── Startup.cs              # DI-контейнер, HTTP-клиенты, загрузка модулей
│   ├── Controllers/            # ApiController, RchApiEndpoints
│   ├── Middlewares/            # WAF, Accsdb, BaseMod, ProxyImg и другие
│   ├── Services/               # CronCacheWatcher, NativeWebSocket
│   ├── data/                   # GeoIP базы, статические JSON-базы
│   ├── plugins/                # JS-плагины (RCH, NWS)
│   └── wwwroot/                # Статические файлы
├── Shared/                     # Общая библиотека
│   ├── CoreInit.cs             # Загрузка и hot-reload конфигурации
│   ├── BaseController.cs       # Базовый контроллер
│   ├── Models/                 # Общие модели данных
│   └── Services/               # Shared-сервисы
├── Online/                     # VOD-модуль
│   ├── Controllers/            # Контроллеры провайдеров (~60 источников)
│   │   ├── Anime/              # Аниме-провайдеры
│   │   ├── ENG/                # Англоязычный контент
│   │   └── UKR/                # Украинские CDN
│   ├── Config/                 # Конфигурационные модели
│   ├── ModInit.cs              # Инициализация модуля, загрузка БД
│   └── OnlineApi.cs            # `/online.js` эндпоинт
├── SISI/                       # Модуль контента 18+
│   ├── Controllers/            # Контроллеры платформ
│   ├── ModInit.cs              # Инициализация, SQLite, таймер очистки
│   └── SisiApi.cs              # `/sisi.js` эндпоинт
├── Modules/                    # Дополнительные модули
│   ├── Catalog/
│   ├── CubProxy/
│   ├── DLNA/
│   ├── JacRed/
│   ├── LampaWeb/
│   ├── Sync/
│   ├── TimeCode/
│   ├── TmdbProxy/
│   ├── TorrServer/
│   ├── Tracks/
│   └── Transcoding/
├── config/
│   └── base.conf               # Базовый шаблон конфигурации
├── docker/
│   ├── docker-compose.yaml     # Продакшн compose-файл
│   └── docker-compose.dev.yaml # Dev compose-файл
├── Dockerfile                  # Мультистейдж, мультиплатформ образ
├── build.sh                    # Скрипт сборки
├── install.sh                  # Скрипт нативной установки (Debian/Ubuntu)
└── NextGen.sln                 # Solution-файл
```

---
