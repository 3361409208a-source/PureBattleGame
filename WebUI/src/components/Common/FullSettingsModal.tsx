import React, { useState, useEffect } from 'react';
import {
  X, Sliders, Monitor, Check, RefreshCw
} from 'lucide-react';
import { bridge } from '../../utils/bridge';

interface FullSettingsData {
  opacity: number;
  homeUrl: string;
  autoStart: boolean;
  hideNameAndPersonality: boolean;
  curseModeByDefault: boolean;
  battleMode: string;
  languageMode: string;
  actionMode: string;
  robotSize: number;
  robotSpeed: number;
  skillScale: number;
  soundVolume: number;
  fightFrequency: number;
  enableAiThinking: boolean;
  aiThoughtFrequency: number;
  isWeaponMaster: boolean;
  robotMaxHp: number;
  isGodMode: boolean;
  enabledWeapons: string[];
  apiKey: string;
}

interface Props {
  isOpen: boolean;
  onClose: () => void;
}

export const FullSettingsModal: React.FC<Props> = ({ isOpen, onClose }) => {
  const [activeTab, setActiveTab] = useState<'system' | 'social' | 'physics' | 'weapons'>('system');
  const [loading, setLoading] = useState(false);
  const [savedSuccess, setSavedSuccess] = useState(false);

  const [form, setForm] = useState<FullSettingsData>({
    opacity: 0.95,
    homeUrl: 'https://www.xiaoheiv.top',
    autoStart: false,
    hideNameAndPersonality: false,
    curseModeByDefault: true,
    battleMode: '近远交替',
    languageMode: '互骂吐槽',
    actionMode: '近远交替',
    robotSize: 64,
    robotSpeed: 100,
    skillScale: 100,
    soundVolume: 50,
    fightFrequency: 15,
    enableAiThinking: false,
    aiThoughtFrequency: 60,
    isWeaponMaster: false,
    robotMaxHp: 1000,
    isGodMode: false,
    enabledWeapons: [],
    apiKey: '',
  });

  useEffect(() => {
    if (isOpen) {
      loadSettings();
    }
  }, [isOpen]);

  const loadSettings = async () => {
    try {
      setLoading(true);
      const res = await bridge.invoke<FullSettingsData>('getSettings');
      if (res) {
        setForm(res);
      }
    } catch { } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      setLoading(true);
      await bridge.invoke('saveSettings', form);
      setSavedSuccess(true);
      setTimeout(() => setSavedSuccess(false), 2000);
    } catch { } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4 animate-in fade-in duration-200 select-none">
      <div className="relative w-full max-w-2xl bg-zinc-950 border border-zinc-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* 顶部 Header */}
        <div className="flex items-center justify-between px-6 py-4 bg-zinc-900/90 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-gradient-to-br from-emerald-500 to-teal-700 rounded-xl text-zinc-950 font-bold shadow-md shadow-emerald-500/20">
              <Sliders className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base font-bold text-zinc-100 flex items-center gap-2">
                系统全功能控制台 & 设置中心
              </h2>
              <p className="text-xs text-zinc-400 font-mono">Unified System & Robot Configurations</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 rounded-xl transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Tab 分类导航 */}
        <div className="flex items-center gap-1 px-6 pt-3 bg-zinc-900/40 border-b border-zinc-800 text-xs font-medium">
          <button
            onClick={() => setActiveTab('system')}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-t-xl border-b-2 transition ${
              activeTab === 'system'
                ? 'border-emerald-500 text-emerald-400 bg-zinc-900 font-bold'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Monitor className="w-4 h-4" />
            系统全局设置
          </button>
        </div>

        {/* Tab 内容展示区 */}
        <div className="flex-1 p-6 overflow-y-auto space-y-5 text-xs custom-scrollbar">
          {activeTab === 'system' && (
            <div className="space-y-4">
              {/* 窗口透明度 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <div className="flex justify-between items-center text-zinc-200">
                  <span className="font-semibold">窗口不透明度 (Opacity)</span>
                  <span className="font-mono text-emerald-400 font-bold">
                    {Math.round(form.opacity * 100)}%
                  </span>
                </div>
                <input
                  type="range"
                  min="0.1"
                  max="1.0"
                  step="0.05"
                  value={form.opacity}
                  onChange={e => setForm({ ...form, opacity: parseFloat(e.target.value) })}
                  className="w-full accent-emerald-500 bg-zinc-800 rounded-lg cursor-pointer"
                />
              </div>

              {/* 默认首页 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <label className="block font-semibold text-zinc-200">浏览器默认首页 URL</label>
                <input
                  type="text"
                  value={form.homeUrl}
                  onChange={e => setForm({ ...form, homeUrl: e.target.value })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 font-mono focus:border-emerald-500 focus:outline-none"
                  placeholder="https://..."
                />
              </div>

              {/* 开机自启 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl flex items-center justify-between">
                <div>
                  <div className="font-semibold text-zinc-200">开机自动启动</div>
                  <div className="text-zinc-400 text-[11px] mt-0.5">跟随 Windows 开机自动进入摸鱼主控状态</div>
                </div>
                <input
                  type="checkbox"
                  checked={form.autoStart}
                  onChange={e => setForm({ ...form, autoStart: e.target.checked })}
                  className="w-4 h-4 accent-emerald-500 rounded cursor-pointer"
                />
              </div>
            </div>
          )}


        </div>

        {/* 底部保存按钮 Panel */}
        <div className="px-6 py-4 bg-zinc-900/90 border-t border-zinc-800 flex items-center justify-between">
          <span className="text-[11px] text-zinc-400 font-mono">
            {savedSuccess ? (
              <span className="text-emerald-400 font-bold flex items-center gap-1 animate-pulse">
                <Check className="w-3.5 h-3.5" /> 已成功应用并持久化所有配置！
              </span>
            ) : (
              '点击保存后立即可对全盘机器人与系统生效'
            )}
          </span>

          <div className="flex items-center gap-3">
            <button
              onClick={onClose}
              className="px-4 py-2 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 font-medium rounded-xl transition"
            >
              取消
            </button>
            <button
              onClick={handleSave}
              disabled={loading}
              className="px-5 py-2 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-xl shadow-lg shadow-emerald-600/20 transition flex items-center gap-1.5"
            >
              {loading ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              保存并生效
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
