```
 ____            _        ____        _     _              _   _
|  _ \ ___  _ __| |_     | __ )  ___ | | __| | ___ _ __   | | | |_   _
| |_) / _ \| '__| __|    |  _ \ / _ \| |/ _` |/ _ \ '_ \  | |_| | | | |
|  __/ (_) | |  | |_     | |_) | (_) | | (_| |  __/ | | | |  _  | |_| |
|_|   \___/|_|   \__|    |____/ \___/|_|\__,_|\___|_| |_| |_| |_|\__, |
                                                                  |___/
```

# 🎮 Pure Battle Hub (纯战枢纽)

> 🕹️ *模块化桌面娱乐枢纽 — 60 FPS 像素桌宠、AI 生成角色、星核防线与摸鱼助手*

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React 18](https://img.shields.io/badge/WebUI-React_18-61DAFB?logo=react)](https://reactjs.org/)
[![WinForms](https://img.shields.io/badge/UI-WinForms-0078D4?logo=windows)](https://learn.microsoft.com/dotnet/desktop/winforms/)
[![WebView2](https://img.shields.io/badge/Browser-WebView2-0078D4?logo=microsoftedge)](https://developer.microsoft.com/microsoft-edge/webview2/)
[![AI Powered](https://img.shields.io/badge/AI-SiliconFlow-FF6B6B?logo=openai)](https://siliconflow.cn)

**[🇨🇳 中文文档](#中文文档)**  ·  **[🇺🇸 English](#english-documentation)**

---

<a id="中文文档"></a>

## 📖 项目简介

**Pure Battle Hub（纯战枢纽）** 是一款模块化桌面娱乐与摸鱼助手，专为 Windows 桌面环境设计。

采用 **C# .NET 8 + WinForms + React 18 (WebView2)** 混合架构。不仅内置了高频挂机塔防《星核防线》，更拥有支持 **AI 智能角色拆解**、**绿幕生图自动抠图**、**60 FPS 高帧率像素分层渲染**的桌面电子宠物《CockroachPet》。

> 💡 *一句话概括：在老板眼皮底下，优雅地拥有属于你的桌面像素世界与 AI 伙伴。*

---

## ✨ 核心特性

<table>
<tr>
<td width="50%">

🤖 **AI 自然语言角色生成**

输入任意指令（如“生成五个三国武将”），LLM 自动拆解设定，并结合 **Kwai-Kolors 生图 + 绿幕自动抠图** 生成专属精灵贴纸！

</td>
<td width="50%">

⚡ **高精度 60 FPS 物理与渲染**

使用高精度内核定时器（`Stopwatch`）与物理渲染解耦架构，实现无抖动、极低 GC 内存开销的丝滑流畅。

</td>
</tr>
<tr>
<td width="50%">

⚔️ **角色专属技能池（最多5种）**

告别全量全技能混战！AI 根据角色特征（如赛罗配光束/爆发，关羽配格斗/重炮）分配最多 5 种专属技能，战斗节奏平缓治愈。

</td>
<td width="50%">

🕵️ **办公友好与隐身机制**

老板键一键隐身 · 10 档透明度滑动切换 · 系统托盘驻留 · 内置多标签 WebView2 摸鱼浏览器。

</td>
</tr>
</table>

---

## 🎮 内置游戏与模块

### 🐜 像素电子宠 · CockroachPet (v2.1 最新升级)

> 🤖 *桌面像素机器人互动终端 — 你的桌面宠物与战斗伙伴！*

```
  ╱╲
 ╱  ╲     " believe in the light! 相信光的力量！"
╱ ◉  ◉    — 赛罗奥特曼 [专属技能: BEAM, LASER, NOVA, KICK, SLAM]
╱  ──  ╲
╲  ▽  ╱
 ╲──╱
```

| | 模块特性 | 说明 |
|:-:|------|------|
| 🧠 | **LLM 自然语言生成** | 接入 SiliconFlow 大模型 API，输入“加入赛罗”或“生成 10 个奥特曼”，AI 自动生成性格、口头禅与色值 |
| 🎨 | **绿幕生图与自动抠图** | 同步调用 Kolors 生图模型生成角色纯绿幕像素图，内置图像算法秒级自动提取透明 PNG 贴纸 |
| ⚡ | **角色专属 5 种技能池** | 每个机器人受限于最多 5 种契合性格的专属技能，告别全技能混乱狂轰，战斗节奏轻松自然 |
| 📐 | **自适应 UI 等比例缩放** | 悬浮文字、名字标签、血条、表情与伤害飘字根据机器人 `Size` 主体尺寸 `scale` 智能自适应缩放 |
| 💬 | **透亮悬浮对话** | 战吼与吐槽文字取消了臃肿背景框，采用纯粹黑影透明悬浮显示，清爽透亮 |
| 💻 | **React 18 现代化 WebUI** | 集成终端管理、私聊控制面板、AI 发生器与状态监控 modal |
| 💾 | **持久化存档** | 机器人状态、专属技能与抠图精灵路径自动保存/加载，下次启动自动恢复陪伴 |

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
| 🏠 | **基地模式** | 🛡️ 堡垒模式（高血量 + 减速场）/ ⚙️ 工业模式（极速采集 + 购买折扣） |
| ☄️ | **资源经营** | 采集矿物 💎 → 赚取资金 💰 → 升级兵种属性 ⬆️ |
| 👾 | **多样怪物** | 史莱姆 🟢 · 蜘蛛 🕷️ · 蝙蝠 🦇 · 蠕虫 🪱 — 随波次递增难度 |

---

## ⌨️ 快捷键

<details open>
<summary>🌐 通用快捷键（Hub 与所有游戏内）</summary>

| 快捷键 | 功能 | 说明 |
|:------:|------|------|
| `Alt + Space` | 🫥 **老板键** | 切换全透明/隐藏，任务栏同步隐藏 |
| `Alt + ↑` / `↓` | 👁️ **透明度调节** | 10 档透明度无缝切换 |
| `Alt + Q` | 🏠 **返回 Hub** | 保留当前游戏进度，返回主导航 |
| `Alt + B` | 🌐 **内置浏览器** | 唤起/关闭多标签 WebView2 浏览器 |
| 关闭按钮 | 📌 **托盘驻留** | 自动最小化至系统托盘 |

</details>

<details>
<summary>🐜 像素电子宠快捷键</summary>

| 快捷键 | 功能 |
|:------:|------|
| `Ctrl + Shift + P` | ⏸️ 暂停/继续所有物理逻辑 |
| `Ctrl + Shift + T` | 🖱️ 开启/关闭桌面点击穿透 |
| `Ctrl + Shift + M` | 📋 打开系统快捷托盘菜单 |
| `Ctrl + Shift + H` | 🙈 摸鱼伪装模式（代码编辑器/Excel 伪装界面） |
| `Ctrl + Shift + D` | ⚡ 开关高精度 60 FPS 性能诊断看板 |

</details>

---

## 📂 项目结构

```
PureBattleGame/
├── 🏠 Core/                          # 核心系统
│   ├── Program.cs                     # 🚀 应用入口 + 全局异常捕获
│   ├── MoyuLauncher.cs                # 🎛️ Hub 主界面（卡片导航、热键、托盘）
│   ├── BrowserForm.cs                 # 🌐 WebView2 内置浏览器
│   └── SettingsManager.cs             # ⚙️ 配置持久化管理
├── 💻 WebUI/                         # React 18 + Vite 前端模块
│   ├── src/
│   │   ├── components/                # 现代化 React UI 组件 (SocialHub, AiGeneratorModal等)
│   │   └── utils/bridge.ts            # C# 与 Web 双向通信 Bridge
│   └── dist/                          # 构建产物
├── 🎮 Games/                          # 游戏模块库
│   ├── 🏆 StarCoreDefense/            # 星核防线
│   └── 🐜 CockroachPet/               # 像素电子宠
│       ├── Models/                    # 数据模型 (Robot.cs, Robot.Combat.cs, Robot.Skills.cs等)
│       ├── Services/                  # 服务层 (AiService.cs, SiliconFlowImageService.cs)
│       ├── Rendering/                 # 渲染层 (PixelRobotRenderer.cs)
│       └── UI/                        # 界面层 (PetForm.cs, TerminalManagerForm.cs)
├── PureBattleGame.csproj             # 项目配置 (v2.1)
├── setup_builder.iss                 # Inno Setup 6 打包脚本
└── build.bat                         # 一键构建脚本
```

---

## 🛠️ 技术栈

| 层级 | 技术 | 说明 |
|:----:|------|------|
| 🏗️ **核心框架** | .NET 8.0 WinForms | 高性能 Windows 原生基础 |
| 💻 **前端 UI** | React 18 + Vite + TailwindCSS | 现代桌面卡片交互界面 |
| 🎨 **渲染引擎** | GDI+ DIB Section + 独立 60 FPS 线程 | 物理/渲染解耦，丝滑零闪烁 |
| 🧠 **LLM API** | SiliconFlow (Qwen3 / Kolors) | 文本拆解 + Kwai-Kolors 绿幕生图与自动抠图 |
| 🌐 **浏览器** | Microsoft WebView2 | Chromium 内核双向通信嵌入 |

---

## 🚀 快速开始与安装

### 📦 安装包直接运行 (推荐)
直接下载并运行 [PureBattleGame_Setup_v2.1.exe](file:///E:/PureBattleGame/PureBattleGame_Setup_v2.1.exe) 一键完成安装。

### 🔨 源码编译
```bash
# 1. 克隆仓库
git clone https://github.com/3361409208a-source/PureBattleGame.git
cd PureBattleGame

# 2. 构建前端
cd WebUI
npm install
npm run build
cd ..

# 3. 发布与运行 .NET
dotnet publish PureBattleGame.csproj -c Release -r win-x64 --self-contained true
dotnet run
```

---

## 📄 许可证

> ⚠️ 本项目仅供学习交流与个人摸鱼娱乐使用。请遵守所在单位规章制度。

<p align="center">
  <sub>Built with ❤️ and ☕ by Pure Battle Hub Team (v2.1)</sub>
</p>

---

<a id="english-documentation"></a>

## 📖 English Documentation

### Overview
**Pure Battle Hub (v2.1)** is a modular Windows desktop entertainment suite combining a sci-fi tower defense game (*Star Core Defense*) and a 60 FPS desktop pixel pet (*CockroachPet*) featuring **AI character decomposition**, **Kolors green-screen auto-matting**, and **role-appropriate 5-skill limit pools**.

### Key Highlights
- 🤖 **AI Natural Language Generation**: Enter instructions like "spawn 5 Three-Kingdom generals", and the LLM will break down names, personalities, barks, and color codes automatically.
- 🎨 **Green-Screen Matting**: Automatically calls Kwai-Kolors text-to-image API to generate green-screen character sprites and extracts PNG cutouts on the fly.
- ⚡ **Role-Appropriate 5-Skill Limit**: Each robot is restricted to up to 5 curated skills matching its persona (e.g., Zero → Beam/Laser/Nova/Kick/Slam).
- 📐 **Dynamic UI Scaling**: Floating text, name tags, health bars, and damage numbers dynamically scale with the robot's `Size`.
- 🕵️ **Office-Friendly**: One-key boss stealth (`Alt + Space`), 10-step opacity control, system tray minimization, and an embedded WebView2 browser.
