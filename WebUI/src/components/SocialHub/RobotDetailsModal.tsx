import React, { useState } from 'react';
import { Sparkles, X, Brain, Zap, Flame, MessageSquare, Trash2, Crosshair, Award, BookOpen, Edit3, Save, Check, ZoomIn, ZoomOut, RotateCcw } from 'lucide-react';
import type { RobotInfo, SkillInfo } from '../../types/bridge';
import { bridge } from '../../utils/bridge';

interface RobotDetailsModalProps {
  robot: RobotInfo | null;
  isOpen: boolean;
  onClose: () => void;
  onOpenPrivateChat: (robotId: string) => void;
  onInspirate: (robotId: string) => void;
  onToggleCurse: (robotId: string, current: boolean) => void;
}

export const RobotDetailsModal: React.FC<RobotDetailsModalProps> = ({
  robot,
  isOpen,
  onClose,
  onOpenPrivateChat,
  onInspirate,
  onToggleCurse,
}) => {
  const [activeTab, setActiveTab] = useState<'brain' | 'skills' | 'phrases'>('brain');
  const [guidelines, setGuidelines] = useState(robot?.guidelines || '');
  const [isSavingGuidelines, setIsSavingGuidelines] = useState(false);
  const [savedSuccess, setSavedSuccess] = useState(false);

  // 图像放大控制 State
  const [isZoomOpen, setIsZoomOpen] = useState(false);
  const [zoomScale, setZoomScale] = useState(1);
  const [panPos, setPanPos] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });

  React.useEffect(() => {
    if (robot) {
      setGuidelines(robot.guidelines || '');
    }
  }, [robot]);

  if (!isOpen || !robot) return null;

  const handleOpenZoom = () => {
    if (robot.avatarDataUrl) {
      setZoomScale(1);
      setPanPos({ x: 0, y: 0 });
      setIsZoomOpen(true);
    }
  };

  const handleWheel = (e: React.WheelEvent) => {
    e.stopPropagation();
    const delta = e.deltaY < 0 ? 0.15 : -0.15;
    setZoomScale(prev => Math.min(4, Math.max(0.5, prev + delta)));
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    if (zoomScale > 1) {
      setIsDragging(true);
      setDragStart({ x: e.clientX - panPos.x, y: e.clientY - panPos.y });
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (isDragging) {
      setPanPos({ x: e.clientX - dragStart.x, y: e.clientY - dragStart.y });
    }
  };

  const handleMouseUp = () => setIsDragging(false);

  const handleSaveGuidelines = async () => {
    setIsSavingGuidelines(true);
    try {
      await bridge.invoke('updateRobotGuidelines', {
        robotId: robot.id,
        guidelines: guidelines.trim(),
      });
      setSavedSuccess(true);
      setTimeout(() => setSavedSuccess(false), 2000);
    } catch (e) {
      console.error(e);
    } finally {
      setIsSavingGuidelines(false);
    }
  };

  const handleFocusOnDesktop = async () => {
    await bridge.invoke('focusRobot', { robotId: robot.id });
  };

  const handleRemoveRobot = async () => {
    if (confirm(`确定要从桌面移除机器人【${robot.name}】吗？`)) {
      await bridge.invoke('removeRobot', { robotId: robot.id });
      onClose();
    }
  };

  const skillsList: SkillInfo[] = robot.skills ? Object.values(robot.skills) : [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4 animate-in fade-in duration-200 select-none">
      <div className="bg-zinc-900 border border-zinc-800 w-full max-w-2xl rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Modal Header */}
        <div className="px-6 py-4 bg-zinc-950 border-b border-zinc-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div
              onClick={handleOpenZoom}
              className={`w-12 h-12 rounded-xl flex items-center justify-center font-bold text-zinc-950 text-base shadow-lg shrink-0 border border-white/20 overflow-hidden ${
                robot.avatarDataUrl ? 'cursor-pointer hover:scale-105 transition-transform' : ''
              }`}
              style={{ backgroundColor: robot.colorHex || '#10B981' }}
              title={robot.avatarDataUrl ? '点击全屏放大立绘' : robot.name}
            >
              {robot.avatarDataUrl ? (
                <img src={robot.avatarDataUrl} alt={robot.name} className="w-full h-full object-contain p-0.5" />
              ) : (
                robot.name.slice(0, 2)
              )}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-base font-bold text-zinc-100 flex items-center gap-2">
                  {robot.name}
                </h2>
                <span className="text-xs px-2 py-0.5 bg-zinc-800 text-emerald-400 font-mono rounded-md border border-zinc-700 font-bold">
                  LV.{robot.level}
                </span>
                <span className="text-xs px-2 py-0.5 bg-zinc-800 text-amber-300 rounded-md border border-zinc-700">
                  {robot.personality}
                </span>
                {robot.isWeaponMaster && (
                  <span className="text-xs px-2 py-0.5 bg-rose-950 text-rose-300 rounded-md border border-rose-800 flex items-center gap-1 font-bold">
                    ⚔️ 武器大师
                  </span>
                )}
              </div>
              <p className="text-xs text-zinc-400 font-mono mt-0.5">
                ID: {robot.id} • HP {robot.hp}/{robot.maxHp} • 速度 {robot.speedMultiplier}x
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 bg-zinc-800 hover:bg-zinc-700 text-zinc-400 hover:text-zinc-200 rounded-xl transition"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* 血量与经验 Progress Header Bar */}
        <div className="px-6 py-2.5 bg-zinc-950/60 border-b border-zinc-800 grid grid-cols-2 gap-4">
          <div>
            <div className="flex justify-between text-[11px] text-zinc-400 font-mono mb-1">
              <span>生命值 (HP)</span>
              <span className="text-emerald-400 font-bold">{robot.hp} / {robot.maxHp}</span>
            </div>
            <div className="w-full h-2 bg-zinc-950 rounded-full overflow-hidden border border-zinc-800">
              <div
                className="h-full bg-emerald-500 transition-all duration-300"
                style={{ width: `${Math.max(0, Math.min(100, (robot.hp / robot.maxHp) * 100))}%` }}
              />
            </div>
          </div>

          <div>
            <div className="flex justify-between text-[11px] text-zinc-400 font-mono mb-1">
              <span>等级经验 (XP)</span>
              <span className="text-amber-400 font-bold">{robot.exp} / {robot.maxExp}</span>
            </div>
            <div className="w-full h-2 bg-zinc-950 rounded-full overflow-hidden border border-zinc-800">
              <div
                className="h-full bg-gradient-to-r from-amber-500 to-emerald-400 transition-all duration-300"
                style={{ width: `${Math.max(0, Math.min(100, (robot.exp / robot.maxExp) * 100))}%` }}
              />
            </div>
          </div>
        </div>

        {/* Modal Navigation Tabs */}
        <div className="flex border-b border-zinc-800 px-6 bg-zinc-950/40">
          <button
            onClick={() => setActiveTab('brain')}
            className={`px-4 py-2.5 text-xs font-bold flex items-center gap-2 border-b-2 transition ${
              activeTab === 'brain'
                ? 'border-emerald-500 text-emerald-400'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Brain className="w-3.5 h-3.5" />
            AI 人设与立绘形象
          </button>
          <button
            onClick={() => setActiveTab('skills')}
            className={`px-4 py-2.5 text-xs font-bold flex items-center gap-2 border-b-2 transition ${
              activeTab === 'skills'
                ? 'border-emerald-500 text-emerald-400'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <Award className="w-3.5 h-3.5" />
            技能四维矩阵 ({skillsList.length})
          </button>
          <button
            onClick={() => setActiveTab('phrases')}
            className={`px-4 py-2.5 text-xs font-bold flex items-center gap-2 border-b-2 transition ${
              activeTab === 'phrases'
                ? 'border-emerald-500 text-emerald-400'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <BookOpen className="w-3.5 h-3.5" />
            口头禅与性格
          </button>
        </div>

        {/* Modal Main Content Area */}
        <div className="flex-1 overflow-y-auto p-6 space-y-4 font-sans">
          {/* Tab 1: AI 人设与形象呈现 */}
          {activeTab === 'brain' && (
            <div className="space-y-4">
              {/* 角色外观特写 Showcase 卡片 */}
              <div className="p-4 bg-zinc-950 rounded-xl border border-zinc-800 flex flex-col md:flex-row items-center gap-4">
                <div
                  onClick={handleOpenZoom}
                  className={`w-24 h-24 rounded-xl bg-zinc-900 border border-zinc-700/80 flex items-center justify-center relative overflow-hidden shrink-0 shadow-inner group ${
                    robot.avatarDataUrl ? 'cursor-pointer' : ''
                  }`}
                  style={{
                    backgroundImage: `radial-gradient(#27272a 1px, transparent 1px)`,
                    backgroundSize: '10px 10px'
                  }}
                  title={robot.avatarDataUrl ? '点击全屏放大查看' : ''}
                >
                  {robot.avatarDataUrl ? (
                    <>
                      <img
                        src={robot.avatarDataUrl}
                        alt={robot.name}
                        className="w-full h-full object-contain p-1 group-hover:scale-110 transition-transform duration-300"
                      />
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <ZoomIn className="w-6 h-6 text-white drop-shadow-md animate-bounce" />
                      </div>
                    </>
                  ) : (
                    <div className="flex flex-col items-center justify-center text-center p-2 text-zinc-500">
                      <div
                        className="w-10 h-10 rounded-xl flex items-center justify-center font-bold text-zinc-950 text-xs mb-1 shadow"
                        style={{ backgroundColor: robot.colorHex || '#10B981' }}
                      >
                        {robot.name.slice(0, 2)}
                      </div>
                      <span className="text-[10px] font-mono">像素形态</span>
                    </div>
                  )}
                </div>

                <div className="flex-1 space-y-1.5 text-center md:text-left">
                  <div className="flex items-center justify-center md:justify-start gap-2">
                    <h3 className="text-xs font-bold text-zinc-100">{robot.name} 的外貌形象</h3>
                    {robot.avatarDataUrl ? (
                      <span className="text-[10px] text-emerald-400 bg-emerald-950 px-2 py-0.5 rounded-full border border-emerald-800 font-mono font-bold flex items-center gap-1">
                        🎨 SiliconFlow 抠图特写已应用
                      </span>
                    ) : (
                      <span className="text-[10px] text-amber-400 bg-amber-950 px-2 py-0.5 rounded-full border border-amber-800 font-mono">
                        👾 基础像素实体形态
                      </span>
                    )}
                  </div>

                  <p className="text-[11px] text-zinc-400">
                    {robot.avatarDataUrl
                      ? '已加载 AI 生成并完成绿幕自动抠图的 2D 专属角色立绘，桌宠以透明精灵平滑呈现。'
                      : '当前桌宠使用标准像素实体图形。你可以随时使用 AI 生成功能为其召唤专属立绘！'}
                  </p>

                  {robot.avatarDataUrl && (
                    <div className="pt-1 flex justify-center md:justify-start">
                      <button
                        onClick={handleOpenZoom}
                        className="px-3 py-1 bg-emerald-950 hover:bg-emerald-900 border border-emerald-700/60 rounded-lg text-xs font-bold text-emerald-400 flex items-center gap-1.5 transition shadow"
                      >
                        <ZoomIn className="w-3.5 h-3.5" />
                        🔍 放大查看全屏高清立绘
                      </button>
                    </div>
                  )}
                </div>
              </div>
              {/* 人设准则编辑器 */}
              <div className="bg-zinc-950 p-4 rounded-xl border border-zinc-800 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                    <Edit3 className="w-3.5 h-3.5" />
                    AI 核心设定与指导原则 (System Guidelines):
                  </span>
                  <button
                    onClick={handleSaveGuidelines}
                    disabled={isSavingGuidelines}
                    className="px-3 py-1 bg-emerald-950 hover:bg-emerald-900 border border-emerald-700/60 rounded-lg text-xs font-bold text-emerald-400 flex items-center gap-1 transition disabled:opacity-50"
                  >
                    {savedSuccess ? <Check className="w-3 h-3 text-emerald-400" /> : <Save className="w-3 h-3" />}
                    {savedSuccess ? '已保存！' : '保存更新'}
                  </button>
                </div>
                <textarea
                  value={guidelines}
                  onChange={e => setGuidelines(e.target.value)}
                  placeholder="在此输入机器人的人生信条、语言风格或特殊限制指令..."
                  rows={3}
                  className="w-full bg-zinc-900 border border-zinc-800 rounded-lg px-3 py-2 text-xs text-zinc-100 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50 font-mono resize-none"
                />
              </div>

              {/* 意识觉醒度与深刻见解 */}
              <div className="bg-zinc-950 p-4 rounded-xl border border-zinc-800 space-y-3">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-bold text-amber-300 flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5" />
                    AI 意识觉醒等级 (Consciousness Level):
                  </span>
                  <span className="font-mono font-bold text-amber-400">
                    LV.{robot.consciousnessLevel || 1}.0
                  </span>
                </div>

                <div>
                  <div className="text-xs font-bold text-zinc-300 mb-1.5">💡 自主进化领悟到的洞见 (Learned Insights):</div>
                  {robot.learnedInsights && robot.learnedInsights.length > 0 ? (
                    <div className="space-y-1.5">
                      {robot.learnedInsights.map((insight, i) => (
                        <div
                          key={i}
                          className="px-3 py-1.5 bg-zinc-900 border border-zinc-800 rounded-lg text-xs text-zinc-300 font-mono flex items-start gap-2"
                        >
                          <span className="text-amber-400 font-bold shrink-0">#{i + 1}</span>
                          <span>{insight}</span>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="p-3 bg-zinc-900/50 rounded-lg text-xs text-zinc-500 font-mono text-center">
                      暂未领悟独立洞见，随着桌宠在桌面的思考与对话，将自动积累意识！
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Tab 2: 技能四维矩阵 */}
          {activeTab === 'skills' && (
            <div className="space-y-3">
              <div className="text-xs text-zinc-400 font-mono mb-2">
                🤖 机器人通过互动、吐槽与战斗自动获得四维技能经验并升级：
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {skillsList.map((skill, idx) => (
                  <div
                    key={idx}
                    className="p-3 bg-zinc-950 border border-zinc-800 rounded-xl space-y-2"
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-xs font-bold text-zinc-200 flex items-center gap-1.5">
                        <Award className="w-3.5 h-3.5 text-amber-400" />
                        {skill.name}
                      </span>
                      <span className="text-[11px] px-1.5 py-0.5 bg-zinc-900 border border-zinc-700 text-amber-400 font-mono font-bold rounded">
                        LV.{skill.level}
                      </span>
                    </div>
                    <p className="text-[11px] text-zinc-400">{skill.description}</p>
                    <div>
                      <div className="flex justify-between text-[10px] text-zinc-500 font-mono mb-1">
                        <span>经验进度</span>
                        <span>{skill.experience} / {skill.nextLevelXp}</span>
                      </div>
                      <div className="w-full h-1.5 bg-zinc-900 rounded-full overflow-hidden border border-zinc-800">
                        <div
                          className="h-full bg-gradient-to-r from-amber-500 to-emerald-500"
                          style={{ width: `${Math.max(0, Math.min(100, (skill.experience / skill.nextLevelXp) * 100))}%` }}
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Tab 3: 口头禅与性格 */}
          {activeTab === 'phrases' && (
            <div className="space-y-3">
              <div className="bg-zinc-950 p-4 rounded-xl border border-zinc-800 space-y-2">
                <div className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                  <BookOpen className="w-3.5 h-3.5" />
                  专属口头禅与性格名言:
                </div>
                {robot.customPhrases && robot.customPhrases.length > 0 ? (
                  <div className="flex flex-wrap gap-2 pt-1">
                    {robot.customPhrases.map((phrase, i) => (
                      <span
                        key={i}
                        className="px-2.5 py-1 bg-zinc-900 border border-zinc-800 text-zinc-300 text-xs rounded-lg italic"
                      >
                        "{phrase}"
                      </span>
                    ))}
                  </div>
                ) : (
                  <div className="p-3 bg-zinc-900/50 rounded-lg text-xs text-zinc-500 font-mono text-center">
                    暂未绑定专属口头禅，将在 AI 生成与性格交互中自动习得。
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer Controls */}
        <div className="px-6 py-3 bg-zinc-950 border-t border-zinc-800 flex items-center justify-between gap-2">
          <div className="flex items-center gap-1.5">
            <button
              onClick={() => {
                onClose();
                onOpenPrivateChat(robot.id);
              }}
              className="px-3 py-1.5 bg-emerald-950 hover:bg-emerald-900 text-emerald-400 border border-emerald-700/60 rounded-xl text-xs font-bold flex items-center gap-1.5 transition"
            >
              <MessageSquare className="w-3.5 h-3.5" />
              单人私聊
            </button>
            <button
              onClick={handleFocusOnDesktop}
              className="px-3 py-1.5 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 border border-zinc-700 rounded-xl text-xs font-bold flex items-center gap-1.5 transition"
              title="在桌面上聚焦定位该机器人"
            >
              <Crosshair className="w-3.5 h-3.5 text-amber-400" />
              桌面定位
            </button>
            <button
              onClick={() => onInspirate(robot.id)}
              className="px-3 py-1.5 bg-amber-950 hover:bg-amber-900 text-amber-400 border border-amber-700/60 rounded-xl text-xs font-bold flex items-center gap-1.5 transition"
            >
              <Zap className="w-3.5 h-3.5" />
              启发灵感
            </button>
            <button
              onClick={() => onToggleCurse(robot.id, robot.curseMode)}
              className={`px-3 py-1.5 rounded-xl text-xs font-bold border flex items-center gap-1.5 transition ${
                robot.curseMode
                  ? 'bg-rose-950 text-rose-400 border-rose-700'
                  : 'bg-zinc-800 text-zinc-300 border-zinc-700 hover:text-zinc-100'
              }`}
            >
              <Flame className="w-3.5 h-3.5" />
              {robot.curseMode ? '吐槽开' : '吐槽关'}
            </button>
          </div>

          <button
            onClick={handleRemoveRobot}
            className="px-3 py-1.5 bg-rose-950/80 hover:bg-rose-900 text-rose-400 border border-rose-800 rounded-xl text-xs font-bold flex items-center gap-1.5 transition"
          >
            <Trash2 className="w-3.5 h-3.5" />
            召回/移除
          </button>
        </div>
      </div>

      {/* 全屏放大查看 Lightbox 弹窗 */}
      {isZoomOpen && robot.avatarDataUrl && (
        <div
          className="fixed inset-0 z-[100] flex flex-col items-center justify-between bg-black/90 backdrop-blur-md p-4 animate-in fade-in duration-200 select-none"
          onClick={() => setIsZoomOpen(false)}
        >
          {/* 顶部控制栏 */}
          <div
            className="w-full max-w-3xl flex items-center justify-between px-5 py-3 bg-zinc-950/90 border border-zinc-800 rounded-2xl shadow-2xl backdrop-blur-md z-10"
            onClick={e => e.stopPropagation()}
          >
            <div className="flex items-center gap-2">
              <span className="text-sm font-bold text-zinc-100 flex items-center gap-2">
                🎨 {robot.name} 2D高清透明立绘
              </span>
              <span className="text-xs px-2 py-0.5 bg-emerald-950 text-emerald-400 rounded-md border border-emerald-800 font-mono font-bold">
                {Math.round(zoomScale * 100)}% 缩放
              </span>
            </div>

            <div className="flex items-center gap-2">
              <button
                onClick={() => setZoomScale(prev => Math.min(4, prev + 0.25))}
                className="p-1.5 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 rounded-xl transition border border-zinc-700"
                title="放大 (+25%)"
              >
                <ZoomIn className="w-4 h-4" />
              </button>
              <button
                onClick={() => setZoomScale(prev => Math.max(0.5, prev - 0.25))}
                className="p-1.5 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 rounded-xl transition border border-zinc-700"
                title="缩小 (-25%)"
              >
                <ZoomOut className="w-4 h-4" />
              </button>
              <button
                onClick={() => {
                  setZoomScale(1);
                  setPanPos({ x: 0, y: 0 });
                }}
                className="p-1.5 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 rounded-xl transition border border-zinc-700"
                title="重置 100%"
              >
                <RotateCcw className="w-4 h-4" />
              </button>
              <div className="w-px h-4 bg-zinc-800 mx-1" />
              <button
                onClick={() => setIsZoomOpen(false)}
                className="p-1.5 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 rounded-xl transition"
                title="关闭"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* 图像显示与滚轮/拖拽区域 */}
          <div
            className="flex-1 w-full flex items-center justify-center overflow-hidden cursor-grab active:cursor-grabbing relative p-6"
            onWheel={handleWheel}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onClick={e => e.stopPropagation()}
          >
            <div
              className="relative transition-transform duration-75 flex items-center justify-center rounded-2xl p-4"
              style={{
                transform: `translate(${panPos.x}px, ${panPos.y}px) scale(${zoomScale})`,
                backgroundImage: `radial-gradient(#3f3f46 1.5px, transparent 1.5px)`,
                backgroundSize: '16px 16px'
              }}
            >
              <img
                src={robot.avatarDataUrl}
                alt={robot.name}
                className="max-w-[70vh] max-h-[70vh] object-contain drop-shadow-2xl pointer-events-none select-none"
              />
            </div>
          </div>

          {/* 底部操作提示 */}
          <div className="text-xs text-zinc-400 bg-zinc-950/90 px-4 py-1.5 rounded-full border border-zinc-800 font-mono z-10">
            💡 支持 鼠标滚轮缩放 • 鼠标按住拖拽移动 • 点击空白处退出
          </div>
        </div>
      )}
    </div>
  );
};
