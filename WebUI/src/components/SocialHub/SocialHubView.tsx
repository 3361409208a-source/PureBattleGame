import React, { useState, useEffect, useRef } from 'react';
import { 
  Bot, MessageSquare, Globe, Zap, Shield, Search, 
  Send, Flame, Cpu, RefreshCw, Sparkles, Terminal, SlidersHorizontal,
  Table, Plus, Pause, Play, Trash2, Eye, EyeOff, Save, Monitor,
  Video, Camera, Palette, Square, CheckCircle, FolderOpen, Info
} from 'lucide-react';
import { bridge } from '../../utils/bridge';
import { AiGeneratorModal } from './AiGeneratorModal';
import { RobotDetailsModal } from './RobotDetailsModal';
import { CombatLogModal } from './CombatLogModal';
import type { RobotInfo, SocialMessage, ChatMessage, SystemStats, AppSettings } from '../../types/bridge';

export const SocialHubView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'overview' | 'world' | 'control' | 'settings' | string>('overview');
  const [robots, setRobots] = useState<RobotInfo[]>([]);
  const [selectedRobotForDetails, setSelectedRobotForDetails] = useState<RobotInfo | null>(null);
  const [isCombatModalOpen, setIsCombatModalOpen] = useState(false);
  const [stats, setStats] = useState<SystemStats>({
    onlineCount: 0,
    totalRobots: 0,
    movingRobots: 0,
    pausedRobots: 0,
    battleMode: '近远交替',
    totalTokens: 0,
    totalCostYuan: 0,
  });

  const [settings, setSettings] = useState<AppSettings>({
    opacity: 0.95,
    hideNameAndPersonality: false,
    curseModeByDefault: true,
    battleMode: '近远交替',
    languageMode: '互骂吐槽',
    actionMode: '近远交替',
    apiKey: '',
    homeUrl: 'https://www.xiaoheiv.top',
  });

  const [worldMessages, setWorldMessages] = useState<SocialMessage[]>([]);
  const [broadcastInput, setBroadcastInput] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [saveStatus, setSaveStatus] = useState('');

  // 单人私聊与 AI 弹窗状态
  const [privateChats, setPrivateChats] = useState<Record<string, ChatMessage[]>>({});
  const [privateInput, setPrivateInput] = useState('');
  const [isAiModalOpen, setIsAiModalOpen] = useState(false);

  // 宣传录像状态
  const [recordingMode, setRecordingMode] = useState<'DESKTOP' | 'CUSTOM_BG'>('CUSTOM_BG');
  const [recordingBgHex, setRecordingBgHex] = useState('#00FF00');
  const [isRecording, setIsRecording] = useState(false);
  const [recordStats, setRecordStats] = useState<{ frameCount: number; durationSeconds: number; folderPath?: string }>({
    frameCount: 0,
    durationSeconds: 0,
  });

  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let timer: any = null;
    if (isRecording) {
      timer = setInterval(async () => {
        try {
          const res = await bridge.invoke<{ isRecording: boolean; frameCount: number; durationSeconds: number; folderPath?: string }>('getRecordingStatus');
          if (res) {
            setRecordStats({
              frameCount: res.frameCount || 0,
              durationSeconds: res.durationSeconds || 0,
              folderPath: res.folderPath
            });
            if (!res.isRecording && isRecording) {
              setIsRecording(false);
            }
          }
        } catch {}
      }, 500);
    }
    return () => { if (timer) clearInterval(timer); };
  }, [isRecording]);

  const handleStartRecording = async () => {
    try {
      const res = await bridge.invoke<{ success: boolean }>('startRecording', {
        mode: recordingMode,
        hexColor: recordingBgHex
      });
      if (res && res.success) {
        setIsRecording(true);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleStopRecording = async () => {
    try {
      const res = await bridge.invoke<{ success: boolean; folderPath?: string; frameCount?: number; durationSeconds?: number }>('stopRecording');
      setIsRecording(false);
      if (res && res.success) {
        setRecordStats(prev => ({
          ...prev,
          folderPath: res.folderPath,
          frameCount: res.frameCount || prev.frameCount,
          durationSeconds: res.durationSeconds || prev.durationSeconds
        }));
      }
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    fetchData();

    const unsubRobots = bridge.on('robotsUpdated', (data: RobotInfo[]) => setRobots(data));
    const unsubStats = bridge.on('statsUpdated', (data: SystemStats) => setStats(data));
    const unsubWorld = bridge.on('worldMessageReceived', (msg: SocialMessage) => {
      setWorldMessages(prev => [...prev.slice(-49), msg]);
    });
    const unsubPrivate = bridge.on('privateMessageReceived', ({ robotId, message }: { robotId: string; message: ChatMessage }) => {
      setPrivateChats(prev => {
        const currentList = prev[robotId] || [];
        const isDuplicate = currentList.length > 0 && 
          currentList[currentList.length - 1].role === message.role && 
          currentList[currentList.length - 1].content === message.content;
          
        if (isDuplicate) return prev;

        return {
          ...prev,
          [robotId]: [...currentList.slice(-29), message]
        };
      });
    });

    const unsubCombatModal = bridge.on('openCombatModal', () => {
      setIsCombatModalOpen(true);
    });

    return () => {
      unsubRobots();
      unsubStats();
      unsubWorld();
      unsubPrivate();
      unsubCombatModal();
    };
  }, []);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [worldMessages, privateChats, activeTab]);

  const fetchData = async () => {
    try {
      const robotList = await bridge.invoke<RobotInfo[]>('getRobots');
      if (robotList) setRobots(robotList);

      const sysStats = await bridge.invoke<SystemStats>('getStats');
      if (sysStats) setStats(sysStats);

      const worldHistory = await bridge.invoke<SocialMessage[]>('getWorldHistory');
      if (worldHistory) setWorldMessages(worldHistory);

      const currentSettings = await bridge.invoke<AppSettings>('getSettings');
      if (currentSettings) setSettings(currentSettings);
    } catch (e) {
      console.error(e);
    }
  };

  const handleBroadcast = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!broadcastInput.trim()) return;

    await bridge.invoke('sendWorldBroadcast', { message: broadcastInput });
    setBroadcastInput('');
  };

  const handleSendPrivate = async (robotId: string, e: React.FormEvent) => {
    e.preventDefault();
    if (!privateInput.trim()) return;

    const userText = privateInput.trim();
    setPrivateInput('');

    const userMsg: ChatMessage = { role: 'user', content: userText };
    setPrivateChats(prev => ({
      ...prev,
      [robotId]: [...(prev[robotId] || []), userMsg]
    }));

    await bridge.invoke('sendPrivateMessage', { robotId, message: userText });
  };

  const handleInspirate = async (robotId?: string) => {
    await bridge.invoke('triggerInspiration', { robotId });
  };

  const handleToggleCurse = async (robotId: string, current: boolean) => {
    await bridge.invoke('toggleCurseMode', { robotId, enable: !current });
    setRobots(prev => prev.map(r => r.id === robotId ? { ...r, curseMode: !current } : r));
  };

  const handleOpenPrivateChat = (robotId: string) => {
    setActiveTab(robotId);
    bridge.invoke<ChatMessage[]>('getPrivateHistory', { robotId }).then(history => {
      if (history && history.length > 0) {
        setPrivateChats(prev => ({ ...prev, [robotId]: history }));
      }
    });
  };

  const handleSaveSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    await bridge.invoke('saveSettings', settings);
    setSaveStatus('✅ 设置保存成功！');
    setTimeout(() => setSaveStatus(''), 3000);
  };

  const filteredRobots = robots.filter(r => 
    r.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
    r.personality.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const activeRobot = robots.find(r => r.id === activeTab);

  return (
    <div className="flex h-screen bg-zinc-950 text-zinc-100 font-sans select-none overflow-hidden">
      {/* 1. 左侧模块化侧边栏 (Categorized Left Sidebar) */}
      <aside className="w-60 bg-zinc-900 border-r border-zinc-800 flex flex-col shrink-0">
        {/* 侧边栏 Logo & 标语 Header */}
        <div className="p-4 border-b border-zinc-800 flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-emerald-600 to-emerald-400 flex items-center justify-center text-zinc-950 font-bold shadow-lg shadow-emerald-500/20 shrink-0">
            <Bot className="w-5 h-5" />
          </div>
          <div className="overflow-hidden">
            <h1 className="text-xs font-bold text-zinc-100 truncate">PURE BATTLE HUB</h1>
            <p className="text-[10px] text-emerald-400 font-mono">机器人控制与社交中心</p>
          </div>
        </div>

        {/* 侧边栏导航分组列表 */}
        <div className="flex-1 overflow-y-auto p-2 space-y-4 font-sans text-xs scrollbar-thin">
          {/* 模块 1: 🤖 机器人与社交 */}
          <div>
            <div className="px-3 py-1 text-[10px] font-bold text-zinc-500 uppercase tracking-wider font-mono">
              🤖 机器人与社交
            </div>
            <div className="space-y-0.5 mt-1">
              <button
                onClick={() => setActiveTab('overview')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'overview'
                    ? 'bg-emerald-950 text-emerald-400 border border-emerald-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <Bot className="w-4 h-4 text-emerald-400" />
                <span>机器人概览 ({robots.length})</span>
              </button>

              <button
                onClick={() => setActiveTab('world')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'world'
                    ? 'bg-amber-950 text-amber-400 border border-amber-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <Globe className="w-4 h-4 text-amber-400" />
                <span>世界广播频道</span>
              </button>
            </div>
          </div>

          {/* 模块 2: 💬 动态单人私聊频道 */}
          {robots.filter(r => privateChats[r.id]).length > 0 && (
            <div>
              <div className="px-3 py-1 text-[10px] font-bold text-zinc-500 uppercase tracking-wider font-mono">
                💬 专属私聊频道
              </div>
              <div className="space-y-0.5 mt-1">
                {robots.filter(r => privateChats[r.id]).map(robot => (
                  <button
                    key={robot.id}
                    onClick={() => setActiveTab(robot.id)}
                    className={`w-full flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-medium transition ${
                      activeTab === robot.id
                        ? 'bg-emerald-950 text-emerald-300 border border-emerald-600/50 font-bold'
                        : 'text-zinc-400 hover:bg-zinc-800/80'
                    }`}
                  >
                    <Terminal className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                    <span className="truncate">💬 {settings.hideNameAndPersonality ? `机器人#${robot.id}` : robot.name}</span>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* 模块 3: 📊 桌宠管控与战斗 */}
          <div>
            <div className="px-3 py-1 text-[10px] font-bold text-zinc-500 uppercase tracking-wider font-mono">
              📊 桌宠管控与战斗
            </div>
            <div className="space-y-0.5 mt-1">
              <button
                onClick={() => setActiveTab('control')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'control'
                    ? 'bg-emerald-950 text-emerald-300 border border-emerald-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <Table className="w-4 h-4 text-emerald-400" />
                <span>实体控制台 (表格)</span>
              </button>

              <button
                onClick={() => setActiveTab('combat')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'combat'
                    ? 'bg-rose-950 text-rose-300 border border-rose-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <Shield className="w-4 h-4 text-rose-400" />
                <span>武器与战斗设置</span>
              </button>
            </div>
          </div>

          {/* 模块 4: ⚙️ AI人设与系统工具 */}
          <div>
            <div className="px-3 py-1 text-[10px] font-bold text-zinc-500 uppercase tracking-wider font-mono">
              ⚙️ AI人设与系统
            </div>
            <div className="space-y-0.5 mt-1">
              <button
                onClick={() => setActiveTab('ai')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'ai'
                    ? 'bg-cyan-950 text-cyan-300 border border-cyan-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <MessageSquare className="w-4 h-4 text-cyan-400" />
                <span>AI人设与语言</span>
              </button>

              <button
                onClick={() => setActiveTab('recorder')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'recorder'
                    ? 'bg-purple-950 text-purple-300 border border-purple-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <Video className="w-4 h-4 text-purple-400" />
                <span>宣传录屏工具</span>
              </button>

              <button
                onClick={() => setActiveTab('settings')}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition ${
                  activeTab === 'settings'
                    ? 'bg-zinc-800 text-amber-400 border border-amber-600/50 shadow-md font-bold'
                    : 'text-zinc-400 hover:bg-zinc-800/80 hover:text-zinc-200'
                }`}
              >
                <SlidersHorizontal className="w-4 h-4 text-amber-400" />
                <span>基础偏好设置</span>
              </button>
            </div>
          </div>
        </div>

        {/* 侧边栏 Footer (一键生图) */}
        <div className="p-3 border-t border-zinc-800 bg-zinc-950/60">
          <button
            onClick={() => setIsAiModalOpen(true)}
            className="w-full py-2 bg-gradient-to-r from-emerald-600 to-emerald-500 hover:from-emerald-500 hover:to-emerald-400 text-zinc-950 font-bold rounded-xl text-xs flex items-center justify-center gap-1.5 shadow-lg transition"
          >
            <Sparkles className="w-4 h-4" />
            AI 生成并投放机器人
          </button>
        </div>
      </aside>

      {/* 2. 右侧主内容全屏区 */}
      <div className="flex-1 flex flex-col overflow-hidden bg-zinc-950">
        {/* 右侧 Header Badges */}
        <header className="px-6 py-2.5 bg-zinc-900 border-b border-zinc-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2 font-mono text-xs text-zinc-400">
            <span className="text-emerald-400 font-bold">PURE BATTLE HUB</span>
            <span>/</span>
            <span className="text-zinc-200 capitalize font-bold">{activeTab}</span>
          </div>

          <div className="flex items-center gap-2 overflow-x-auto">
            <span className="flex items-center gap-1.5 px-2.5 py-0.5 bg-zinc-950 border border-emerald-600/50 rounded-full text-xs font-semibold text-emerald-400 whitespace-nowrap">
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
              在线: {stats.onlineCount}
            </span>
            <span className="flex items-center gap-1 px-2.5 py-0.5 bg-zinc-950 border border-zinc-700 rounded-full text-xs font-semibold text-zinc-300 whitespace-nowrap">
              <Shield className="w-3 h-3 text-emerald-400" />
              动作: {settings.actionMode || settings.battleMode}
            </span>
            <span className="flex items-center gap-1 px-2.5 py-0.5 bg-zinc-950 border border-amber-600/50 rounded-full text-xs font-semibold text-amber-400 whitespace-nowrap">
              <MessageSquare className="w-3 h-3 text-amber-400" />
              语言: {settings.languageMode || '互骂吐槽'}
            </span>
            <span className="flex items-center gap-1 px-2.5 py-0.5 bg-zinc-950 border border-amber-600/50 rounded-full text-xs font-semibold text-amber-400 whitespace-nowrap">
              <Zap className="w-3 h-3 text-amber-400" />
              Token: {stats.totalTokens}
            </span>
            <button
              onClick={() => setIsCombatModalOpen(true)}
              className="px-3 py-1 bg-gradient-to-r from-rose-600 to-rose-500 hover:from-rose-500 hover:to-rose-400 text-white font-bold rounded-lg text-xs flex items-center gap-1 shadow transition shrink-0"
              title="查看实时对战日志与MVP数据榜"
            >
              ⚔️ 战况与伤害榜
            </button>
            <button 
              onClick={fetchData}
              className="p-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 rounded-lg transition shrink-0"
              title="刷新状态"
            >
              <RefreshCw className="w-3.5 h-3.5" />
            </button>
          </div>
        </header>

        {/* 右侧 Main Panel Container */}
        <main className="flex-1 overflow-hidden p-4 relative bg-zinc-950">
        {/* 1. 机器人概览模式 */}
        {activeTab === 'overview' && (
          <div className="flex flex-col h-full gap-3">
            {/* 工具栏 */}
            <div className="flex items-center justify-between gap-3 bg-zinc-900 p-2.5 rounded-xl border border-zinc-800">
              <div className="relative flex-1 max-w-md">
                <Search className="w-3.5 h-3.5 absolute left-3 top-2.5 text-zinc-500" />
                <input
                  type="text"
                  placeholder="搜索机器人..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg pl-9 pr-4 py-1 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50"
                />
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => handleInspirate()}
                  className="flex items-center gap-1.5 px-3 py-1 bg-amber-600 hover:bg-amber-500 text-zinc-950 rounded-lg text-xs font-bold shadow-md transition"
                >
                  <Sparkles className="w-3.5 h-3.5" />
                  ⚡ 全体启发
                </button>
              </div>
            </div>

            {/* 卡片网格 (使用 items-start content-start 紧凑自适应包裹高度，防止无意义垂直拉伸) */}
            <div className="flex-1 overflow-y-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 pr-1 items-start content-start">
              {filteredRobots.map(robot => (
                <div
                  key={robot.id}
                  className="bg-zinc-900 border border-zinc-800 hover:border-emerald-600/50 rounded-xl p-3 flex flex-col transition-all hover:shadow-lg group h-auto"
                >
                  <div>
                    {/* 卡片头部 */}
                    <div className="flex items-center justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <div
                          className="w-8 h-8 rounded-lg flex items-center justify-center font-bold text-zinc-950 text-xs shadow-md overflow-hidden shrink-0 border border-white/10"
                          style={{ backgroundColor: robot.colorHex || '#10B981' }}
                        >
                          {robot.avatarDataUrl ? (
                            <img src={robot.avatarDataUrl} alt={robot.name} className="w-full h-full object-contain p-0.5" />
                          ) : (
                            settings.hideNameAndPersonality ? '🤖' : robot.name.slice(0, 2)
                          )}
                        </div>
                        <div>
                          <div className="flex items-center gap-1.5">
                            <h3 className="font-bold text-xs text-zinc-100">
                              {settings.hideNameAndPersonality ? `机器人 #${robot.id}` : robot.name}
                            </h3>
                            <span className="text-[10px] px-1 py-0.2 bg-zinc-800 text-emerald-400 rounded font-mono border border-zinc-700">
                              LV.{robot.level}
                            </span>
                          </div>
                          {!settings.hideNameAndPersonality && (
                            <p className="text-[11px] text-zinc-400">{robot.personality}</p>
                          )}
                        </div>
                      </div>

                      {/* 思考指示灯 */}
                      {robot.isThinking ? (
                        <span className="flex items-center gap-1 text-[10px] text-amber-400 bg-amber-950/80 px-2 py-0.5 rounded-full border border-amber-700 font-mono">
                          <Cpu className="w-3 h-3 animate-spin" />
                          思考中...
                        </span>
                      ) : (
                        <span className="w-2 h-2 rounded-full bg-emerald-500 shadow-sm shadow-emerald-500" title="活跃在线" />
                      )}
                    </div>

                    {/* 血量条 */}
                    <div className="space-y-1 my-2">
                      <div className="flex justify-between text-[10px] text-zinc-400 font-mono">
                        <span>HP {robot.hp}/{robot.maxHp}</span>
                        <span>{robot.killCount} 击杀 / {robot.deathCount} 阵亡</span>
                      </div>
                      <div className="w-full h-1.5 bg-zinc-950 rounded-full overflow-hidden border border-zinc-800">
                        <div
                          className="h-full bg-emerald-500 transition-all duration-300"
                          style={{ width: `${Math.max(0, Math.min(100, (robot.hp / robot.maxHp) * 100))}%` }}
                        />
                      </div>
                    </div>

                    {/* 当前对话/气泡文本 (纯文本，去除 AI生成 字样) */}
                    {robot.chatText && (
                      <div className="p-2 bg-zinc-950 rounded-lg border border-zinc-800 text-xs text-zinc-300 mb-2 italic">
                        "{robot.chatText.replace(/🤖\s*\[AI生成\]/g, '')}"
                      </div>
                    )}
                  </div>

                  {/* 底部卡片操作 */}
                  <div className="flex items-center gap-1.5 pt-2 border-t border-zinc-800">
                    <button
                      onClick={() => setSelectedRobotForDetails(robot)}
                      className="px-2.5 py-1 bg-zinc-800 hover:bg-zinc-700 text-amber-300 border border-zinc-700 rounded-lg text-xs font-bold transition flex items-center gap-1"
                    >
                      <Info className="w-3 h-3" />
                      详情
                    </button>
                    <button
                      onClick={() => handleOpenPrivateChat(robot.id)}
                      className="flex-1 flex items-center justify-center gap-1 py-1 bg-emerald-950 hover:bg-emerald-900 text-emerald-400 border border-emerald-700/60 rounded-lg text-xs font-semibold transition"
                    >
                      <MessageSquare className="w-3 h-3" />
                      私聊
                    </button>
                    <button
                      onClick={() => handleInspirate(robot.id)}
                      className="px-2 py-1 bg-amber-950 hover:bg-amber-900 text-amber-400 border border-amber-700/60 rounded-lg text-xs font-semibold transition"
                      title="单独启发"
                    >
                      ⚡
                    </button>
                    <button
                      onClick={() => handleToggleCurse(robot.id, robot.curseMode)}
                      className={`px-2 py-1 rounded-lg text-xs font-semibold border transition ${
                        robot.curseMode
                          ? 'bg-rose-950 text-rose-400 border-rose-700'
                          : 'bg-zinc-800 text-zinc-400 border-zinc-700 hover:text-zinc-200'
                      }`}
                      title={robot.curseMode ? '吐槽模式开启' : '点击开启吐槽模式'}
                    >
                      <Flame className="w-3 h-3" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* 2. 世界广播频道 */}
        {activeTab === 'world' && (
          <div className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-3">
            <div className="flex-1 overflow-y-auto space-y-2.5 pr-2 scrollbar-thin">
              {worldMessages.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-zinc-500 text-xs">
                  <Globe className="w-7 h-7 mb-2 opacity-30 text-amber-400" />
                  暂无广播消息，输入下方框开始向所有机器人广播吧！
                </div>
              ) : (
                worldMessages.map((msg, i) => {
                  const getDisplayName = (sender: string) => {
                    if (sender === '管理员' || sender === '系统') return sender;
                    if (!settings.hideNameAndPersonality) return sender;
                    const idx = robots.findIndex(r => r.name === sender);
                    return idx >= 0 ? `机器人 #${idx + 1}` : '机器人';
                  };
                  return (
                    <div key={i} className="flex gap-2.5 text-xs">
                      <span className="font-bold text-amber-400 whitespace-nowrap">
                        [{getDisplayName(msg.sender)}]
                      </span>
                      <span className="text-zinc-200 leading-relaxed">
                        {msg.content.replace(/🤖\s*\[AI生成\]/g, '')}
                      </span>
                    </div>
                  );
                })
              )}
              <div ref={chatEndRef} />
            </div>

            <form onSubmit={handleBroadcast} className="mt-2 flex items-center gap-2 pt-2 border-t border-zinc-800">
              <input
                type="text"
                placeholder="发送全局广播消息..."
                value={broadcastInput}
                onChange={e => setBroadcastInput(e.target.value)}
                className="flex-1 bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-amber-500/50"
              />
              <button
                type="submit"
                className="px-3.5 py-1.5 bg-amber-600 hover:bg-amber-500 text-zinc-950 font-bold rounded-lg text-xs flex items-center gap-1 shadow-md transition"
              >
                <Send className="w-3.5 h-3.5" />
                广播
              </button>
            </form>
          </div>
        )}

        {/* 3. 📊 控制面板 (Control Panel Tab) */}
        {activeTab === 'control' && (
          <div className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-3 gap-2.5">
            <div className="flex items-center justify-between pb-1.5 border-b border-zinc-800">
              <h2 className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                <Table className="w-3.5 h-3.5" />
                Pixel Robot Pet - Control Panel
              </h2>
              <span className="text-[11px] font-mono text-zinc-400">双击列表或使用下方按钮进行管控</span>
            </div>

            <div className="flex-1 overflow-x-auto overflow-y-auto border border-zinc-800 rounded-lg bg-zinc-950">
              <table className="w-full text-left text-xs font-mono">
                <thead className="bg-zinc-900 text-zinc-400 border-b border-zinc-800">
                  <tr>
                    <th className="px-3 py-1.5">ID</th>
                    <th className="px-3 py-1.5">名称</th>
                    <th className="px-3 py-1.5">个性</th>
                    <th className="px-3 py-1.5">状态</th>
                    <th className="px-3 py-1.5">意识</th>
                    <th className="px-3 py-1.5">经验</th>
                    <th className="px-3 py-1.5">位置</th>
                    <th className="px-3 py-1.5">速度</th>
                    <th className="px-3 py-1.5">大小</th>
                    <th className="px-3 py-1.5 text-center">显示</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-zinc-800 text-zinc-300">
                  {robots.map((r, index) => (
                    <tr key={r.id} className="hover:bg-zinc-900/60 transition">
                      <td className="px-3 py-1.5 font-bold text-emerald-400">{index + 1}</td>
                      <td className="px-3 py-1.5 font-semibold">
                        {settings.hideNameAndPersonality ? `机器人 #${r.id}` : r.name}
                      </td>
                      <td className="px-3 py-1.5 text-zinc-400">
                        {settings.hideNameAndPersonality ? '***' : r.personality}
                      </td>
                      <td className="px-3 py-1.5">
                        {r.isDead ? (
                          <span className="text-rose-400 font-bold">☠ 阵亡</span>
                        ) : r.isMoving ? (
                          <span className="text-emerald-400">▶ 移动</span>
                        ) : (
                          <span className="text-amber-400">⏸ 暂停</span>
                        )}
                      </td>
                      <td className="px-3 py-1.5 text-emerald-400">Lvl 1.0</td>
                      <td className="px-3 py-1.5 text-zinc-400">{r.exp}/{r.maxExp}</td>
                      <td className="px-3 py-1.5 text-zinc-400">({r.posX}, {r.posY})</td>
                      <td className="px-3 py-1.5 text-zinc-300">{r.speedMultiplier || 1.0}x</td>
                      <td className="px-3 py-1.5 text-zinc-300">{r.size || 64}px</td>
                      <td className="px-3 py-1.5 text-center">
                        <button
                          onClick={() => bridge.invoke('toggleRobotVisibility', { robotId: r.id })}
                          className={`p-1 rounded hover:bg-zinc-800 ${r.isVisible ? 'text-emerald-400' : 'text-zinc-600'}`}
                          title={r.isVisible ? '已显示' : '已隐藏'}
                        >
                          {r.isVisible ? <Eye className="w-3.5 h-3.5" /> : <EyeOff className="w-3.5 h-3.5" />}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex flex-wrap items-center gap-2 pt-1 border-t border-zinc-800">
              <button
                onClick={() => bridge.invoke('spawnRobot')}
                className="flex items-center gap-1 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-lg text-xs shadow transition"
              >
                <Plus className="w-3.5 h-3.5" />
                投放机器人
              </button>

              <button
                onClick={() => setIsAiModalOpen(true)}
                className="flex items-center gap-1 px-3 py-1.5 bg-emerald-950 hover:bg-emerald-900 border border-emerald-600 text-emerald-400 font-bold rounded-lg text-xs transition"
              >
                <Bot className="w-3.5 h-3.5" />
                🤖 AI智能生成
              </button>

              <button
                onClick={() => bridge.invoke('quickSpawnRobot')}
                className="flex items-center gap-1 px-3 py-1.5 bg-zinc-800 hover:bg-zinc-700 text-emerald-400 font-bold rounded-lg text-xs border border-zinc-700 transition"
              >
                ⚡ 快速投放
              </button>

              <button
                onClick={() => bridge.invoke('pauseAllRobots')}
                className="flex items-center gap-1 px-3 py-1.5 bg-amber-950 hover:bg-amber-900 text-amber-400 border border-amber-700 font-bold rounded-lg text-xs transition"
              >
                <Pause className="w-3.5 h-3.5" />
                全部暂停
              </button>

              <button
                onClick={() => bridge.invoke('resumeAllRobots')}
                className="flex items-center gap-1 px-3 py-1.5 bg-emerald-950 hover:bg-emerald-900 text-emerald-400 border border-emerald-700 font-bold rounded-lg text-xs transition"
              >
                <Play className="w-3.5 h-3.5" />
                全部启动
              </button>

              <button
                onClick={() => bridge.invoke('clearAllRobots')}
                className="flex items-center gap-1 px-3 py-1.5 bg-rose-950 hover:bg-rose-900 text-rose-400 border border-rose-700 font-bold rounded-lg text-xs transition"
              >
                <Trash2 className="w-3.5 h-3.5" />
                清除全部
              </button>
            </div>

            <div className="flex items-center justify-between text-[11px] font-mono text-zinc-400 px-1 pt-0.5 border-t border-zinc-800/60">
              <span>
                总数: <b className="text-emerald-400">{stats.totalRobots || robots.length}</b> | 
                移动: <b className="text-emerald-400">{stats.movingRobots || 0}</b> | 
                暂停: <b className="text-amber-400">{stats.pausedRobots || 0}</b> | 
                Token: <b className="text-amber-400">{stats.totalTokens}</b>
              </span>
            </div>
          </div>
        )}

        {/* 1. ⚔️ 武器与战斗配置 Tab */}
        {activeTab === 'combat' && (
          <form onSubmit={handleSaveSettings} className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-5 overflow-y-auto space-y-5 custom-scrollbar">
            <div className="flex items-center justify-between pb-3 border-b border-zinc-800">
              <div>
                <h2 className="text-sm font-bold text-rose-400 flex items-center gap-1.5">
                  <Shield className="w-4 h-4" />
                  ⚔️ 武器与战斗管理系统
                </h2>
                <p className="text-[11px] text-zinc-400 mt-0.5 font-mono">Combat Rules, Weapon Master & Skills Whitelist</p>
              </div>
              {saveStatus && <span className="text-xs text-rose-400 font-bold animate-bounce">{saveStatus}</span>}
            </div>

            {/* 战斗基本法则 */}
            <div className="space-y-3 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <h3 className="text-xs font-bold text-zinc-200">⚔️ 战斗动作与物理法则</h3>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800">
                  <label className="block text-xs font-bold text-zinc-200 mb-1">⚔️ 动作互动模式</label>
                  <select
                    value={settings.actionMode || settings.battleMode}
                    onChange={e => setSettings({ ...settings, actionMode: e.target.value, battleMode: e.target.value })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 focus:outline-none focus:border-rose-500/50"
                  >
                    <option value="近远交替">⚔️ 近远交替 (远程技能与肉搏结合)</option>
                    <option value="全程近战拉扯">🤺 全程近战拉扯 (贴身追逐肉搏)</option>
                    <option value="全程远程对射">💥 全程远程对射 (保持距离对射)</option>
                    <option value="和平相处">🕊️ 和平相处 (友好走动，不下死手)</option>
                  </select>
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <span className="text-[11px] font-bold text-zinc-300">基础血量上限 (Max HP)</span>
                  <input
                    type="number"
                    min="100"
                    max="50000"
                    step="100"
                    value={settings.robotMaxHp || 1000}
                    onChange={e => setSettings({ ...settings, robotMaxHp: parseInt(e.target.value) || 1000 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-2 py-1 text-xs text-zinc-100 font-mono"
                  />
                </div>

                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">🛡️ 全局无敌免伤 (God Mode)</div>
                    <div className="text-[10px] text-zinc-400">所有机器人免除扣血与阵亡</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={!!settings.isGodMode}
                    onChange={e => setSettings({ ...settings, isGodMode: e.target.checked })}
                    className="w-4 h-4 accent-rose-500 rounded"
                  />
                </label>
              </div>
            </div>

            {/* 武器库管理 */}
            <div className="space-y-3 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-bold text-rose-400 flex items-center gap-1.5">
                  <Flame className="w-3.5 h-3.5" />
                  🎯 武器与招式白名单 (勾选开启/关闭)
                </h3>

                <label className="flex items-center gap-2 cursor-pointer bg-zinc-900 px-3 py-1 rounded-lg border border-zinc-800">
                  <span className="text-xs font-bold text-zinc-200">👑 武器大师模式</span>
                  <input
                    type="checkbox"
                    checked={!!settings.isWeaponMaster}
                    onChange={e => setSettings({ ...settings, isWeaponMaster: e.target.checked })}
                    className="w-4 h-4 accent-rose-500 rounded"
                  />
                </label>
              </div>

              <div className="space-y-3 pt-1">
                {[
                  {
                    title: '✨ 基础能量光束',
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
                    title: '👑 武器大师兵器',
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
                    title: '🤼 物理近战招式',
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
                  <div key={idx} className="bg-zinc-900 p-3 rounded-lg border border-zinc-800">
                    <div className="font-bold text-[11px] text-zinc-300 mb-2 pb-1 border-b border-zinc-800/80">{category.title}</div>
                    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2">
                      {category.items.map(weapon => (
                        <label key={weapon.id} className="flex items-center gap-1.5 cursor-pointer group">
                          <input
                            type="checkbox"
                            checked={settings.enabledWeapons?.includes(weapon.id) ?? true}
                            onChange={e => {
                              const checked = e.target.checked;
                              const current = settings.enabledWeapons || [];
                              if (checked) {
                                if (!current.includes(weapon.id)) setSettings({ ...settings, enabledWeapons: [...current, weapon.id] });
                              } else {
                                setSettings({ ...settings, enabledWeapons: current.filter(w => w !== weapon.id) });
                              }
                            }}
                            className="w-3.5 h-3.5 accent-rose-500 rounded"
                          />
                          <span className="text-[11px] text-zinc-400 group-hover:text-zinc-200 transition">
                            {weapon.label}
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <button
              type="submit"
              className="w-full py-2.5 bg-rose-600 hover:bg-rose-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center justify-center gap-1.5 shadow-lg transition"
            >
              <Save className="w-4 h-4" />
              保存战斗与武器设置
            </button>
          </form>
        )}

        {/* 2. 🗣️ AI 与语言性格配置 Tab */}
        {activeTab === 'ai' && (
          <form onSubmit={handleSaveSettings} className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-5 overflow-y-auto space-y-5 custom-scrollbar">
            <div className="flex items-center justify-between pb-3 border-b border-zinc-800">
              <div>
                <h2 className="text-sm font-bold text-cyan-400 flex items-center gap-1.5">
                  <MessageSquare className="w-4 h-4" />
                  🗣️ 语言性格与 AI 互动配置
                </h2>
                <p className="text-[11px] text-zinc-400 mt-0.5 font-mono">Language Interaction Modes & LLM Engine Settings</p>
              </div>
              {saveStatus && <span className="text-xs text-cyan-400 font-bold animate-bounce">{saveStatus}</span>}
            </div>

            <div className="space-y-4 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800">
                <label className="block text-xs font-bold text-zinc-200 mb-1">🗣️ 语言互动模式</label>
                <select
                  value={settings.languageMode || '互骂吐槽'}
                  onChange={e => setSettings({ ...settings, languageMode: e.target.value })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-xs text-zinc-200 focus:outline-none focus:border-cyan-500/50"
                >
                  <option value="互骂吐槽">🗣️ 互骂吐槽 (搞笑对骂金句模式)</option>
                  <option value="赞美吹捧">🌸 赞美吹捧 (夸奖吹捧拉满模式)</option>
                  <option value="哲理探讨">🌌 哲理探讨 (宇宙哲学硬核思考)</option>
                  <option value="幽默冷笑话">🤡 幽默冷笑话 (讲无厘头冷笑话)</option>
                  <option value="静音默契">🤫 静音默契 (安静互动不喊话 / 完全静音)</option>
                </select>
                <p className="text-[11px] text-zinc-400 mt-1.5">选择“静音默契”后角色将安静走动战斗，不再弹出喊话气泡与全屏文本。</p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">默认开启对骂模式</div>
                    <div className="text-[10px] text-zinc-400">交战碰撞时自动吐露幽默金句</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={settings.curseModeByDefault !== false}
                    onChange={e => setSettings({ ...settings, curseModeByDefault: e.target.checked })}
                    className="w-4 h-4 accent-cyan-500 rounded"
                  />
                </label>

                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">开启 AI 动态台词思考</div>
                    <div className="text-[10px] text-zinc-400">闲置时由 LLM 引擎生成台词</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={!!settings.enableAiThinking}
                    onChange={e => setSettings({ ...settings, enableAiThinking: e.target.checked })}
                    className="w-4 h-4 accent-cyan-500 rounded"
                  />
                </label>
              </div>

              <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                <span className="text-xs font-bold text-zinc-200">AI 服务 API Key (SiliconFlow / Gemini)</span>
                <input
                  type="password"
                  placeholder="sk-..."
                  value={settings.apiKey || ''}
                  onChange={e => setSettings({ ...settings, apiKey: e.target.value })}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-100 font-mono focus:outline-none focus:border-cyan-500/50"
                />
              </div>
            </div>

            <button
              type="submit"
              className="w-full py-2.5 bg-cyan-600 hover:bg-cyan-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center justify-center gap-1.5 shadow-lg transition"
            >
              <Save className="w-4 h-4" />
              保存 AI 与语言设置
            </button>
          </form>
        )}

        {/* 3. 🎥 宣传录屏与抠图工坊 Tab */}
        {activeTab === 'recorder' && (
          <div className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-5 overflow-y-auto space-y-5 custom-scrollbar">
            <div className="flex items-center justify-between pb-3 border-b border-zinc-800">
              <div>
                <h2 className="text-sm font-bold text-purple-400 flex items-center gap-1.5">
                  <Video className="w-4 h-4" />
                  🎥 宣传录像与抠图工坊
                </h2>
                <p className="text-[11px] text-zinc-400 mt-0.5 font-mono">Promo Video Recorder & Chroma Key Studio</p>
              </div>
              {isRecording && (
                <span className="flex items-center gap-1.5 text-xs font-bold text-rose-400 bg-rose-950/80 border border-rose-700 px-3 py-1 rounded-full animate-pulse font-mono">
                  <span className="w-2 h-2 rounded-full bg-rose-500" />
                  REC {Math.floor(recordStats.durationSeconds / 60).toString().padStart(2, '0')}:{(Math.floor(recordStats.durationSeconds) % 60).toString().padStart(2, '0')} ({recordStats.frameCount} 帧)
                </span>
              )}
            </div>

            <div className="space-y-4 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <div className="space-y-2">
                <label className="text-xs font-bold text-zinc-200 block">1. 选记录制模式</label>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <button
                    type="button"
                    onClick={() => setRecordingMode('CUSTOM_BG')}
                    className={`p-3 rounded-xl border text-left transition flex items-center justify-between ${
                      recordingMode === 'CUSTOM_BG'
                        ? 'bg-purple-950/80 border-purple-500 text-purple-200'
                        : 'bg-zinc-900 border-zinc-800 text-zinc-400 hover:border-zinc-700'
                    }`}
                  >
                    <div>
                      <div className="text-xs font-bold flex items-center gap-1.5">
                        <Camera className="w-3.5 h-3.5 text-purple-400" />
                        🎬 纯净实体视角 (自选背景/绿幕抠图)
                      </div>
                      <div className="text-[10px] opacity-75 mt-0.5">仅捕获角色实体与技能动作，隐藏桌面背景，方便剪辑抠图</div>
                    </div>
                    {recordingMode === 'CUSTOM_BG' && <CheckCircle className="w-4 h-4 text-purple-400" />}
                  </button>

                  <button
                    type="button"
                    onClick={() => setRecordingMode('DESKTOP')}
                    className={`p-3 rounded-xl border text-left transition flex items-center justify-between ${
                      recordingMode === 'DESKTOP'
                        ? 'bg-purple-950/80 border-purple-500 text-purple-200'
                        : 'bg-zinc-900 border-zinc-800 text-zinc-400 hover:border-zinc-700'
                    }`}
                  >
                    <div>
                      <div className="text-xs font-bold flex items-center gap-1.5">
                        <Monitor className="w-3.5 h-3.5 text-purple-400" />
                        🖥️ 全屏桌面视角 (含真实背景)
                      </div>
                      <div className="text-[10px] opacity-75 mt-0.5">捕获桌面壁纸与软件窗口全貌，展现真实摸鱼游侠乱斗体验</div>
                    </div>
                    {recordingMode === 'DESKTOP' && <CheckCircle className="w-4 h-4 text-purple-400" />}
                  </button>
                </div>
              </div>

              {/* 底色选择 (仅纯净实体视角时显示) */}
              {recordingMode === 'CUSTOM_BG' && (
                <div className="space-y-2 pt-2 border-t border-zinc-800">
                  <label className="text-xs font-bold text-zinc-200 flex items-center gap-1.5">
                    <Palette className="w-3.5 h-3.5 text-purple-400" />
                    2. 自定义抠图底色 (Chroma Key Background)
                  </label>
                  <div className="grid grid-cols-2 sm:grid-cols-5 gap-2">
                    {[
                      { name: '🟩 绿幕抠图', hex: '#00FF00' },
                      { name: '⬛ 极客暗黑', hex: '#18181B' },
                      { name: '⬜ 纯白背景', hex: '#FFFFFF' },
                      { name: '🟦 蓝幕抠图', hex: '#0000FF' },
                    ].map(bg => (
                      <button
                        key={bg.hex}
                        type="button"
                        onClick={() => setRecordingBgHex(bg.hex)}
                        className={`p-2 rounded-lg border text-xs font-bold flex items-center justify-between transition ${
                          recordingBgHex === bg.hex
                            ? 'border-purple-500 bg-purple-950/50 text-purple-300'
                            : 'border-zinc-800 bg-zinc-900 text-zinc-300 hover:border-zinc-700'
                        }`}
                      >
                        <span>{bg.name}</span>
                        <span className="w-3.5 h-3.5 rounded border border-zinc-700 shadow-inner" style={{ backgroundColor: bg.hex }} />
                      </button>
                    ))}

                    <div className="flex items-center gap-1 bg-zinc-900 border border-zinc-800 rounded-lg px-2">
                      <span className="text-[10px] text-zinc-400 font-bold">Hex</span>
                      <input
                        type="text"
                        value={recordingBgHex}
                        onChange={e => setRecordingBgHex(e.target.value)}
                        className="w-full bg-transparent text-xs text-zinc-200 font-mono focus:outline-none"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* 控制按钮 */}
              <div className="pt-3 border-t border-zinc-800 flex flex-col gap-2">
                {!isRecording ? (
                  <button
                    type="button"
                    onClick={handleStartRecording}
                    className="w-full py-3 bg-purple-600 hover:bg-purple-500 text-zinc-950 font-bold rounded-xl text-sm flex items-center justify-center gap-2 shadow-lg transition"
                  >
                    <Video className="w-4 h-4" />
                    🔴 开始宣传录制
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={handleStopRecording}
                    className="w-full py-3 bg-rose-600 hover:bg-rose-500 text-white font-bold rounded-xl text-sm flex items-center justify-center gap-2 shadow-lg transition animate-pulse"
                  >
                    <Square className="w-4 h-4 fill-white" />
                    ⏹️ 停止录制并导出视频帧
                  </button>
                )}
              </div>

              {/* 导出信息提示 */}
              {recordStats.folderPath && (
                <div className="p-3 bg-zinc-900 border border-purple-800/50 rounded-xl flex items-center justify-between">
                  <div className="space-y-0.5">
                    <div className="text-xs font-bold text-purple-300">✅ 录制完成！视频帧已导出至：</div>
                    <div className="text-[11px] text-zinc-400 font-mono truncate max-w-md">{recordStats.folderPath}</div>
                  </div>
                  <button
                    type="button"
                    onClick={() => bridge.invoke('openFolder', { path: recordStats.folderPath })}
                    className="px-3 py-1.5 bg-purple-950 hover:bg-purple-900 border border-purple-700 text-purple-300 font-bold rounded-lg text-xs flex items-center gap-1 transition shrink-0"
                  >
                    <FolderOpen className="w-3.5 h-3.5" />
                    打开文件夹
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* 4. ⚙️ 基础偏好 Tab */}
        {activeTab === 'settings' && (
          <form onSubmit={handleSaveSettings} className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-5 overflow-y-auto space-y-5 custom-scrollbar">
            <div className="flex items-center justify-between pb-3 border-b border-zinc-800">
              <div>
                <h2 className="text-sm font-bold text-amber-400 flex items-center gap-1.5">
                  <SlidersHorizontal className="w-4 h-4" />
                  ⚙️ 系统与基础偏好设置
                </h2>
                <p className="text-[11px] text-zinc-400 mt-0.5 font-mono">Window, Performance & Display Scale Controls</p>
              </div>
              {saveStatus && <span className="text-xs text-amber-400 font-bold animate-bounce">{saveStatus}</span>}
            </div>

            <div className="space-y-4 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <div className="flex justify-between text-xs text-zinc-200">
                    <span className="font-bold">窗口不透明度</span>
                    <span className="font-mono text-amber-400 font-bold">{Math.round((settings.opacity || 0.95) * 100)}%</span>
                  </div>
                  <input
                    type="range"
                    min="0.1"
                    max="1.0"
                    step="0.05"
                    value={settings.opacity || 0.95}
                    onChange={e => setSettings({ ...settings, opacity: parseFloat(e.target.value) })}
                    className="w-full accent-amber-500 bg-zinc-800 rounded-lg cursor-pointer"
                  />
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <label className="block text-xs font-bold text-zinc-200">浏览器默认首页 URL</label>
                  <input
                    type="text"
                    value={settings.homeUrl || 'https://www.xiaoheiv.top'}
                    onChange={e => setSettings({ ...settings, homeUrl: e.target.value })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 font-mono focus:outline-none focus:border-amber-500/50"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">开机自动启动</div>
                    <div className="text-[10px] text-zinc-400">Windows 开机自动进入摸鱼主控</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={!!settings.autoStart}
                    onChange={e => setSettings({ ...settings, autoStart: e.target.checked })}
                    className="w-4 h-4 accent-amber-500 rounded"
                  />
                </label>

                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">隐藏名称与性格</div>
                    <div className="text-[10px] text-zinc-400">隐藏机器人头顶与卡片展示名</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={!!settings.hideNameAndPersonality}
                    onChange={e => setSettings({ ...settings, hideNameAndPersonality: e.target.checked })}
                    className="w-4 h-4 accent-amber-500 rounded"
                  />
                </label>
              </div>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <span className="text-[11px] font-bold text-zinc-300">默认尺寸 (px)</span>
                  <input
                    type="number"
                    min="16"
                    max="128"
                    value={settings.robotSize || 64}
                    onChange={e => setSettings({ ...settings, robotSize: parseInt(e.target.value) || 64 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-2 py-1 text-xs text-zinc-100 font-mono"
                  />
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <span className="text-[11px] font-bold text-zinc-300">移动速度 (%)</span>
                  <input
                    type="number"
                    min="50"
                    max="300"
                    value={settings.robotSpeed || 100}
                    onChange={e => setSettings({ ...settings, robotSpeed: parseInt(e.target.value) || 100 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-2 py-1 text-xs text-zinc-100 font-mono"
                  />
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <span className="text-[11px] font-bold text-zinc-300">音效音量 (%)</span>
                  <input
                    type="number"
                    min="0"
                    max="100"
                    value={settings.soundVolume ?? 50}
                    onChange={e => setSettings({ ...settings, soundVolume: parseInt(e.target.value) || 0 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-2 py-1 text-xs text-zinc-100 font-mono"
                  />
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <span className="text-[11px] font-bold text-zinc-300">技能特效 (%)</span>
                  <input
                    type="number"
                    min="10"
                    max="200"
                    value={settings.skillScale || 100}
                    onChange={e => setSettings({ ...settings, skillScale: parseInt(e.target.value) || 100 })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-2 py-1 text-xs text-zinc-100 font-mono"
                  />
                </div>
              </div>
            </div>

            <button
              type="submit"
              className="w-full py-2.5 bg-amber-600 hover:bg-amber-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center justify-center gap-1.5 shadow-lg transition"
            >
              <Save className="w-4 h-4" />
              保存基础偏好
            </button>
          </form>
        )}

        {/* 5. 1-on-1 单人对话终端 */}
        {!['overview', 'world', 'control', 'combat', 'ai', 'recorder', 'settings'].includes(activeTab) && activeRobot && (
          <div className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-3">
            <div className="flex items-center justify-between pb-2 border-b border-zinc-800 mb-2">
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-emerald-500" />
                <h2 className="font-bold text-xs text-zinc-100">
                  {settings.hideNameAndPersonality ? `机器人 #${activeRobot.id}` : activeRobot.name} 的私聊终端
                </h2>
                {!settings.hideNameAndPersonality && (
                  <span className="text-[11px] text-zinc-400">({activeRobot.personality})</span>
                )}
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => handleInspirate(activeRobot.id)}
                  className="px-2.5 py-0.5 bg-amber-950 text-amber-400 border border-amber-700/60 rounded-lg text-xs font-semibold transition"
                >
                  ⚡ 启发思想
                </button>
              </div>
            </div>

            <div className="flex-1 overflow-y-auto space-y-2.5 pr-2">
              {(privateChats[activeRobot.id] || []).map((msg, i) => (
                <div key={i} className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
                  {msg.thought && (
                    <div className="max-w-[80%] mb-1 p-2 bg-zinc-950 border border-amber-800/40 rounded-lg text-[11px] text-amber-400 font-mono italic">
                      💭 [思维链]: {msg.thought}
                    </div>
                  )}

                  <div
                    className={`max-w-[80%] p-2.5 rounded-xl text-xs ${
                      msg.role === 'user'
                        ? 'bg-emerald-600 text-zinc-950 font-semibold rounded-br-none shadow'
                        : 'bg-zinc-950 text-zinc-200 rounded-bl-none border border-zinc-800'
                    }`}
                  >
                    {msg.content.replace(/🤖\s*\[AI生成\]/g, '')}
                  </div>
                </div>
              ))}

              {activeRobot.isThinking && (
                <div className="flex items-center gap-1.5 text-xs text-amber-400 bg-amber-950/60 p-2 rounded-lg border border-amber-800/50 w-fit font-mono">
                  <Cpu className="w-3.5 h-3.5 animate-spin" />
                  <span>{settings.hideNameAndPersonality ? '机器人' : activeRobot.name} 正在思考回应...</span>
                </div>
              )}

              <div ref={chatEndRef} />
            </div>

            <form onSubmit={e => handleSendPrivate(activeRobot.id, e)} className="mt-2 flex items-center gap-2 pt-2 border-t border-zinc-800">
              <input
                type="text"
                placeholder={`与 ${settings.hideNameAndPersonality ? '机器人' : activeRobot.name} 单独对话...`}
                value={privateInput}
                onChange={e => setPrivateInput(e.target.value)}
                className="flex-1 bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50"
              />
              <button
                type="submit"
                className="px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-lg text-xs flex items-center gap-1 shadow transition"
              >
                <Send className="w-3.5 h-3.5" />
                发送
              </button>
            </form>
          </div>
        )}
      </main>
      </div>

      {/* AI 智能生成机器人弹窗 (包含全流程步骤进度展示) */}
      <AiGeneratorModal
        isOpen={isAiModalOpen}
        onClose={() => setIsAiModalOpen(false)}
        onSuccess={fetchData}
      />

      {/* 机器人全维度属性与人设控制详情弹窗 */}
      <RobotDetailsModal
        robot={selectedRobotForDetails}
        isOpen={!!selectedRobotForDetails}
        onClose={() => setSelectedRobotForDetails(null)}
        onOpenPrivateChat={handleOpenPrivateChat}
        onInspirate={handleInspirate}
        onToggleCurse={handleToggleCurse}
      />

      {/* 实时战况与伤害击杀数据看板弹窗 */}
      <CombatLogModal
        isOpen={isCombatModalOpen}
        onClose={() => setIsCombatModalOpen(false)}
      />
    </div>
  );
};
