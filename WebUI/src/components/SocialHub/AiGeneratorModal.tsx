import React, { useState, useEffect } from 'react';
import { Bot, Sparkles, CheckCircle2, AlertCircle, Loader2, X, Wand2, Image as ImageIcon } from 'lucide-react';
import { bridge } from '../../utils/bridge';

interface AiGeneratedConfig {
  name: string;
  personality: string;
  guidelines: string;
  color: string;
  isWeaponMaster: boolean;
  avatarPath?: string;
  weapons?: string[];
}

interface ProgressState {
  percent: number;
  step: number;
  message: string;
  error?: boolean;
  completed?: boolean;
}

interface AiGeneratorModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

const PRESETS = [
  { label: '⚡ 加入赛罗', prompt: '加入赛罗' },
  { label: '🦸 10个奥特曼成员', prompt: '加入十个奥特曼成员' },
  { label: '⚔️ 三国五虎上将', prompt: '生成五个三国武将' },
  { label: '🛡️ 复仇者联盟', prompt: '召唤四个复仇者成员' },
  { label: '🔥 4元素法师', prompt: '生成四个元素法师' },
];

export const AiGeneratorModal: React.FC<AiGeneratorModalProps> = ({ isOpen, onClose, onSuccess }) => {
  const [prompt, setPrompt] = useState('加入赛罗');
  const [enableImageGen, setEnableImageGen] = useState(true);
  const [imageModel, setImageModel] = useState('Kwai-Kolors/Kolors');
  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState<ProgressState>({
    percent: 0,
    step: 0,
    message: '',
  });
  const [generatedConfigs, setGeneratedConfigs] = useState<AiGeneratedConfig[]>([]);

  useEffect(() => {
    const unsub = bridge.on('aiGenerateProgress', (data: ProgressState) => {
      setProgress(data);
    });
    return () => unsub();
  }, []);

  if (!isOpen) return null;

  const handleStartGeneration = async () => {
    if (!prompt.trim() || isGenerating) return;

    setIsGenerating(true);
    setGeneratedConfigs([]);
    setProgress({ percent: 10, step: 1, message: '🚀 准备发起大模型智能生成...' });

    try {
      const res = await bridge.invoke<{
        success: boolean;
        message?: string;
        count?: number;
        configs?: AiGeneratedConfig[];
      }>('generateAiRobots', { prompt: prompt.trim(), enableImageGen, imageModel });

      if (res && res.success && res.configs) {
        setGeneratedConfigs(res.configs);
        onSuccess();
      } else {
        setProgress(prev => ({
          ...prev,
          percent: 100,
          error: true,
          message: res?.message || '❌ 生成失败，未获得有效配置',
        }));
      }
    } catch (e: any) {
      setProgress({
        percent: 100,
        step: 4,
        error: true,
        message: `❌ 请求异常: ${e?.message || e}`,
      });
    } finally {
      setIsGenerating(false);
    }
  };

  const steps = [
    { num: 1, label: '建立 API 连接', desc: '连接 SiliconFlow 大模型' },
    { num: 2, label: 'LLM 语义拆解', desc: '提取角色设定与性格' },
    { num: 3, label: '生图与自动抠图', desc: 'Kwai-Kolors绿幕抠图' },
    { num: 4, label: '实例化投放', desc: '生成桌宠并降临桌面' },
  ];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4 animate-in fade-in duration-200 select-none">
      <div className="bg-zinc-900 border border-zinc-800 rounded-2xl w-full max-w-xl shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-3.5 bg-zinc-950 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-emerald-950 border border-emerald-600/50 rounded-xl text-emerald-400">
              <Bot className="w-5 h-5 animate-pulse" />
            </div>
            <div>
              <h2 className="text-sm font-bold text-emerald-400 flex items-center gap-1.5">
                🤖 AI 自然语言智能生成机器人
              </h2>
              <p className="text-[11px] text-zinc-400">输入任意指令，AI 自动拆解并生成专属像素桌面宠物</p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={isGenerating}
            className="p-1.5 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 rounded-lg transition disabled:opacity-50"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-5 space-y-4 max-h-[75vh] overflow-y-auto">
          {/* 指令输入 */}
          <div>
            <label className="block text-xs font-bold text-zinc-200 mb-1.5 flex items-center justify-between">
              <span>💬 请输入生成指令：</span>
              <span className="text-[11px] font-normal text-zinc-400">支持批量生成（如“生成五个三国武将”）</span>
            </label>
            <textarea
              rows={2}
              value={prompt}
              onChange={e => setPrompt(e.target.value)}
              disabled={isGenerating}
              placeholder="例如：“加入赛罗”、“生成五个三国武将”、“召唤四个复仇者”..."
              className="w-full bg-zinc-950 border border-zinc-800 rounded-xl px-3.5 py-2 text-xs text-zinc-100 placeholder-zinc-500 focus:outline-none focus:border-emerald-500/50 resize-none font-mono"
            />
          </div>

          {/* 预设快捷推荐 */}
          <div>
            <div className="text-[11px] text-zinc-400 mb-1.5 font-semibold">💡 快捷指令推荐：</div>
            <div className="flex flex-wrap gap-1.5">
              {PRESETS.map((p, i) => (
                <button
                  key={i}
                  onClick={() => setPrompt(p.prompt)}
                  disabled={isGenerating}
                  className="px-2.5 py-1 bg-zinc-950 hover:bg-zinc-800 border border-zinc-800 hover:border-emerald-600/50 rounded-lg text-xs text-zinc-300 hover:text-emerald-400 transition"
                >
                  {p.label}
                </button>
              ))}
            </div>
          </div>

          {/* SiliconFlow AI 生图与纯绿幕抠图配置 */}
          <div className="p-3 bg-zinc-950/80 rounded-xl border border-zinc-800 space-y-2">
            <label className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={enableImageGen}
                onChange={e => setEnableImageGen(e.target.checked)}
                disabled={isGenerating}
                className="w-4 h-4 rounded border-zinc-700 bg-zinc-900 text-emerald-500 focus:ring-emerald-500/30"
              />
              <span className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                <ImageIcon className="w-3.5 h-3.5" />
                同步开启 SiliconFlow 生图与纯绿幕自动抠图
              </span>
            </label>

            {enableImageGen && (
              <div className="space-y-1.5 pl-6 pt-1">
                <div className="flex items-center gap-2">
                  <span className="text-[11px] text-zinc-400 font-mono">生图模型：</span>
                  <select
                    value={imageModel}
                    onChange={e => setImageModel(e.target.value)}
                    disabled={isGenerating}
                    className="bg-zinc-900 border border-zinc-700 rounded-lg px-2.5 py-1 text-xs text-amber-300 font-mono focus:outline-none focus:border-emerald-500/50"
                  >
                    <option value="Kwai-Kolors/Kolors">Kwai-Kolors/Kolors (首选 - 角色生成优选)</option>
                    <option value="black-forest-labs/FLUX.1-schnell">black-forest-labs/FLUX.1-schnell</option>
                    <option value="stabilityai/stable-diffusion-3-5-large">stabilityai/stable-diffusion-3-5-large</option>
                  </select>
                </div>
                <div className="text-[10px] text-zinc-400">
                  💡 提示：SiliconFlow 生图 API (Kwai-Kolors) 需 API Key 保持充值余额。若余额不足 (Code 30001) 将自动降级生成像素形态桌宠。
                </div>
              </div>
            )}
          </div>

          {/* 过程进度展示 (Progress & Step Timeline) */}
          {(isGenerating || progress.percent > 0) && (
            <div className="bg-zinc-950 p-4 rounded-xl border border-zinc-800 space-y-3">
              {/* 进度条 Header */}
              <div className="flex items-center justify-between text-xs">
                <span className="font-bold text-zinc-200 flex items-center gap-1.5">
                  {isGenerating && <Loader2 className="w-3.5 h-3.5 text-emerald-400 animate-spin" />}
                  {progress.completed && <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />}
                  {progress.error && <AlertCircle className="w-3.5 h-3.5 text-rose-400" />}
                  生成进度展示
                </span>
                <span className="font-mono font-bold text-emerald-400">{progress.percent}%</span>
              </div>

              {/* 动态进度条 */}
              <div className="w-full h-2 bg-zinc-900 rounded-full overflow-hidden border border-zinc-800 relative">
                <div
                  className={`h-full transition-all duration-300 ${
                    progress.error ? 'bg-rose-500' : 'bg-gradient-to-r from-emerald-600 to-amber-500 shadow-lg shadow-emerald-500/50'
                  }`}
                  style={{ width: `${progress.percent}%` }}
                />
              </div>

              {/* 步骤 Timeline 图标节点 */}
              <div className="grid grid-cols-4 gap-1 pt-1">
                {steps.map(s => {
                  const isCurrent = progress.step === s.num && isGenerating;
                  const isDone = progress.step > s.num || progress.completed;
                  return (
                    <div
                      key={s.num}
                      className={`p-2 rounded-lg border text-center transition ${
                        isDone
                          ? 'bg-emerald-950/60 border-emerald-700/60 text-emerald-400'
                          : isCurrent
                          ? 'bg-amber-950/60 border-amber-600 text-amber-400 animate-pulse'
                          : 'bg-zinc-900 border-zinc-800 text-zinc-500'
                      }`}
                    >
                      <div className="text-[10px] font-mono font-bold mb-0.5">步骤 {s.num}</div>
                      <div className="text-xs font-bold truncate">{s.label}</div>
                    </div>
                  );
                })}
              </div>

              {/* 步骤实时提示消息 */}
              {progress.message && (
                <div className={`p-2.5 rounded-lg border text-xs font-mono ${
                  progress.error
                    ? 'bg-rose-950/80 border-rose-800 text-rose-300'
                    : progress.completed
                    ? 'bg-emerald-950/80 border-emerald-800 text-emerald-300'
                    : 'bg-zinc-900 border-zinc-800 text-amber-300'
                }`}>
                  {progress.message}
                </div>
              )}
            </div>
          )}

          {/* 生成结果预览卡片 */}
          {generatedConfigs.length > 0 && (
            <div className="bg-zinc-950 p-3 rounded-xl border border-zinc-800 space-y-2">
              <div className="text-xs font-bold text-emerald-400 flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5" />
                已生成并投放的专属机器人 ({generatedConfigs.length} 个):
              </div>
              <div className="space-y-2">
                {generatedConfigs.map((cfg, idx) => (
                  <div
                    key={idx}
                    className="p-2.5 bg-zinc-900 border border-zinc-800 rounded-lg space-y-1.5"
                  >
                    {/* 角色基本信息行 */}
                    <div className="flex items-center gap-2 flex-wrap">
                      <span
                        className="w-3 h-3 rounded-full shrink-0"
                        style={{ backgroundColor: cfg.color || '#10B981' }}
                      />
                      <span className="font-bold text-zinc-100 text-xs">{cfg.name}</span>
                      <span className="text-[10px] px-1.5 py-0.5 bg-zinc-800 text-amber-400 rounded font-mono">
                        {cfg.personality}
                      </span>
                      {cfg.isWeaponMaster && (
                        <span className="text-[10px] text-rose-400 font-semibold">⚔️ 武器大师</span>
                      )}
                      {cfg.avatarPath && (
                        <span className="text-[10px] text-emerald-400 bg-emerald-950/80 px-1 rounded border border-emerald-800 flex items-center gap-0.5">🎨 抠图已生成</span>
                      )}
                    </div>
                    {/* 专属技能列表 */}
                    {cfg.weapons && cfg.weapons.length > 0 && (
                      <div className="flex flex-wrap gap-1">
                        <span className="text-[10px] text-zinc-500 font-mono mr-0.5">⚡技能:</span>
                        {cfg.weapons.slice(0, 5).map((w, wi) => (
                          <span
                            key={wi}
                            className="text-[10px] px-1.5 py-0.5 bg-indigo-950/80 border border-indigo-700/50 text-indigo-300 rounded font-mono"
                          >
                            {w}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer */}
        <div className="flex items-center justify-end gap-2 px-5 py-3 bg-zinc-950 border-t border-zinc-800">
          <button
            onClick={onClose}
            disabled={isGenerating}
            className="px-4 py-1.5 bg-zinc-800 hover:bg-zinc-700 text-zinc-300 rounded-xl text-xs font-semibold transition disabled:opacity-50"
          >
            {progress.completed ? '完成并关闭' : '取消'}
          </button>

          <button
            onClick={handleStartGeneration}
            disabled={isGenerating || !prompt.trim()}
            className="px-5 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold rounded-xl text-xs flex items-center gap-1.5 shadow-md transition disabled:opacity-50"
          >
            {isGenerating ? (
              <>
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
                智能生成中...
              </>
            ) : (
              <>
                <Wand2 className="w-3.5 h-3.5" />
                ✨ 智能生成并投放
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
