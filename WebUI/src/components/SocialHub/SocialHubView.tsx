import React, { useState, useEffect, useRef } from 'react';
import { 
  Bot, MessageSquare, Globe, Zap, Shield, Search, 
  Send, Flame, Cpu, RefreshCw, Sparkles, Terminal
} from 'lucide-react';
import { bridge } from '../../utils/bridge';
import type { RobotInfo, SocialMessage, ChatMessage, SystemStats } from '../../types/bridge';

export const SocialHubView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'overview' | 'world' | string>('overview');
  const [robots, setRobots] = useState<RobotInfo[]>([]);
  const [stats, setStats] = useState<SystemStats>({
    onlineCount: 0,
    battleMode: '近远交替',
    totalTokens: 0,
    totalCostYuan: 0,
  });

  const [worldMessages, setWorldMessages] = useState<SocialMessage[]>([]);
  const [broadcastInput, setBroadcastInput] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  
  // 单人私聊状态
  const [privateChats, setPrivateChats] = useState<Record<string, ChatMessage[]>>({});
  const [privateInput, setPrivateInput] = useState('');

  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // 初始获取数据
    fetchData();

    // 监听来自 C# 的实时推送
    const unsubRobots = bridge.on('robotsUpdated', (data: RobotInfo[]) => setRobots(data));
    const unsubStats = bridge.on('statsUpdated', (data: SystemStats) => setStats(data));
    const unsubWorld = bridge.on('worldMessageReceived', (msg: SocialMessage) => {
      setWorldMessages(prev => [...prev.slice(-49), msg]);
    });
    const unsubPrivate = bridge.on('privateMessageReceived', ({ robotId, message }: { robotId: string; message: ChatMessage }) => {
      setPrivateChats(prev => {
        const currentList = prev[robotId] || [];
        // 避免重复添加完全一样的 user 消息
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

    // 乐观添加发送消息
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

  const filteredRobots = robots.filter(r => 
    r.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
    r.personality.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const activeRobot = robots.find(r => r.id === activeTab);

  return (
    <div className="flex flex-col h-screen bg-zinc-950 text-zinc-100 font-sans select-none overflow-hidden">
      {/* 顶部 Header (翡翠绿与温润金配色，无紫无蓝) */}
      <header className="flex items-center justify-between px-5 py-3 bg-zinc-900 border-b border-zinc-800">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-emerald-950 border border-emerald-700/50 rounded-xl text-emerald-400">
            <MessageSquare className="w-5 h-5 animate-pulse" />
          </div>
          <div>
            <h1 className="text-base font-bold text-emerald-400">
              🤖 机器人社交中心 | Robot Social Hub
            </h1>
            <p className="text-xs text-zinc-400">实时大模型对话、全网广播与 1-on-1 极客终端</p>
          </div>
        </div>

        {/* 状态 Pill Badges */}
        <div className="flex items-center gap-2">
          <span className="flex items-center gap-1.5 px-3 py-1 bg-zinc-950 border border-emerald-600/50 rounded-full text-xs font-semibold text-emerald-400">
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
            🟢 在线: {stats.onlineCount}
          </span>
          <span className="flex items-center gap-1.5 px-3 py-1 bg-zinc-950 border border-zinc-700 rounded-full text-xs font-semibold text-zinc-300">
            <Shield className="w-3.5 h-3.5 text-emerald-400" />
            ⚔️ 模式: {stats.battleMode}
          </span>
          <span className="flex items-center gap-1.5 px-3 py-1 bg-zinc-950 border border-amber-600/50 rounded-full text-xs font-semibold text-amber-400">
            <Zap className="w-3.5 h-3.5 text-amber-400" />
            🪙 Token: {stats.totalTokens} (¥{stats.totalCostYuan.toFixed(4)})
          </span>
          <button 
            onClick={fetchData}
            className="p-1.5 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 rounded-lg transition"
            title="刷新状态"
          >
            <RefreshCw className="w-4 h-4" />
          </button>
        </div>
      </header>

      {/* 顶部动态 Tab 导航栏 */}
      <nav className="flex items-center gap-1 px-4 py-2 bg-zinc-900 border-b border-zinc-800 overflow-x-auto scrollbar-none">
        <button
          onClick={() => setActiveTab('overview')}
          className={`flex items-center gap-2 px-4 py-1.5 rounded-lg text-xs font-semibold transition ${
            activeTab === 'overview'
              ? 'bg-emerald-950 text-emerald-400 border border-emerald-600/60 shadow-md'
              : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
          }`}
        >
          <Bot className="w-4 h-4" />
          🤖 机器人概览 ({robots.length})
        </button>

        <button
          onClick={() => setActiveTab('world')}
          className={`flex items-center gap-2 px-4 py-1.5 rounded-lg text-xs font-semibold transition ${
            activeTab === 'world'
              ? 'bg-amber-950 text-amber-400 border border-amber-600/60 shadow-md'
              : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
          }`}
        >
          <Globe className="w-4 h-4" />
          🌍 世界广播频道
        </button>

        {/* 动态单人私聊选项卡 */}
        {robots.filter(r => privateChats[r.id]).map(robot => (
          <button
            key={robot.id}
            onClick={() => setActiveTab(robot.id)}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-medium transition ${
              activeTab === robot.id
                ? 'bg-emerald-950 text-emerald-300 border border-emerald-600/60'
                : 'text-zinc-400 hover:bg-zinc-800'
            }`}
          >
            <Terminal className="w-3.5 h-3.5 text-emerald-400" />
            <span>💬 {robot.name}</span>
          </button>
        ))}
      </nav>

      {/* 主视图区域 */}
      <main className="flex-1 overflow-hidden p-4 relative bg-zinc-950">
        {/* 1. 机器人概览模式 */}
        {activeTab === 'overview' && (
          <div className="flex flex-col h-full gap-4">
            {/* 工具栏 */}
            <div className="flex items-center justify-between gap-3 bg-zinc-900 p-3 rounded-xl border border-zinc-800">
              <div className="relative flex-1 max-w-md">
                <Search className="w-4 h-4 absolute left-3 top-2.5 text-zinc-500" />
                <input
                  type="text"
                  placeholder="搜索机器人姓名或性格..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  className="w-full bg-zinc-950 border border-zinc-800 rounded-lg pl-9 pr-4 py-1.5 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50"
                />
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => handleInspirate()}
                  className="flex items-center gap-1.5 px-3 py-1.5 bg-amber-600 hover:bg-amber-500 text-zinc-950 rounded-lg text-xs font-bold shadow-md transition"
                >
                  <Sparkles className="w-3.5 h-3.5" />
                  ⚡ 全体启发
                </button>
              </div>
            </div>

            {/* 卡片网格 */}
            <div className="flex-1 overflow-y-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 pr-1">
              {filteredRobots.map(robot => (
                <div
                  key={robot.id}
                  className="bg-zinc-900 border border-zinc-800 hover:border-emerald-600/50 rounded-2xl p-4 flex flex-col justify-between transition-all hover:shadow-xl group"
                >
                  <div>
                    {/* 卡片头部 */}
                    <div className="flex items-center justify-between mb-2">
                      <div className="flex items-center gap-2.5">
                        <div
                          className="w-9 h-9 rounded-xl flex items-center justify-center font-bold text-zinc-950 text-sm shadow-md bg-emerald-500"
                        >
                          {robot.name.slice(0, 2)}
                        </div>
                        <div>
                          <div className="flex items-center gap-2">
                            <h3 className="font-bold text-sm text-zinc-100">{robot.name}</h3>
                            <span className="text-[10px] px-1.5 py-0.5 bg-zinc-800 text-emerald-400 rounded font-mono border border-zinc-700">
                              LV.{robot.level}
                            </span>
                          </div>
                          <p className="text-xs text-zinc-400">{robot.personality}</p>
                        </div>
                      </div>

                      {/* 思考指示灯 */}
                      {robot.isThinking ? (
                        <span className="flex items-center gap-1 text-[11px] text-amber-400 bg-amber-950/80 px-2 py-0.5 rounded-full border border-amber-700 font-mono">
                          <Cpu className="w-3 h-3 animate-spin" />
                          思考中...
                        </span>
                      ) : (
                        <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-sm shadow-emerald-500" title="活跃在线" />
                      )}
                    </div>

                    {/* 血量条 */}
                    <div className="space-y-1.5 my-3">
                      <div className="flex justify-between text-[11px] text-zinc-400 font-mono">
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

                    {/* 当前对话/气泡文本 */}
                    {robot.chatText && (
                      <div className="p-2.5 bg-zinc-950 rounded-xl border border-zinc-800 text-xs text-zinc-300 mb-3 italic">
                        "{robot.chatText}"
                      </div>
                    )}
                  </div>

                  {/* 底部卡片操作 */}
                  <div className="flex items-center gap-2 pt-3 border-t border-zinc-800">
                    <button
                      onClick={() => handleOpenPrivateChat(robot.id)}
                      className="flex-1 flex items-center justify-center gap-1.5 py-1.5 bg-emerald-950 hover:bg-emerald-900 text-emerald-400 border border-emerald-700/60 rounded-lg text-xs font-semibold transition"
                    >
                      <MessageSquare className="w-3.5 h-3.5" />
                      单人私聊
                    </button>
                    <button
                      onClick={() => handleInspirate(robot.id)}
                      className="px-2.5 py-1.5 bg-amber-950 hover:bg-amber-900 text-amber-400 border border-amber-700/60 rounded-lg text-xs font-semibold transition"
                      title="单独启发"
                    >
                      ⚡
                    </button>
                    <button
                      onClick={() => handleToggleCurse(robot.id, robot.curseMode)}
                      className={`px-2.5 py-1.5 rounded-lg text-xs font-semibold border transition ${
                        robot.curseMode
                          ? 'bg-rose-950 text-rose-400 border-rose-700'
                          : 'bg-zinc-800 text-zinc-400 border-zinc-700 hover:text-zinc-200'
                      }`}
                      title={robot.curseMode ? '骂人模式开启中' : '点击开启骂人模式'}
                    >
                      <Flame className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* 2. 世界广播频道 */}
        {activeTab === 'world' && (
          <div className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-2xl p-4">
            {/* 消息滚动区 */}
            <div className="flex-1 overflow-y-auto space-y-3 pr-2 scrollbar-thin">
              {worldMessages.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-zinc-500 text-xs">
                  <Globe className="w-8 h-8 mb-2 opacity-30 text-amber-400" />
                  暂无广播消息，输入下方框开始向所有机器人广播吧！
                </div>
              ) : (
                worldMessages.map((msg, i) => (
                  <div key={i} className="flex gap-3 text-xs">
                    <span className="font-bold text-amber-400 whitespace-nowrap">[{msg.sender}]</span>
                    <span className="text-zinc-200 leading-relaxed">{msg.content}</span>
                  </div>
                ))
              )}
              <div ref={chatEndRef} />
            </div>

            {/* 广播输入栏 */}
            <form onSubmit={handleBroadcast} className="mt-3 flex items-center gap-2 pt-3 border-t border-zinc-800">
              <input
                type="text"
                placeholder="发送全局广播消息 (所有活动机器人均可接收响应)..."
                value={broadcastInput}
                onChange={e => setBroadcastInput(e.target.value)}
                className="flex-1 bg-zinc-950 border border-zinc-800 rounded-xl px-4 py-2 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-amber-500/50"
              />
              <button
                type="submit"
                className="px-4 py-2 bg-amber-600 hover:bg-amber-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center gap-1.5 shadow-md transition"
              >
                <Send className="w-3.5 h-3.5" />
                广播
              </button>
            </form>
          </div>
        )}

        {/* 3. 1-on-1 单人对话终端 */}
        {activeTab !== 'overview' && activeTab !== 'world' && activeRobot && (
          <div className="flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-2xl p-4">
            {/* 极客 Header */}
            <div className="flex items-center justify-between pb-3 border-b border-zinc-800 mb-3">
              <div className="flex items-center gap-2">
                <span className="w-3 h-3 rounded-full bg-emerald-500" />
                <h2 className="font-bold text-sm text-zinc-100">{activeRobot.name} 的私聊终端</h2>
                <span className="text-xs text-zinc-400">({activeRobot.personality})</span>
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => handleInspirate(activeRobot.id)}
                  className="px-3 py-1 bg-amber-950 text-amber-400 border border-amber-700/60 rounded-lg text-xs font-semibold transition"
                >
                  ⚡ 启发思想
                </button>
              </div>
            </div>

            {/* 对话消息区 */}
            <div className="flex-1 overflow-y-auto space-y-3 pr-2">
              {(privateChats[activeRobot.id] || []).map((msg, i) => (
                <div key={i} className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
                  {/* 思维链 Thought 展开框 */}
                  {msg.thought && (
                    <div className="max-w-[80%] mb-1 p-2 bg-zinc-950 border border-amber-800/40 rounded-lg text-[11px] text-amber-400 font-mono italic">
                      💭 [思维链]: {msg.thought}
                    </div>
                  )}

                  <div
                    className={`max-w-[80%] p-3 rounded-2xl text-xs ${
                      msg.role === 'user'
                        ? 'bg-emerald-600 text-zinc-950 font-semibold rounded-br-none shadow-md'
                        : 'bg-zinc-950 text-zinc-200 rounded-bl-none border border-zinc-800'
                    }`}
                  >
                    {msg.content}
                  </div>
                </div>
              ))}

              {/* AI 思考中浮层 */}
              {activeRobot.isThinking && (
                <div className="flex items-center gap-2 text-xs text-amber-400 bg-amber-950/60 p-2.5 rounded-xl border border-amber-800/50 w-fit font-mono">
                  <Cpu className="w-3.5 h-3.5 animate-spin" />
                  <span>{activeRobot.name} 正在思考回应...</span>
                </div>
              )}

              <div ref={chatEndRef} />
            </div>

            {/* 发送消息栏 */}
            <form onSubmit={e => handleSendPrivate(activeRobot.id, e)} className="mt-3 flex items-center gap-2 pt-3 border-t border-zinc-800">
              <input
                type="text"
                placeholder={`与 ${activeRobot.name} 单独对话...`}
                value={privateInput}
                onChange={e => setPrivateInput(e.target.value)}
                className="flex-1 bg-zinc-950 border border-zinc-800 rounded-xl px-4 py-2 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50"
              />
              <button
                type="submit"
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center gap-1.5 shadow-md transition"
              >
                <Send className="w-3.5 h-3.5" />
                发送
              </button>
            </form>
          </div>
        )}
      </main>
    </div>
  );
};
