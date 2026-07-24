import React, { useState, useEffect, useRef } from 'react';
import bridge from '../../utils/bridge';

interface CombatLogItem {
  time: string;
  attacker: string;
  target: string;
  skill: string;
  damage: number;
  type: string;
  message: string;
}

interface RobotStat {
  id: string;
  name: string;
  personality: string;
  color: string;
  hp: number;
  maxHp: number;
  damageDealt: number;
  damageTaken: number;
  kills: number;
  shotsFired: number;
  isWeaponMaster: boolean;
  weapons?: string[];
}

interface CombatLogModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CombatLogModal: React.FC<CombatLogModalProps> = ({ isOpen, onClose }) => {
  const [logs, setLogs] = useState<CombatLogItem[]>([]);
  const [stats, setStats] = useState<RobotStat[]>([]);
  const [activeTab, setActiveTab] = useState<'stats' | 'logs'>('stats');
  const [autoScroll, setAutoScroll] = useState(true);
  const logEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const fetchData = async () => {
      try {
        const statsRes = await bridge.send<{ success: boolean; stats: RobotStat[] }>('getCombatStats', {});
        if (statsRes && statsRes.stats) {
          setStats(statsRes.stats);
        }

        const logsRes = await bridge.send<{ success: boolean; logs: CombatLogItem[] }>('getCombatLogs', {});
        if (logsRes && logsRes.logs) {
          setLogs(logsRes.logs);
        }
      } catch (e) {
        console.error('Fetch combat data error:', e);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 1000); // 1秒实时刷新

    return () => clearInterval(interval);
  }, [isOpen]);

  useEffect(() => {
    if (autoScroll && logEndRef.current && activeTab === 'logs') {
      logEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [logs, autoScroll, activeTab]);

  if (!isOpen) return null;

  const maxDamage = Math.max(1, ...stats.map(s => s.damageDealt));

  const handleClearLogs = async () => {
    await bridge.send('clearCombatLogs', {});
    setLogs([]);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm animate-fade-in p-4">
      <div className="bg-zinc-900 border border-zinc-800 rounded-xl w-full max-w-4xl max-h-[85vh] flex flex-col shadow-2xl overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 bg-zinc-950 border-b border-zinc-800">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-rose-500/20 border border-rose-500/40 flex items-center justify-center text-rose-400 font-bold text-lg">
              ⚔️
            </div>
            <div>
              <h2 className="text-zinc-100 font-bold text-base flex items-center gap-2">
                实时战斗风云与数据统计
                <span className="text-xs px-2 py-0.5 rounded-full bg-rose-950 border border-rose-800 text-rose-300 font-mono">
                  LIVE 60FPS
                </span>
              </h2>
              <p className="text-zinc-400 text-xs">查看桌面角色的输出伤害、击杀榜与即时对战日志</p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {/* Tab Switcher */}
            <div className="flex bg-zinc-900 border border-zinc-800 rounded-lg p-1">
              <button
                onClick={() => setActiveTab('stats')}
                className={`px-3 py-1 text-xs font-semibold rounded-md transition-all ${
                  activeTab === 'stats'
                    ? 'bg-rose-600 text-white shadow'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                📊 伤害击杀榜 ({stats.length})
              </button>
              <button
                onClick={() => setActiveTab('logs')}
                className={`px-3 py-1 text-xs font-semibold rounded-md transition-all ${
                  activeTab === 'logs'
                    ? 'bg-rose-600 text-white shadow'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                📜 实时战报 ({logs.length})
              </button>
            </div>

            <button
              onClick={onClose}
              className="text-zinc-400 hover:text-zinc-100 p-1.5 rounded-lg hover:bg-zinc-800 transition-colors"
            >
              ✕
            </button>
          </div>
        </div>

        {/* Content Body */}
        <div className="p-6 overflow-y-auto flex-1 custom-scrollbar">
          {activeTab === 'stats' ? (
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-2">
                <div className="bg-zinc-950/60 border border-zinc-800/80 rounded-lg p-3 flex flex-col justify-center">
                  <span className="text-zinc-400 text-xs">全员累积输出伤害</span>
                  <span className="text-rose-400 font-mono text-xl font-bold">
                    {stats.reduce((acc, curr) => acc + curr.damageDealt, 0)} HP
                  </span>
                </div>
                <div className="bg-zinc-950/60 border border-zinc-800/80 rounded-lg p-3 flex flex-col justify-center">
                  <span className="text-zinc-400 text-xs">总击杀次数</span>
                  <span className="text-amber-400 font-mono text-xl font-bold">
                    {stats.reduce((acc, curr) => acc + curr.kills, 0)} Kills
                  </span>
                </div>
                <div className="bg-zinc-950/60 border border-zinc-800/80 rounded-lg p-3 flex flex-col justify-center">
                  <span className="text-zinc-400 text-xs">最高 MVP 表现</span>
                  <span className="text-emerald-400 font-bold text-sm truncate">
                    {stats.length > 0 ? `👑 ${stats[0].name} (${stats[0].damageDealt} 伤害)` : '暂无数据'}
                  </span>
                </div>
              </div>

              {stats.length === 0 ? (
                <div className="text-center py-12 text-zinc-500 text-sm">暂无参与对战的机器人数据</div>
              ) : (
                <div className="space-y-3">
                  {stats.map((item, idx) => {
                    const percent = Math.min(100, Math.round((item.damageDealt / maxDamage) * 100));
                    const isMVP = idx === 0 && item.damageDealt > 0;

                    return (
                      <div
                        key={item.id}
                        className={`bg-zinc-950 border ${
                          isMVP ? 'border-amber-500/50 bg-amber-950/10' : 'border-zinc-800/80'
                        } rounded-xl p-4 transition-all hover:border-zinc-700`}
                      >
                        <div className="flex items-center justify-between mb-2">
                          <div className="flex items-center gap-2">
                            <span className="font-mono text-xs font-bold text-zinc-500 w-5">#{idx + 1}</span>
                            <div
                              className="w-3.5 h-3.5 rounded-full"
                              style={{ backgroundColor: item.color || '#10B981' }}
                            />
                            <span className="font-bold text-zinc-100 text-sm flex items-center gap-1.5">
                              {item.name}
                              {isMVP && (
                                <span className="text-xs px-1.5 py-0.2 bg-amber-500/20 text-amber-300 border border-amber-500/40 rounded font-semibold flex items-center gap-1">
                                  👑 MVP
                                </span>
                              )}
                              {item.isWeaponMaster && (
                                <span className="text-[10px] px-1 bg-rose-950 text-rose-300 border border-rose-800 rounded">
                                  ⚔️ 大师
                                </span>
                              )}
                            </span>
                            <span className="text-xs px-2 py-0.5 bg-zinc-800 text-zinc-300 rounded font-mono">
                              {item.personality}
                            </span>
                          </div>

                          <div className="flex items-center gap-4 text-xs font-mono">
                            <div>
                              <span className="text-zinc-500">HP: </span>
                              <span className="text-emerald-400 font-bold">{item.hp}/{item.maxHp}</span>
                            </div>
                            <div>
                              <span className="text-zinc-500">伤害: </span>
                              <span className="text-rose-400 font-bold">{item.damageDealt}</span>
                            </div>
                            <div>
                              <span className="text-zinc-500">承伤: </span>
                              <span className="text-blue-400">{item.damageTaken}</span>
                            </div>
                            <div>
                              <span className="text-zinc-500">击杀: </span>
                              <span className="text-amber-400 font-bold">{item.kills}</span>
                            </div>
                          </div>
                        </div>

                        {/* Damage Progress Bar */}
                        <div className="w-full bg-zinc-900 h-2 rounded-full overflow-hidden flex">
                          <div
                            className="h-full transition-all duration-500"
                            style={{
                              width: `${percent}%`,
                              backgroundColor: item.color || '#E11D48'
                            }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-3">
              <div className="flex items-center justify-between pb-2 border-b border-zinc-800/80">
                <span className="text-xs text-zinc-400 font-mono">包含攻击命中、击杀事件与技能释放日志</span>
                <div className="flex items-center gap-3">
                  <label className="flex items-center gap-1.5 text-xs text-zinc-400 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={autoScroll}
                      onChange={e => setAutoScroll(e.target.checked)}
                      className="rounded bg-zinc-800 border-zinc-700 text-rose-600 focus:ring-0"
                    />
                    自动滚动
                  </label>
                  <button
                    onClick={handleClearLogs}
                    className="text-xs px-2.5 py-1 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 rounded border border-zinc-700 transition-colors"
                  >
                    🗑️ 清空战报
                  </button>
                </div>
              </div>

              <div className="space-y-1.5 font-mono text-xs max-h-[50vh] overflow-y-auto pr-1">
                {logs.length === 0 ? (
                  <div className="text-center py-12 text-zinc-500">暂无战报记录，去让机器人们相遇交火吧！</div>
                ) : (
                  logs.map((log, index) => (
                    <div
                      key={index}
                      className={`p-2 rounded-lg flex items-center justify-between border ${
                        log.type === 'KILL'
                          ? 'bg-rose-950/40 border-rose-800/60 text-rose-200 font-bold'
                          : 'bg-zinc-950/80 border-zinc-800/60 text-zinc-300'
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <span className="text-zinc-500 font-mono text-[11px]">{log.time}</span>
                        <span>{log.message}</span>
                      </div>
                      {log.damage > 0 && (
                        <span className="text-rose-400 font-bold bg-rose-950/80 px-2 py-0.5 rounded border border-rose-900 text-[11px]">
                          -{log.damage} HP
                        </span>
                      )}
                    </div>
                  ))
                )}
                <div ref={logEndRef} />
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-3 bg-zinc-950 border-t border-zinc-800 text-xs text-zinc-500">
          <span>提示: 点击机器人的名字可以在桌面上精确定位其位置</span>
          <button
            onClick={onClose}
            className="px-4 py-1.5 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 font-semibold rounded-lg transition-colors"
          >
            关闭
          </button>
        </div>
      </div>
    </div>
  );
};
