import React, { useState, useEffect } from 'react';
import {
  Gamepad2, Globe, ShieldAlert, Bot, MessageSquare, Settings, X, Minus, Sparkles,
  Zap, ChevronRight, Sliders, ShieldCheck
} from 'lucide-react';
import { bridge } from '../../utils/bridge';

interface LauncherStats {
  opacity: number;
  homeUrl: string;
  robotCount: number;
  isPetActive: boolean;
  isGameActive: boolean;
  isBrowserActive: boolean;
}

export const LauncherHubView: React.FC = () => {
  const [stats, setStats] = useState<LauncherStats>({
    opacity: 1.0,
    homeUrl: 'https://www.xiaoheiv.top',
    robotCount: 0,
    isPetActive: false,
    isGameActive: false,
    isBrowserActive: false,
  });

  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [opacityVal, setOpacityVal] = useState(10);
  const [homeUrlInput, setHomeUrlInput] = useState('https://www.xiaoheiv.top');

  useEffect(() => {
    fetchStats();
    const unsub = bridge.on('launcherStatsUpdated', (data: LauncherStats) => {
      setStats(data);
      setOpacityVal(Math.round(data.opacity * 10));
      setHomeUrlInput(data.homeUrl);
    });
    return () => unsub();
  }, []);

  const fetchStats = async () => {
    try {
      const res = await bridge.invoke<LauncherStats>('getLauncherStats');
      if (res) {
        setStats(res);
        setOpacityVal(Math.round(res.opacity * 10));
        setHomeUrlInput(res.homeUrl || 'https://www.xiaoheiv.top');
      }
    } catch { }
  };

  const handleOpacityChange = (val: number) => {
    setOpacityVal(val);
    const op = val / 10.0;
    bridge.invoke('setLauncherOpacity', { opacity: op });
  };

  const handleSaveSettings = async () => {
    await bridge.invoke('saveLauncherSettings', {
      homeUrl: homeUrlInput,
      opacity: opacityVal / 10.0,
    });
    setIsSettingsOpen(false);
  };

  const handleDrag = () => {
    bridge.invoke('dragWindow');
  };

  return (
    <div className="flex flex-col h-screen bg-[#0d0e15] text-zinc-100 select-none overflow-hidden font-sans border border-zinc-800/80 rounded-2xl shadow-2xl">
      {/* 顶部自定义标题拖拽栏 */}
      <div
        onMouseDown={handleDrag}
        className="flex items-center justify-between px-4 py-3 bg-zinc-950/90 border-b border-zinc-800/80 cursor-move"
      >
        <div className="flex items-center gap-2.5">
          <div className="p-1.5 bg-gradient-to-br from-emerald-500 to-teal-700 rounded-lg text-zinc-950 shadow-md shadow-emerald-500/20">
            <Zap className="w-4 h-4 font-black" />
          </div>
          <div>
            <h1 className="text-xs font-black tracking-widest text-transparent bg-clip-text bg-gradient-to-r from-emerald-400 via-teal-300 to-cyan-400">
              PURE BATTLE HUB
            </h1>
            <p className="text-[10px] text-zinc-500 font-mono">摸鱼游戏主控导航终端 v2.0</p>
          </div>
        </div>

        {/* 顶部按钮控制区 */}
        <div className="flex items-center gap-1.5" onMouseDown={e => e.stopPropagation()}>
          <button
            onClick={() => setIsSettingsOpen(!isSettingsOpen)}
            className="p-1.5 text-zinc-400 hover:text-emerald-400 hover:bg-zinc-800/80 rounded-lg transition"
            title="系统配置"
          >
            <Settings className="w-4 h-4" />
          </button>
          <button
            onClick={() => bridge.invoke('minimizeLauncher')}
            className="p-1.5 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/80 rounded-lg transition"
            title="最小化至托盘"
          >
            <Minus className="w-4 h-4" />
          </button>
          <button
            onClick={() => bridge.invoke('minimizeLauncher')}
            className="p-1.5 text-zinc-400 hover:text-rose-400 hover:bg-zinc-800/80 rounded-lg transition"
            title="隐藏并托盘运行"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* 主体卡片导航列表 */}
      <div className="flex-1 p-4 space-y-2.5 overflow-y-auto custom-scrollbar">
        {/* 卡片 1：极速游戏导航 */}
        <div
          onClick={() => bridge.invoke('launchGameNav')}
          className="group relative p-3.5 bg-zinc-900/80 hover:bg-zinc-800/90 border border-zinc-800 hover:border-emerald-500/50 rounded-xl transition duration-200 cursor-pointer shadow-lg overflow-hidden flex items-center justify-between"
        >
          <div className="flex items-center gap-3.5 z-10">
            <div className="p-3 bg-emerald-950/80 border border-emerald-600/40 rounded-xl text-emerald-400 group-hover:scale-105 transition">
              <Gamepad2 className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-zinc-100 group-hover:text-emerald-400 transition">
                  🎮 极速游戏导航
                </h2>
                <span className="text-[10px] px-1.5 py-0.5 bg-emerald-950 text-emerald-400 border border-emerald-700/50 rounded-md font-mono">
                  HOT
                </span>
              </div>
              <p className="text-xs text-zinc-400 mt-0.5">精选各类优质在线 H5 摸鱼游戏导航与全网资源</p>
            </div>
          </div>
          <ChevronRight className="w-5 h-5 text-zinc-600 group-hover:text-emerald-400 group-hover:translate-x-1 transition z-10" />
        </div>

        {/* 卡片 2：极速浏览器 */}
        <div
          onClick={() => bridge.invoke('launchBrowser')}
          className="group relative p-3.5 bg-zinc-900/80 hover:bg-zinc-800/90 border border-zinc-800 hover:border-cyan-500/50 rounded-xl transition duration-200 cursor-pointer shadow-lg overflow-hidden flex items-center justify-between"
        >
          <div className="flex items-center gap-3.5 z-10">
            <div className="p-3 bg-cyan-950/80 border border-cyan-600/40 rounded-xl text-cyan-400 group-hover:scale-105 transition">
              <Globe className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-zinc-100 group-hover:text-cyan-400 transition">
                  🌐 极速浏览器
                </h2>
                {stats.isBrowserActive && (
                  <span className="text-[10px] px-1.5 py-0.5 bg-cyan-950 text-cyan-400 border border-cyan-700/50 rounded-md font-mono">
                    运行中
                  </span>
                )}
              </div>
              <p className="text-xs text-zinc-400 mt-0.5">沉浸式办公多标签浏览环境，支持暗色隐蔽模版</p>
            </div>
          </div>
          <ChevronRight className="w-5 h-5 text-zinc-600 group-hover:text-cyan-400 group-hover:translate-x-1 transition z-10" />
        </div>

        {/* 卡片 3：星核防线 */}
        <div
          onClick={() => bridge.invoke('launchStarDefense')}
          className="group relative p-3.5 bg-zinc-900/80 hover:bg-zinc-800/90 border border-zinc-800 hover:border-amber-500/50 rounded-xl transition duration-200 cursor-pointer shadow-lg overflow-hidden flex items-center justify-between"
        >
          <div className="flex items-center gap-3.5 z-10">
            <div className="p-3 bg-amber-950/80 border border-amber-600/40 rounded-xl text-amber-400 group-hover:scale-105 transition">
              <ShieldAlert className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-zinc-100 group-hover:text-amber-400 transition">
                  🏆 星核防线
                </h2>
                {stats.isGameActive && (
                  <span className="text-[10px] px-1.5 py-0.5 bg-amber-950 text-amber-400 border border-amber-700/50 rounded-md font-mono animate-pulse">
                    战场进行中
                  </span>
                )}
              </div>
              <p className="text-xs text-zinc-400 mt-0.5">宇宙级挂机塔防站，防御陨石波次与星核据点</p>
            </div>
          </div>
          <ChevronRight className="w-5 h-5 text-zinc-600 group-hover:text-amber-400 group-hover:translate-x-1 transition z-10" />
        </div>

        {/* 卡片 4：像素电子宠 */}
        <div
          onClick={() => bridge.invoke('launchPixelPet')}
          className="group relative p-3.5 bg-zinc-900/80 hover:bg-zinc-800/90 border border-zinc-800 hover:border-teal-500/50 rounded-xl transition duration-200 cursor-pointer shadow-lg overflow-hidden flex items-center justify-between"
        >
          <div className="flex items-center gap-3.5 z-10">
            <div className="p-3 bg-teal-950/80 border border-teal-600/40 rounded-xl text-teal-400 group-hover:scale-105 transition">
              <Bot className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-zinc-100 group-hover:text-teal-400 transition">
                  🐜 像素电子宠
                </h2>
                <span className="text-[10px] px-1.5 py-0.5 bg-teal-950 text-teal-400 border border-teal-700/50 rounded-md font-mono">
                  {stats.robotCount} 个在线
                </span>
              </div>
              <p className="text-xs text-zinc-400 mt-0.5">桌面像素电子宠物，支持物理碰撞、游走与技能对决</p>
            </div>
          </div>
          <ChevronRight className="w-5 h-5 text-zinc-600 group-hover:text-teal-400 group-hover:translate-x-1 transition z-10" />
        </div>

        {/* 卡片 5：机器人社交中心 & 控制台 */}
        <div
          onClick={() => bridge.invoke('launchSocialHub')}
          className="group relative p-3.5 bg-zinc-900/80 hover:bg-zinc-800/90 border border-zinc-800 hover:border-purple-500/50 rounded-xl transition duration-200 cursor-pointer shadow-lg overflow-hidden flex items-center justify-between"
        >
          <div className="flex items-center gap-3.5 z-10">
            <div className="p-3 bg-purple-950/80 border border-purple-600/40 rounded-xl text-purple-400 group-hover:scale-105 transition">
              <MessageSquare className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-zinc-100 group-hover:text-purple-400 transition">
                  💬 机器人社交中心 & 控制台
                </h2>
                <span className="text-[10px] px-1.5 py-0.5 bg-purple-950 text-purple-400 border border-purple-700/50 rounded-md font-mono">
                  AI 对战
                </span>
              </div>
              <p className="text-xs text-zinc-400 mt-0.5">多机器人语言互动模式、世界聊天频道与属性管控台</p>
            </div>
          </div>
          <ChevronRight className="w-5 h-5 text-zinc-600 group-hover:text-purple-400 group-hover:translate-x-1 transition z-10" />
        </div>
      </div>

      {/* 系统配置 Drawer / Modal */}
      {isSettingsOpen && (
        <div className="p-4 bg-zinc-950 border-t border-zinc-800 animate-in slide-in-from-bottom duration-200 space-y-3">
          <div className="flex items-center justify-between text-xs font-bold text-emerald-400 pb-1 border-b border-zinc-800">
            <span className="flex items-center gap-1.5">
              <Sliders className="w-4 h-4" />
              主控系统偏好设置
            </span>
            <button
              onClick={() => setIsSettingsOpen(false)}
              className="text-zinc-400 hover:text-zinc-100"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="space-y-2.5 text-xs">
            {/* 窗口不透明度 */}
            <div>
              <div className="flex justify-between text-zinc-300 mb-1">
                <span>窗口透明度：</span>
                <span className="font-mono text-emerald-400">{opacityVal * 10}%</span>
              </div>
              <input
                type="range"
                min="1"
                max="10"
                value={opacityVal}
                onChange={e => handleOpacityChange(parseInt(e.target.value))}
                className="w-full accent-emerald-500 bg-zinc-800 rounded-lg cursor-pointer"
              />
            </div>

            {/* 默认首页 */}
            <div>
              <label className="block text-zinc-300 mb-1">浏览器默认首页 URL：</label>
              <input
                type="text"
                value={homeUrlInput}
                onChange={e => setHomeUrlInput(e.target.value)}
                className="w-full bg-zinc-900 border border-zinc-800 rounded-lg px-3 py-1.5 text-zinc-100 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50 font-mono"
              />
            </div>

            <button
              onClick={handleSaveSettings}
              className="w-full py-2 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-lg transition"
            >
              应用并保存配置
            </button>
          </div>
        </div>
      )}

      {/* 底部状态提示栏 */}
      <div className="px-4 py-2.5 bg-zinc-950 border-t border-zinc-800/80 flex items-center justify-between text-[11px] text-zinc-400 font-mono">
        <span className="flex items-center gap-1.5 text-amber-400">
          <ShieldCheck className="w-3.5 h-3.5" />
          快捷键 <kbd className="px-1 py-0.5 bg-zinc-800 text-zinc-200 rounded border border-zinc-700 text-[10px]">Alt + Space</kbd> 瞬间摸鱼隐藏
        </span>
        <span className="text-zinc-500 flex items-center gap-1">
          <Sparkles className="w-3 h-3 text-emerald-400" />
          Vite React WebUI
        </span>
      </div>
    </div>
  );
};
