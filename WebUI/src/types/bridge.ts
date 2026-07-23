export interface RobotInfo {
  id: string;
  name: string;
  personality: string;
  hp: number;
  maxHp: number;
  level: number;
  exp: number;
  maxExp: number;
  killCount: number;
  deathCount: number;
  isDead: boolean;
  isActive: boolean;
  isThinking: boolean;
  isAiSpeaking: boolean;
  curseMode: boolean;
  colorHex: string;
  chatText: string;
  isMoving: boolean;
  isVisible: boolean;
  size: number;
  speedMultiplier: number;
  posX: number;
  posY: number;
}

export interface SocialMessage {
  sender: string;
  content: string;
  time?: string;
  color?: string;
}

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
  thought?: string;
}

export interface SystemStats {
  onlineCount: number;
  totalRobots?: number;
  movingRobots?: number;
  pausedRobots?: number;
  battleMode: string;
  totalTokens: number;
  totalCostYuan: number;
}

export interface AppSettings {
  opacity: number;
  homeUrl: string;
  autoStart?: boolean;
  hideNameAndPersonality: boolean;
  curseModeByDefault: boolean;
  battleMode: string;
  languageMode: string;
  actionMode: string;
  robotSize?: number;
  robotSpeed?: number;
  skillScale?: number;
  soundVolume?: number;
  fightFrequency?: number;
  enableAiThinking?: boolean;
  aiThoughtFrequency?: number;
  isWeaponMaster?: boolean;
  robotMaxHp?: number;
  isGodMode?: boolean;
  apiKey: string;
  enabledWeapons?: string[];
}
