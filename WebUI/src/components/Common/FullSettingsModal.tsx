import React, { useState, useEffect } from 'react';
import {
  X, Sliders, Monitor, Bot, Zap, Volume2, Key, Check, AlertCircle, RefreshCw, Shield
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
            系统与环境
          </button>
          <button
            onClick={() => setActiveTab('social')}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-t-xl border-b-2 transition ${
              activeTab === 'social'
                ? 'border-emerald-500 text-emerald-400 bg-zinc-900 font-bold'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Bot className="w-4 h-4" />
            语言与 AI 互动
          </button>
          <button
            onClick={() => setActiveTab('physics')}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-t-xl border-b-2 transition ${
              activeTab === 'physics'
                ? 'border-emerald-500 text-emerald-400 bg-zinc-900 font-bold'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Zap className="w-4 h-4" />
            属性与物理控制
          </button>
          <button
            onClick={() => setActiveTab('weapons')}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-t-xl border-b-2 transition ${
              activeTab === 'weapons'
                ? 'border-emerald-500 text-emerald-400 bg-zinc-900 font-bold'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Shield className="w-4 h-4" />
            武器管理系统
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

              {/* API Key 配置 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <div className="flex items-center justify-between">
                  <label className="font-semibold text-zinc-200 flex items-center gap-1.5">
                    <Key className="w-3.5 h-3.5 text-amber-400" />
                    SiliconFlow 大模型 API Key
                  </label>
                  {form.apiKey ? (
                    <span className="text-[10px] text-emerald-400 flex items-center gap-1 font-mono">
                      <Check className="w-3 h-3" /> 已配置在线 AI 引擎
                    </span>
                  ) : (
                    <span className="text-[10px] text-amber-400 flex items-center gap-1 font-mono">
                      <AlertCircle className="w-3 h-3" /> 离线预置词库模式
                    </span>
                  )}
                </div>
                <input
                  type="password"
                  value={form.apiKey}
                  onChange={e => setForm({ ...form, apiKey: e.target.value })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 font-mono focus:border-emerald-500 focus:outline-none"
                  placeholder="请输入 SiliconFlow API Key..."
                />
              </div>
            </div>
          )}

          {activeTab === 'social' && (
            <div className="space-y-4">
              {/* 隐藏名称和性格 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl flex items-center justify-between">
                <div>
                  <div className="font-semibold text-zinc-200">隐藏名称和性格卡片</div>
                  <div className="text-zinc-400 text-[11px] mt-0.5">隐藏桌面机器人头顶和浮动气泡的展示名</div>
                </div>
                <input
                  type="checkbox"
                  checked={form.hideNameAndPersonality}
                  onChange={e => setForm({ ...form, hideNameAndPersonality: e.target.checked })}
                  className="w-4 h-4 accent-emerald-500 rounded cursor-pointer"
                />
              </div>

              {/* 默认对骂模式 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl flex items-center justify-between">
                <div>
                  <div className="font-semibold text-zinc-200">默认对骂互动模式</div>
                  <div className="text-zinc-400 text-[11px] mt-0.5">机器人遇敌交战时默认开启对骂喷话</div>
                </div>
                <input
                  type="checkbox"
                  checked={form.curseModeByDefault}
                  onChange={e => setForm({ ...form, curseModeByDefault: e.target.checked })}
                  className="w-4 h-4 accent-emerald-500 rounded cursor-pointer"
                />
              </div>

              {/* 语言互动模式 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <label className="block font-semibold text-zinc-200">语言互动模式 (Language Mode)</label>
                <select
                  value={form.languageMode}
                  onChange={e => setForm({ ...form, languageMode: e.target.value })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 focus:border-emerald-500 focus:outline-none cursor-pointer"
                >
                  <option value="关闭 / 静音">🚫 关闭语言交互 (静音纯动作对抗)</option>
                  <option value="互骂吐槽">🗣️ 互骂吐槽 (互相攻讦金句)</option>
                  <option value="赞美吹捧">🌸 赞美吹捧 (极力夸奖吹捧)</option>
                  <option value="哲理探讨">🌌 哲理探讨 (探寻宇宙与生活哲学)</option>
                  <option value="幽默冷笑话">🤡 幽默冷笑话 (讲无厘头冷笑话)</option>
                  <option value="静音默契">🤫 静音默契 (安静互动不喊话)</option>
                </select>
              </div>

              {/* 动作互动模式 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <label className="block font-semibold text-zinc-200">动作互动模式 (Action Mode)</label>
                <select
                  value={form.actionMode}
                  onChange={e => setForm({ ...form, actionMode: e.target.value })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 focus:border-emerald-500 focus:outline-none cursor-pointer"
                >
                  <option value="近远交替">⚔️ 近远交替 (远程技能与肉搏结合)</option>
                  <option value="全程近战拉扯">🤺 全程近战拉扯 (高频近身追逐肉搏)</option>
                  <option value="全程远程对射">💥 全程远程对射 (保持距离发招对射)</option>
                  <option value="和平相处">🕊️ 和平相处 (友好走动，绝不下死手)</option>
                </select>
              </div>

              {/* AI 动态思考台词 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-3">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="font-semibold text-zinc-200">开启 AI 动态台词思考</div>
                    <div className="text-zinc-400 text-[11px] mt-0.5">空闲时自动生成并广播独特台词</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={form.enableAiThinking}
                    onChange={e => setForm({ ...form, enableAiThinking: e.target.checked })}
                    className="w-4 h-4 accent-emerald-500 rounded cursor-pointer"
                  />
                </div>

                {form.enableAiThinking && (
                  <div className="pt-2 border-t border-zinc-800/80 flex items-center justify-between">
                    <span className="text-zinc-300">思考间隔频率 (秒)</span>
                    <input
                      type="number"
                      min="10"
                      max="300"
                      value={form.aiThoughtFrequency}
                      onChange={e => setForm({ ...form, aiThoughtFrequency: parseInt(e.target.value) || 60 })}
                      className="w-24 bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1 text-right text-zinc-100 font-mono"
                    />
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'physics' && (
            <div className="space-y-4">
              {/* 机器人默认尺寸与移动速度 */}
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                  <span className="font-semibold text-zinc-200">机器人默认尺寸 (px)</span>
                  <input
                    type="number"
                    min="16"
                    max="128"
                    value={form.robotSize}
                    onChange={e => setForm({ ...form, robotSize: parseInt(e.target.value) || 64 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 font-mono"
                  />
                </div>

                <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                  <span className="font-semibold text-zinc-200">移动速度比例 (%)</span>
                  <input
                    type="number"
                    min="50"
                    max="300"
                    value={form.robotSpeed}
                    onChange={e => setForm({ ...form, robotSpeed: parseInt(e.target.value) || 100 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 font-mono"
                  />
                </div>
              </div>

              {/* 音效音量 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <div className="flex justify-between items-center text-zinc-200">
                  <span className="font-semibold flex items-center gap-1.5">
                    <Volume2 className="w-4 h-4 text-emerald-400" />
                    全局音效音量 (Sound Volume)
                  </span>
                  <span className="font-mono text-emerald-400 font-bold">{form.soundVolume}%</span>
                </div>
                <input
                  type="range"
                  min="0"
                  max="100"
                  value={form.soundVolume}
                  onChange={e => setForm({ ...form, soundVolume: parseInt(e.target.value) })}
                  className="w-full accent-emerald-500 bg-zinc-800 rounded-lg cursor-pointer"
                />
              </div>

              {/* 技能特效缩放 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <div className="flex justify-between items-center text-zinc-200">
                  <span className="font-semibold">技能威力与特效缩放</span>
                  <span className="font-mono text-emerald-400 font-bold">{form.skillScale}%</span>
                </div>
                <input
                  type="range"
                  min="10"
                  max="200"
                  value={form.skillScale}
                  onChange={e => setForm({ ...form, skillScale: parseInt(e.target.value) })}
                  className="w-full accent-emerald-500 bg-zinc-800 rounded-lg cursor-pointer"
                />
              </div>

            </div>
          )}

          {activeTab === 'weapons' && (
            <div className="space-y-4 animate-in fade-in slide-in-from-right-4 duration-300">
              {/* 武器大师开关 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl flex items-center justify-between">
                <div>
                  <div className="font-semibold text-zinc-200">武器大师模式</div>
                  <div className="text-zinc-400 text-[11px] mt-0.5">随机为生成或现有的机器人装备神级隐藏武器</div>
                </div>
                <input
                  type="checkbox"
                  checked={form.isWeaponMaster}
                  onChange={e => setForm({ ...form, isWeaponMaster: e.target.checked })}
                  className="w-4 h-4 accent-emerald-500 rounded cursor-pointer"
                />
              </div>

              {/* 机器人血量上限设置 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl space-y-2">
                <div className="flex justify-between items-center text-zinc-200">
                  <span className="font-semibold">机器人基础血量上限 (Max HP)</span>
                  <span className="font-mono text-emerald-400 font-bold">{form.robotMaxHp || 1000} HP</span>
                </div>
                <input
                  type="number"
                  min="100"
                  max="50000"
                  step="100"
                  value={form.robotMaxHp || 1000}
                  onChange={e => setForm({ ...form, robotMaxHp: parseInt(e.target.value) || 1000 })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-zinc-100 font-mono"
                />
              </div>

              {/* 无敌模式 */}
              <div className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl flex items-center justify-between">
                <div>
                  <div className="font-semibold text-zinc-200">🛡️ 全局无敌免伤模式 (God Mode)</div>
                  <div className="text-zinc-400 text-[11px] mt-0.5">开启后所有机器人免除扣血与阵亡，护盾恒定满血</div>
                </div>
                <input
                  type="checkbox"
                  checked={form.isGodMode || false}
                  onChange={e => setForm({ ...form, isGodMode: e.target.checked })}
                  className="w-4 h-4 accent-emerald-500 rounded cursor-pointer"
                />
              </div>


              {[
                {
                  title: '✨ 基础能量光束 (Base Energy)',
                  items: [
                    { id: 'LASER', label: '基础激光' },
                    { id: 'SHOCK', label: '电击束' },
                    { id: 'BURST', label: '脉冲连发' },
                    { id: 'WAVE', label: '声波巨浪' },
                    { id: 'BEAM', label: '毁灭光束' },
                    { id: 'PULSE', label: '环形脉冲' },
                    { id: 'NOVA', label: '新星爆破' },
                    { id: 'BLASTER', label: '重型爆能' }
                  ]
                },
                {
                  title: '👑 武器大师兵器 (Weapon Master)',
                  items: [
                    { id: 'BULLET', label: '实体子弹' },
                    { id: 'ROCKET', label: '跟踪导弹' },
                    { id: 'PLASMA', label: '等离子球' },
                    { id: 'CANNON', label: '加农炮弹' },
                    { id: 'LIGHTNING', label: '闪电链' },
                    { id: 'SPIT', label: '毒液吐息' },
                    { id: 'INK', label: '致盲墨汁' },
                    { id: 'BOOMERANG', label: '回旋镖' },
                    { id: 'SHURIKEN', label: '手里剑' },
                    { id: 'GRENADE', label: '高爆手雷' },
                    { id: 'FIREBALL', label: '烈焰火球' },
                    { id: 'ICE_SHARD', label: '冰霜尖刺' }
                  ]
                },
                {
                  title: '🤼 物理近战招式 (Physical Melee)',
                  items: [
                    { id: 'PUSH', label: '普通推搡' },
                    { id: 'PULL', label: '引力拉扯' },
                    { id: 'GRAB', label: '暴力擒拿' },
                    { id: 'THROW', label: '隔空投掷' },
                    { id: 'DUEL', label: '贴身决斗' },
                    { id: 'SLAM', label: '泰山压顶' },
                    { id: 'KICK', label: '像素飞踢' },
                    { id: 'SUPLEX', label: '过肩背摔' },
                    { id: 'HEADBUTT', label: '无情头槌' },
                    { id: 'TORNADO', label: '旋风斩' }
                  ]
                }
              ].map((category, idx) => (
                <div key={idx} className="bg-zinc-900/60 p-4 border border-zinc-800/80 rounded-xl">
                  <div className="font-semibold text-zinc-200 mb-3 pb-2 border-b border-zinc-800/50">{category.title}</div>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                    {category.items.map(weapon => (
                      <label key={weapon.id} className="flex items-center gap-2 cursor-pointer group">
                        <input
                          type="checkbox"
                          checked={form.enabledWeapons?.includes(weapon.id) ?? true}
                          onChange={e => {
                            const checked = e.target.checked;
                            const current = form.enabledWeapons || [];
                            if (checked) {
                              if (!current.includes(weapon.id)) setForm({ ...form, enabledWeapons: [...current, weapon.id] });
                            } else {
                              setForm({ ...form, enabledWeapons: current.filter(w => w !== weapon.id) });
                            }
                          }}
                          className="w-4 h-4 accent-emerald-500 rounded border-zinc-700 bg-zinc-900"
                        />
                        <span className="text-sm text-zinc-400 group-hover:text-zinc-200 transition">
                          {weapon.label} <span className="text-[10px] opacity-50 font-mono">({weapon.id})</span>
                        </span>
                      </label>
                    ))}
                  </div>
                </div>
              ))}
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
