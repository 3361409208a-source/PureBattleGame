import React, { useState, useEffect, useRef } from 'react';
import { 
  Bot, MessageSquare, Globe, Zap, Shield, Search, 
  Send, Flame, Cpu, RefreshCw, Sparkles, Terminal, SlidersHorizontal,
  Table, Plus, Pause, Play, Trash2, Eye, EyeOff, Save, Monitor
} from 'lucide-react';
import { bridge } from '../../utils/bridge';
import { AiGeneratorModal } from './AiGeneratorModal';
import type { RobotInfo, SocialMessage, ChatMessage, SystemStats, AppSettings } from '../../types/bridge';

export const SocialHubView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'overview' | 'world' | 'control' | 'settings' | string>('overview');
  const [robots, setRobots] = useState<RobotInfo[]>([]);
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

  const chatEndRef = useRef<HTMLDivElement>(null);

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

    return () => {
      unsubRobots();
      unsubStats();
      unsubWorld();
      unsubPrivate();
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
    <div className="flex flex-col h-screen bg-zinc-950 text-zinc-100 font-sans select-none overflow-hidden">
      {/* 极简单行 Header (压成单行，防覆盖) */}
      <header className="flex items-center justify-between px-4 py-2 bg-zinc-900 border-b border-zinc-800 shrink-0">
        <div className="flex items-center gap-2">
          <div className="p-1.5 bg-emerald-950 border border-emerald-700/50 rounded-lg text-emerald-400">
            <MessageSquare className="w-4 h-4" />
          </div>
          <h1 className="text-sm font-bold text-emerald-400 whitespace-nowrap">
            🤖 机器人社交中心 & 控制台
          </h1>
        </div>

        {/* 状态 Pill Badges */}
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
            onClick={fetchData}
            className="p-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 rounded-lg transition shrink-0"
            title="刷新状态"
          >
            <RefreshCw className="w-3.5 h-3.5" />
          </button>
        </div>
      </header>

      {/* 顶部动态 Tab 导航栏 */}
      <nav className="flex items-center gap-1 px-4 py-1.5 bg-zinc-900 border-b border-zinc-800 overflow-x-auto scrollbar-none shrink-0">
        <button
          onClick={() => setActiveTab('overview')}
          className={`flex items-center gap-2 px-3 py-1 rounded-lg text-xs font-semibold transition ${
            activeTab === 'overview'
              ? 'bg-emerald-950 text-emerald-400 border border-emerald-600/60 shadow-md'
              : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
          }`}
        >
          <Bot className="w-3.5 h-3.5" />
          🤖 机器人概览 ({robots.length})
        </button>

        <button
          onClick={() => setActiveTab('world')}
          className={`flex items-center gap-2 px-3 py-1 rounded-lg text-xs font-semibold transition ${
            activeTab === 'world'
              ? 'bg-amber-950 text-amber-400 border border-amber-600/60 shadow-md'
              : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
          }`}
        >
          <Globe className="w-3.5 h-3.5" />
          🌍 世界广播频道
        </button>

        <button
          onClick={() => setActiveTab('control')}
          className={`flex items-center gap-2 px-3 py-1 rounded-lg text-xs font-semibold transition ${
            activeTab === 'control'
              ? 'bg-emerald-950 text-emerald-300 border border-emerald-600/60 shadow-md'
              : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
          }`}
        >
          <Table className="w-3.5 h-3.5" />
          📊 控制面板
        </button>

        <button
          onClick={() => setActiveTab('settings')}
          className={`flex items-center gap-2 px-3 py-1 rounded-lg text-xs font-semibold transition ${
            activeTab === 'settings'
              ? 'bg-zinc-800 text-amber-400 border border-amber-600/60 shadow-md'
              : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
          }`}
        >
          <SlidersHorizontal className="w-3.5 h-3.5" />
          ⚙️ 系统设置
        </button>

        {/* 动态单人私聊选项卡 */}
        {robots.filter(r => privateChats[r.id]).map(robot => (
          <button
            key={robot.id}
            onClick={() => setActiveTab(robot.id)}
            className={`flex items-center gap-2 px-3 py-1 rounded-lg text-xs font-medium transition ${
              activeTab === robot.id
                ? 'bg-emerald-950 text-emerald-300 border border-emerald-600/60'
                : 'text-zinc-400 hover:bg-zinc-800'
            }`}
          >
            <Terminal className="w-3.5 h-3.5 text-emerald-400" />
            <span>💬 {settings.hideNameAndPersonality ? `机器人#${robot.id}` : robot.name}</span>
          </button>
        ))}
      </nav>

      {/* 主视图区域 */}
      <main className="flex-1 overflow-hidden p-3 relative bg-zinc-950">
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
                          className="w-8 h-8 rounded-lg flex items-center justify-center font-bold text-zinc-950 text-xs shadow-md bg-emerald-500"
                        >
                          {settings.hideNameAndPersonality ? '🤖' : robot.name.slice(0, 2)}
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
                      onClick={() => handleOpenPrivateChat(robot.id)}
                      className="flex-1 flex items-center justify-center gap-1 py-1 bg-emerald-950 hover:bg-emerald-900 text-emerald-400 border border-emerald-700/60 rounded-lg text-xs font-semibold transition"
                    >
                      <MessageSquare className="w-3 h-3" />
                      单人私聊
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

        {/* 4. ⚙️ 系统全功能设置 Tab */}
        {activeTab === 'settings' && (
          <form onSubmit={handleSaveSettings} className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-xl p-5 overflow-y-auto space-y-5 custom-scrollbar">
            <div className="flex items-center justify-between pb-3 border-b border-zinc-800">
              <div>
                <h2 className="text-sm font-bold text-amber-400 flex items-center gap-1.5">
                  <SlidersHorizontal className="w-4 h-4" />
                  系统与机器人全功能综合设置中心
                </h2>
                <p className="text-[11px] text-zinc-400 mt-0.5 font-mono">Comprehensive System & Robot Control Panel</p>
              </div>
              {saveStatus && <span className="text-xs text-emerald-400 font-bold animate-bounce">{saveStatus}</span>}
            </div>

            {/* 1. 🖥️ 系统与应用大项 */}
            <div className="space-y-3 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <h3 className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                <Monitor className="w-3.5 h-3.5" />
                🖥️ 系统与应用程序偏好
              </h3>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <div className="flex justify-between text-xs text-zinc-200">
                    <span className="font-bold">窗口不透明度</span>
                    <span className="font-mono text-emerald-400 font-bold">{Math.round((settings.opacity || 0.95) * 100)}%</span>
                  </div>
                  <input
                    type="range"
                    min="0.1"
                    max="1.0"
                    step="0.05"
                    value={settings.opacity || 0.95}
                    onChange={e => setSettings({ ...settings, opacity: parseFloat(e.target.value) })}
                    className="w-full accent-emerald-500 bg-zinc-800 rounded-lg cursor-pointer"
                  />
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <label className="block text-xs font-bold text-zinc-200">浏览器默认首页 URL</label>
                  <input
                    type="text"
                    value={settings.homeUrl || 'https://www.xiaoheiv.top'}
                    onChange={e => setSettings({ ...settings, homeUrl: e.target.value })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 font-mono focus:outline-none focus:border-emerald-500/50"
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
                    className="w-4 h-4 accent-emerald-500 rounded"
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
                    className="w-4 h-4 accent-emerald-500 rounded"
                  />
                </label>
              </div>
            </div>

            {/* 2. 🗣️ 语言与动作互动模式大项 */}
            <div className="space-y-3 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <h3 className="text-xs font-bold text-cyan-400 flex items-center gap-1.5">
                <MessageSquare className="w-3.5 h-3.5" />
                🗣️ 语言与动作 AI 互动模式
              </h3>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800">
                  <label className="block text-xs font-bold text-zinc-200 mb-1">🗣️ 语言互动模式</label>
                  <select
                    value={settings.languageMode || '互骂吐槽'}
                    onChange={e => setSettings({ ...settings, languageMode: e.target.value })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 focus:outline-none focus:border-cyan-500/50"
                  >
                    <option value="互骂吐槽">🗣️ 互骂吐槽 (搞笑对骂模式)</option>
                    <option value="赞美吹捧">🌸 赞美吹捧 (夸奖吹捧模式)</option>
                    <option value="哲理探讨">🌌 哲理探讨 (宇宙哲学思考)</option>
                    <option value="幽默冷笑话">🤡 幽默冷笑话 (讲无厘头冷笑话)</option>
                    <option value="静音默契">🤫 静音默契 (安静互动不喊话)</option>
                  </select>
                </div>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800">
                  <label className="block text-xs font-bold text-zinc-200 mb-1">⚔️ 动作互动模式</label>
                  <select
                    value={settings.actionMode || settings.battleMode}
                    onChange={e => setSettings({ ...settings, actionMode: e.target.value, battleMode: e.target.value })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-200 focus:outline-none focus:border-cyan-500/50"
                  >
                    <option value="近远交替">⚔️ 近远交替 (远程技能与肉搏结合)</option>
                    <option value="全程近战拉扯">🤺 全程近战拉扯 (贴身追逐肉搏)</option>
                    <option value="全程远程对射">💥 全程远程对射 (保持距离对射)</option>
                    <option value="和平相处">🕊️ 和平相处 (友好走动，不下死手)</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">默认开启对骂模式</div>
                    <div className="text-[10px] text-zinc-400">遇敌交战时默认喷吐幽默金句</div>
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
                    <div className="text-[10px] text-zinc-400">闲置时自动吐露灵感台词</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={!!settings.enableAiThinking}
                    onChange={e => setSettings({ ...settings, enableAiThinking: e.target.checked })}
                    className="w-4 h-4 accent-cyan-500 rounded"
                  />
                </label>
              </div>
            </div>

            {/* 3. ⚙️ 属性与物理控制大项 */}
            <div className="space-y-3 bg-zinc-950 p-4 rounded-xl border border-zinc-800">
              <h3 className="text-xs font-bold text-purple-400 flex items-center gap-1.5">
                <Zap className="w-3.5 h-3.5" />
                ⚙️ 属性与物理控制
              </h3>

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

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="flex items-center justify-between p-3 bg-zinc-900 rounded-lg border border-zinc-800 cursor-pointer">
                  <div>
                    <div className="text-xs font-bold text-zinc-200">武器大师模式</div>
                    <div className="text-[10px] text-zinc-400">随机为机器人分配神级随机武器</div>
                  </div>
                  <input
                    type="checkbox"
                    checked={!!settings.isWeaponMaster}
                    onChange={e => setSettings({ ...settings, isWeaponMaster: e.target.checked })}
                    className="w-4 h-4 accent-purple-500 rounded"
                  />
                </label>

                <div className="p-3 bg-zinc-900 rounded-lg border border-zinc-800 space-y-1">
                  <span className="text-xs font-bold text-zinc-200">Gemini / AI Key</span>
                  <input
                    type="password"
                    placeholder="sk-..."
                    value={settings.apiKey || ''}
                    onChange={e => setSettings({ ...settings, apiKey: e.target.value })}
                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-1 text-xs text-zinc-100 font-mono focus:outline-none focus:border-purple-500/50"
                  />
                </div>
              </div>
            </div>

            <div className="pt-2">
              <button
                type="submit"
                className="w-full py-2.5 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center justify-center gap-1.5 shadow-lg shadow-emerald-600/20 transition"
              >
                <Save className="w-4 h-4" />
                保存并对全局生效
              </button>
            </div>
          </form>
        )}

        {/* 5. 1-on-1 单人对话终端 */}
        {activeTab !== 'overview' && activeTab !== 'world' && activeTab !== 'control' && activeTab !== 'settings' && activeRobot && (
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

      {/* AI 智能生成机器人弹窗 (包含全流程步骤进度展示) */}
      <AiGeneratorModal
        isOpen={isAiModalOpen}
        onClose={() => setIsAiModalOpen(false)}
        onSuccess={fetchData}
      />
    </div>
  );
};
