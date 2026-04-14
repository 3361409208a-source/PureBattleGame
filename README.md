```
 ____            _        ____        _     _              _   _
|  _ \ ___  _ __| |_     | __ )  ___ | | __| | ___ _ __   | | | |_   _
| |_) / _ \| '__| __|    |  _ \ / _ \| |/ _` |/ _ \ '_ \  | |_| | | | |
|  __/ (_) | |  | |_     | |_) | (_) | | (_| |  __/ | | | |  _  | |_| |
|_|   \___/|_|   \__|    |____/ \___/|_|\__,_|\___|_| |_| |_| |_|\__, |
                                                                   |___/
```

  # 🎮 Pure Battle Hub

  > 🕹️ *模块化桌面娱乐枢纽 — 战斗、养成、摸鱼，一键切换*

  [![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![WinForms](https://img.shields.io/badge/UI-WinForms-0078D4?logo=windows)](https://learn.microsoft.com/dotnet/desktop/winforms/)
  [![WebView2](https://img.shields.io/badge/Browser-WebView2-0078D4?logo=microsoftedge)](https://developer.microsoft.com/microsoft-edge/webview2/)
  [![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)

  **[🇨🇳 中文](#中文文档)**  ·  **[🇺🇸 English](#english-documentation)**

  ---

  <a id="中文文档"></a>

  ## 📖 项目简介

  **Pure Battle Hub（纯战枢纽）** 是一款模块化桌面娱乐工具合集，专为 Windows 桌面环境设计。

  采用 **核心启动器 + 游戏模块** 分离架构，支持无限扩展新游戏模块，并内置办公友好功能——全局热键隐身、透明度调节、系统托盘驻留及嵌入式浏览器。

  > 💡 *一句话概括：在老板眼皮底下，优雅地享受战斗与养成的乐趣。*

  ## ✨ 核心特性

  <table>
  <tr>
  <td width="50%">

  🧩 **模块化架构**

  核心启动器（Hub）与游戏模块完全解耦，新增游戏只需实现模块接口，无限扩展 🚀

  </td>
  <td width="50%">

  🕵️ **办公友好**

  老板键一键隐身 · 透明度无缝切换 · 托盘驻留 · 内置浏览器 · 游戏状态保留

  </td>
  </tr>
  </table>

  <details>
  <summary>🔍 查看办公友好功能详情</summary>

  | 快捷键 | 功能 | 说明 |
  |:------:|------|------|
  | `Alt + Space` | 🫥 **老板键** | 一键全透明 + 从任务栏消失 |
  | `Alt + ↑` / `↓` | 👁️ **透明度调节** | 10 档透明度无缝切换 |
  | `Alt + B` | 🌐 **内置浏览器** | WebView2 多标签沉浸式浏览器 |
  | `Alt + Q` | 🏠 **返回 Hub** | 从任意游戏返回，保留游戏状态 |
  | 关闭按钮 | 📌 **托盘驻留** | 自动最小化到系统托盘 |

  </details>

  <details>
  <summary>⚙️ 持久化配置</summary>

  透明度、浏览器首页等设置自动保存至 `settings.json`，下次启动自动恢复。

  ```json
  {
    "DefaultOpacity": 0.95,
    "HomeUrl": "https://www.xiaoheiv.top",
    "AutoHideInTaskbar": true
  }
  ```

  </details>

  ## 🎮 内置游戏

  ---

  ### 🏆 星核防线 · Star Core Defense

  > ☄️ *科幻风格挂机塔防机器人战斗游戏 — 守护星核，抵御虫潮！*

  ```
    ⚡ Normal ──▶ Mega ──▶ Ultra ⚡
    ┌──────┐    ┌──────┐    ┌──────┐
    │ ▫▫▫▫ │ ──▶│ ████ │ ──▶│ ★★★ │
    └──────┘    └──────┘    └──────┘
  ```

  | | 特性 | 说明 |
  |:-:|------|------|
  | 🤖 | **9 大兵种** | 基地 · 采集工 · 医疗兵 · 守卫者 · 工程兵 · 机枪手 · 火箭兵 · 等离子兵 · 激光狙击 · 闪电特攻 |
  | 📊 | **3 级兵阶** | `Normal` ➜ `Mega` ➜ `Ultra`，属性逐级跃升 |
  | 🏠 | **基地模块** | 🛡️ 堡垒模式（高血量 + 减速场）/ ⚙️ 工业模式（极速采集 + 购买折扣） |
  | ☄️ | **资源经营** | 采集矿物 💎 → 赚取资金 💰 → 升级兵种属性 ⬆️ |
  | 👾 | **多样怪物** | 史莱姆 🟢 · 蜘蛛 🕷️ · 蝙蝠 🦇 · 蠕虫 🪱 — 随波次递增难度 |
  | ⚡ | **蓄力特效波** | 基地 6 秒蓄力 → 释放范围冲击波 💥 |
  | 🎵 | **动态音效** | 内置 BGM 与战斗音效系统 🎶 |
  | 🛡️ | **防御工事** | 工程兵建造/修复墙体防线 🧱 |

  ### 🐜 像素电子宠 · CockroachPet

  > 🤖 *桌面像素机器人互动终端 — 你的桌面宠物与战斗伙伴！*

  ```
    ╱╲
    ╱  ╲     "嘿！今天想和我聊点什么？"
  ╱ ◉  ◉    — 你的 AI 像素伙伴
  ╱  ──  ╲
  ╲  ▽  ╱
  ╲──╱
  ```

  | | 特性 | 说明 |
  |:-:|------|------|
  | 🤖 | **8 种性格** | 😊友好 · 😳害羞 · 😤叛逆 · 😄幽默 · 😐严肃 · 🤔好奇 · 😴懒惰 · 🔥精力充沛 |
  | 😊 | **9 种情绪** | 平静 → 开心 → 兴奋 → 难过 → 生气 → 害怕 → 好奇 → 无聊 → 困倦 |
  | ⚔️ | **自动战斗** | 机器人自主追逐、射击、格斗 🔫 多武器：激光 · 火箭 · 等离子 |
  | 👾 | **怪物投放** | 手动投放怪物 → 所有机器人集火攻击 🎯 |
  | 🧠 | **AI 自主思考** | 接入大语言模型（SiliconFlow API），机器人可自主对话与决策 |
  | 💻 | **嵌入式终端** | 每个机器人拥有独立终端，实时显示状态输出 |
  | 🎨 | **像素渲染** | GDI+ 像素级双缓冲绘制，逐帧动画丝滑无闪烁 ✨ |
  | 💾 | **持久化存档** | 机器人状态自动保存/加载，下次启动继续陪伴 |

  ## ⌨️ 快捷键

  <details open>
  <summary>🌐 通用快捷键（Hub 与所有游戏内）</summary>

  | 快捷键 | 功能 |
  |:------:|------|
  | `Alt + Space` | 🫥 老板键：切换全透明/显示 |
  | `Alt + ↑` / `Alt + ↓` | 👁️ 增加/降低窗口透明度 |
  | `Alt + Q` | 🏠 从游戏返回 Hub 主界面（保留进度） |
  | `Alt + B` | 🌐 打开/关闭内置浏览器 |

  </details>

  <details>
  <summary>🏆 星核防线专用</summary>

  | 快捷键 | 功能 |
  |:------:|------|
  | 🖱️ 左键 | 选择单位、点击控制面板升级/招募 |
  | 🖱️ 右键 | 命令选中单位强制攻击目标点 |
  | `Ctrl + R` | 🔄 重置当前游戏进度 |

  </details>

  <details>
  <summary>🐜 像素电子宠专用</summary>

  | 快捷键 | 功能 |
  |:------:|------|
  | `Ctrl + N` | ➕ 添加新机器人 |
  | `Ctrl + M` | 👾 开启/关闭怪物放置模式 |
  | 🖱️ 左键 | 选择机器人 / 放置怪物 |
  | 🖱️ 右键 | 选中机器人后发射子弹到目标位置 |
  | `ESC` | ❌ 取消怪物放置模式 |

  </details>

  ## 📂 项目结构

  <details>
  <summary>📁 点击展开完整目录树</summary>

  ```
  PureBattleGame/
  ├── 🏠 Core/                          # 核心系统
  │   ├── Program.cs                     # 🚀 应用入口 + 全局异常捕获
  │   ├── MoyuLauncher.cs                # 🎛️ Hub 主界面（卡片导航、热键、托盘）
  │   ├── BrowserForm.cs                 # 🌐 WebView2 内置浏览器
  │   └── SettingsManager.cs             # ⚙️ 配置持久化管理
  ├── 🎮 Games/                          # 游戏模块库
  │   ├── 🏆 StarCoreDefense/            # 星核防线
  │   │   ├── BattleForm.cs         # 战斗界面核心（3824 行）
  │   │   ├── Robot.cs              # 兵种实体（9 大兵种 + 3 级兵阶）
  │   │   ├── Monster.cs            # 怪物实体
  │   │   ├── Projectile.cs         # 弹道系统
  │   │   ├── Mineral.cs            # 矿物资源
  │   │   ├── WallSegment.cs        # 防御墙体
  │   │   ├── ObjectPool.cs         # 对象池（GC 优化）
  │   │   ├── DirtyRectManager.cs   # 脏矩形渲染优化
  │   │   ├── Particle.cs           # 粒子特效
  │   │   ├── FloatingText.cs       # 浮动伤害文字
  │   │   ├── AudioManager.cs       # 音频管理
  │   │   ├── MonsterRenderer.cs    # 怪物渲染器
  │   │   └── *.mp3                 # 背景音乐
  │   └── CockroachPet/             # 像素电子宠
  │       ├── Core/                 # 核心逻辑
  │       │   ├── EmbeddedTerminal.cs    # 嵌入式终端
  │       │   ├── ConPtyTerminal.cs      # Windows ConPTY 终端
  │       │   └── SelfImprovingManager.cs # 自我进化管理
  │       ├── Models/               # 数据模型
  │       │   ├── Robot.cs               # 机器人主体
  │       │   ├── Robot.AI.cs            # AI 决策
  │       │   ├── Robot.Animation.cs     # 动画系统
  │       │   ├── Robot.Combat.cs        # 战斗系统
  │       │   ├── Robot.Emotion.cs       # 情绪系统
  │       │   ├── Robot.Personality.cs   # 性格系统
  │       │   ├── Robot.Physics.cs       # 物理系统
  │       │   ├── Robot.Skills.cs        # 技能系统
  │       │   ├── Robot.Social.cs        # 社交系统
  │       │   ├── Monster.cs             # 怪物实体
  │       │   ├── Projectile.cs         # 弹道实体
  │       │   └── Skill.cs              # 技能定义
  │       ├── Services/             # 服务层
  │       │   ├── AiService.cs           # LLM API 对接
  │       │   ├── AudioManager.cs        # 音频服务
  │       │   ├── PersistenceManager.cs  # 存档管理
  │       │   └── SkillManager.cs        # 技能管理
  │       ├── Rendering/            # 渲染层
  │       │   ├── PixelRobotRenderer.cs  # 像素机器人渲染器
  │       │   └── MonsterRenderer.cs     # 怪物渲染器
  │       └── UI/                   # 界面层
  │           ├── PetForm.cs             # 宠物主界面
  │           ├── ControlPanelForm.cs    # 控制面板
  │           ├── SettingsForm.cs         # 设置界面
  │           └── TerminalManagerForm.cs  # 终端管理
  ├── PureBattleGame.csproj         # 项目配置
  ├── build.bat                     # 一键构建脚本
  └── .gitignore
  ```

  ## 🛠️ 技术栈

  <table>
  <tr>
  <td align="center">

  ![.NET](https://img.shields.io/badge/.NET_8.0-512BD4?style=for-the-badge&logo=dotnet)

  </td>
  <td align="center">

  ![WinForms](https://img.shields.io/badge/WinForms-0078D4?style=for-the-badge&logo=windows)

  </td>
  <td align="center">

  ![WebView2](https://img.shields.io/badge/WebView2-0078D4?style=for-the-badge&logo=microsoftedge)

  </td>
  <td align="center">

  ![C#](https://img.shields.io/badge/C_Sharp-68217A?style=for-the-badge&logo=csharp)

  </td>
  </tr>
  </table>

  | 层级 | 技术 | 说明 |
  |:----:|------|------|
  | 🏗️ **框架** | .NET 8.0 WinForms + WPF | 高性能桌面应用框架 |
  | 🎨 **渲染** | GDI+ 双缓冲 | 像素级绘制，丝滑无闪烁 |
  | 🌐 **Web** | Microsoft WebView2 | Chromium 内核嵌入式浏览器 |
  | 🧠 **AI** | SiliconFlow API | Qwen3-Omni-30B 大语言模型 |
  | 💾 **序列化** | System.Text.Json | 轻量高性能 JSON 处理 |
  | 🏛️ **架构** | Entity-Priority + 对象池 + 脏矩形 | 高性能组件化游戏引擎 |

  <details>
  <summary>⚡ 性能优化细节</summary>

  - ♻️ **对象池**（`ObjectPool`）：复用实体对象，减少 GC 压力
  - 📐 **脏矩形管理**（`DirtyRectManager`）：仅重绘变化区域
  - 🎨 **GDI 句柄缓存**：Brush/Pen 缓存 + Alpha 步进取整，防止句柄泄漏
  - 🔒 **并发安全**：弹道系统使用锁保护，避免多线程竞争

  </details>

  ## 🚀 快速开始

  ### 📋 环境要求

  | 依赖 | 版本 | 备注 |
  |------|------|------|
  | 🖥️ **操作系统** | Windows 10 1803+ / Win11 | — |
  | ⚙️ **.NET SDK** | [8.0+](https://dotnet.microsoft.com/download/dotnet/8.0) | 必需 |
  | 🌐 **WebView2** | Edge WebView2 Runtime | Win11 内置，Win10 需单独安装 |

  ### 🔨 构建与运行

  **方式一：命令行** 🖥️
  ```bash
  git clone https://github.com/<your-username>/PureBattleGame.git
  cd PureBattleGame
  dotnet restore
  dotnet run
  ```

  **方式二：一键构建** ⚡
  ```bash
  build.bat
  ```

  > 📦 构建产物：`bin\Release\net8.0-windows\PureBattleGame.exe`

  ### 🧠 AI 功能配置（像素电子宠）

  如需启用机器人的 AI 自主思考功能：

  > ⚠️ 未配置 API Key 时，机器人仍可正常战斗与互动，仅 AI 对话功能不可用。

  1. 🎮 运行游戏，进入像素电子宠
  2. ⚙️ 打开设置界面，填入 SiliconFlow API Key
  3. 🔑 申请地址：[https://siliconflow.cn](https://siliconflow.cn)

  ---

## 📄 许可证

> ⚠️ 本项目仅供学习交流使用。请遵守所在单位的规章制度。

<p align="center">
  <sub>Built with ❤️ and ☕ by Pure Battle Hub Team</sub>
</p>

---

<a id="english-documentation"></a>

## 📖 Overview

**Pure Battle Hub** is a modular desktop entertainment suite designed for Windows. It features a core launcher (Hub) decoupled from game modules, supporting unlimited game extensions with office-friendly capabilities — global stealth hotkey, opacity control, system tray residency, and an embedded browser.

> 💡 *TL;DR: Enjoy battles and pet-raising elegantly, right under the boss's nose.*

## ✨ Key Features

<table>
<tr>
<td width="50%">

🧩 **Modular Architecture**

Core launcher (Hub) fully decoupled from game modules; add new games by implementing the module interface. Infinite extensibility 🚀

</td>
<td width="50%">

🕵️ **Office-Friendly**

Boss key instant stealth · Seamless opacity switching · System tray residency · Embedded browser · Game state preservation

</td>
</tr>
</table>

<details>
<summary>🔍 Office-Friendly Feature Details</summary>

| Shortcut | Feature | Description |
|:--------:|---------|-------------|
| `Alt + Space` | 🫥 **Boss Key** | Instant transparency + disappear from taskbar |
| `Alt + ↑` / `↓` | 👁️ **Opacity Control** | 10-step seamless opacity switching |
| `Alt + B` | 🌐 **Embedded Browser** | WebView2-powered multi-tab immersive browser |
| `Alt + Q` | 🏠 **Return to Hub** | Return from any game while preserving game state |
| Close button | 📌 **System Tray** | Auto-minimize to system tray |

</details>

<details>
<summary>⚙️ Persistent Settings</summary>

Opacity, browser homepage, etc. auto-saved to `settings.json`, restored on next launch.

```json
{
  "DefaultOpacity": 0.95,
  "HomeUrl": "https://www.xiaoheiv.top",
  "AutoHideInTaskbar": true
}
```

</details>

## 🎮 Built-in Games

### 🏆 Star Core Defense

> ☄️ *A sci-fi idle tower-defense robot battle game — Defend the Star Core, repel the swarm!*

```
  ⚡ Normal ──▶ Mega ──▶ Ultra ⚡
   ┌──────┐    ┌──────┐    ┌──────┐
   │ ▫▫▫▫ │ ──▶│ ████ │ ──▶│ ★★★ │
   └──────┘    └──────┘    └──────┘
```

| | Feature | Description |
|:-:|---------|-------------|
| 🤖 | **9 Unit Classes** | Base · Worker · Healer · Guardian · Engineer · Gunner · Rocket · Plasma · Laser · Lightning |
| 📊 | **3 Rank Tiers** | `Normal` ➜ `Mega` ➜ `Ultra`, with escalating stats |
| 🏠 | **Base Modules** | 🛡️ Bastion (high HP + slow field) / ⚙️ Industrial (fast mining + discount) |
| ☄️ | **Resource Economy** | Mine minerals 💎 → Earn gold 💰 → Upgrade unit stats ⬆️ |
| 👾 | **Diverse Monsters** | Slime 🟢 · Spider 🕷️ · Bat 🦇 · Worm 🪱 — difficulty scales with waves |
| ⚡ | **Charge Blast** | Base charges 6s → AoE shockwave 💥 |
| 🎵 | **Dynamic Audio** | Built-in BGM and combat sound effects 🎶 |
| 🛡️ | **Fortifications** | Engineers build and repair wall segments 🧱 |

### 🐜 CockroachPet

> 🤖 *A desktop pixel robot interactive terminal — Your desktop pet and battle companion!*

```
   ╱╲
  ╱  ╲     "Hey! What shall we chat about today?"
 ╱ ◉  ◉    — Your AI pixel buddy
╱  ──  ╲
╲  ▽  ╱
 ╲──╱
```

| | Feature | Description |
|:-:|---------|-------------|
| 🤖 | **8 Personalities** | 😊Friendly · 😳Shy · 😤Rebel · 😄Humorous · 😐Serious · 🤔Curious · 😴Lazy · 🔥Energetic |
| 😊 | **9 Emotions** | Neutral → Happy → Excited → Sad → Angry → Scared → Curious → Bored → Sleepy |
| ⚔️ | **Auto Combat** | Robots autonomously chase, shoot, and duel 🔫 Multi-weapon: Laser · Rocket · Plasma |
| 👾 | **Monster Spawning** | Manually spawn monsters → All robots focus-fire 🎯 |
| 🧠 | **AI Thinking** | Integrates LLM (SiliconFlow API) for autonomous dialogue and decision-making |
| 💻 | **Embedded Terminal** | Each robot has its own terminal with real-time status output |
| 🎨 | **Pixel Rendering** | GDI+ pixel-level double-buffered drawing, smooth per-frame animation ✨ |
| 💾 | **Persistence** | Robot state auto-saved and loaded, continues on next launch |

## ⌨️ Keyboard Shortcuts

<details open>
<summary>🌐 Global (Hub & All Games)</summary>

| Shortcut | Action |
|:--------:|--------|
| `Alt + Space` | 🫥 Boss Key: toggle full transparency / visible |
| `Alt + ↑` / `Alt + ↓` | 👁️ Increase / decrease window opacity |
| `Alt + Q` | 🏠 Return from game to Hub (preserves progress) |
| `Alt + B` | 🌐 Open / close embedded browser |

</details>

<details>
<summary>🏆 Star Core Defense</summary>

| Shortcut | Action |
|:--------:|--------|
| 🖱️ Left Click | Select unit, click control panel to upgrade/recruit |
| 🖱️ Right Click | Command selected unit to attack target point |
| `Ctrl + R` | 🔄 Reset current game progress |

</details>

<details>
<summary>🐜 CockroachPet</summary>

| Shortcut | Action |
|:--------:|--------|
| `Ctrl + N` | ➕ Add a new robot |
| `Ctrl + M` | 👾 Toggle monster placement mode |
| 🖱️ Left Click | Select robot / place monster |
| 🖱️ Right Click | Selected robot fires bullet to target position |
| `ESC` | ❌ Cancel monster placement mode |

</details>

## 📂 Project Structure

<details>
<summary>📁 Click to expand full directory tree</summary>

```
PureBattleGame/
├── 🏠 Core/                          # Core system
│   ├── Program.cs                     # 🚀 App entry + global exception handler
│   ├── MoyuLauncher.cs                # 🎛️ Hub main UI (card navigation, hotkeys, tray)
│   ├── BrowserForm.cs                 # 🌐 WebView2 embedded browser
│   └── SettingsManager.cs             # ⚙️ Persistent settings manager
├── 🎮 Games/                          # Game modules
│   ├── 🏆 StarCoreDefense/            # Star Core Defense
│   │   ├── BattleForm.cs         # Battle UI core (3824 lines)
│   │   ├── Robot.cs              # Unit entity (9 classes + 3 ranks)
│   │   ├── Monster.cs            # Monster entity
│   │   ├── Projectile.cs         # Projectile system
│   │   ├── Mineral.cs            # Mineral resource
│   │   ├── WallSegment.cs        # Defensive wall
│   │   ├── ObjectPool.cs         # Object pool (GC optimization)
│   │   ├── DirtyRectManager.cs   # Dirty-rect render optimization
│   │   ├── Particle.cs           # Particle effects
│   │   ├── FloatingText.cs       # Floating damage text
│   │   ├── AudioManager.cs       # Audio manager
│   │   ├── MonsterRenderer.cs    # Monster renderer
│   │   └── *.mp3                 # Background music
│   └── 🐜 CockroachPet/               # CockroachPet
│       ├── Core/                 # Core logic
│       │   ├── EmbeddedTerminal.cs    # Embedded terminal
│       │   ├── ConPtyTerminal.cs      # Windows ConPTY terminal
│       │   └── SelfImprovingManager.cs # Self-improvement manager
│       ├── Models/               # Data models
│       │   ├── Robot.cs               # Robot main body
│       │   ├── Robot.AI.cs            # AI decision-making
│       │   ├── Robot.Animation.cs     # Animation system
│       │   ├── Robot.Combat.cs        # Combat system
│       │   ├── Robot.Emotion.cs       # Emotion system
│       │   ├── Robot.Personality.cs   # Personality system
│       │   ├── Robot.Physics.cs       # Physics system
│       │   ├── Robot.Skills.cs        # Skill system
│       │   ├── Robot.Social.cs        # Social system
│       │   ├── Monster.cs             # Monster entity
│       │   ├── Projectile.cs         # Projectile entity
│       │   └── Skill.cs              # Skill definition
│       ├── Services/             # Service layer
│       │   ├── AiService.cs           # LLM API integration
│       │   ├── AudioManager.cs        # Audio service
│       │   ├── PersistenceManager.cs  # Save manager
│       │   └── SkillManager.cs        # Skill manager
│       ├── Rendering/            # Rendering layer
│       │   ├── PixelRobotRenderer.cs  # Pixel robot renderer
│       │   └── MonsterRenderer.cs     # Monster renderer
│       └── UI/                   # UI layer
│           ├── PetForm.cs             # Pet main UI
│           ├── ControlPanelForm.cs    # Control panel
│           ├── SettingsForm.cs         # Settings UI
│           └── TerminalManagerForm.cs  # Terminal manager
├── PureBattleGame.csproj         # Project config
├── build.bat                     # One-click build script
└── .gitignore
```

</details>

## 🛠️ Tech Stack

<table>
<tr>
<td align="center">

![.NET](https://img.shields.io/badge/.NET_8.0-512BD4?style=for-the-badge&logo=dotnet)

</td>
<td align="center">

![WinForms](https://img.shields.io/badge/WinForms-0078D4?style=for-the-badge&logo=windows)

</td>
<td align="center">

![WebView2](https://img.shields.io/badge/WebView2-0078D4?style=for-the-badge&logo=microsoftedge)

</td>
<td align="center">

![C#](https://img.shields.io/badge/C_Sharp-68217A?style=for-the-badge&logo=csharp)

</td>
</tr>
</table>

| Layer | Technology | Description |
|:-----:|------------|-------------|
| 🏗️ **Framework** | .NET 8.0 WinForms + WPF | High-performance desktop app framework |
| 🎨 **Rendering** | GDI+ Double Buffer | Pixel-level drawing, smooth & flicker-free |
| 🌐 **Web** | Microsoft WebView2 | Chromium-core embedded browser |
| 🧠 **AI** | SiliconFlow API | Qwen3-Omni-30B large language model |
| 💾 **Serialization** | System.Text.Json | Lightweight high-performance JSON |
| 🏛️ **Architecture** | Entity-Priority + Object Pool + Dirty Rect | High-performance component game engine |

<details>
<summary>⚡ Performance Optimization Details</summary>

- ♻️ **Object Pool** (`ObjectPool`): Reuse entity objects to reduce GC pressure
- 📐 **Dirty Rect Manager** (`DirtyRectManager`): Only repaint changed regions
- 🎨 **GDI Handle Cache**: Brush/Pen caching + Alpha step quantization to prevent handle leaks
- 🔒 **Concurrency Safety**: Projectile system uses locks to avoid thread races

</details>

## 🚀 Getting Started

### 📋 Prerequisites

| Dependency | Version | Notes |
|------------|---------|-------|
| 🖥️ **OS** | Windows 10 1803+ / Win11 | — |
| ⚙️ **.NET SDK** | [8.0+](https://dotnet.microsoft.com/download/dotnet/8.0) | Required |
| 🌐 **WebView2** | Edge WebView2 Runtime | Built into Win11; separate install for Win10 |

### 🔨 Build & Run

**Option A: Command Line** 🖥️
```bash
git clone https://github.com/<your-username>/PureBattleGame.git
cd PureBattleGame
dotnet restore
dotnet run
```

**Option B: One-click Build** ⚡
```bash
build.bat
```

> 📦 Build output: `bin\Release\net8.0-windows\PureBattleGame.exe`

### 🧠 AI Feature Setup (CockroachPet)

To enable AI autonomous thinking for robots:

> ⚠️ Without an API key, robots still function normally for combat and interaction — only AI dialogue is unavailable.

1. 🎮 Launch the game and enter CockroachPet
2. ⚙️ Open Settings and enter your SiliconFlow API Key
3. 🔑 Get an API key at: [https://siliconflow.cn](https://siliconflow.cn)

---

## 📄 License

> ⚠️ This project is for educational and personal use only. Please comply with your organization's policies.

<p align="center">
  <sub>Built with ❤️ and ☕ by Pure Battle Hub Team</sub>
</p>
