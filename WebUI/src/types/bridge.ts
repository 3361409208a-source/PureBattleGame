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
  battleMode: string;
  totalTokens: number;
  totalCostYuan: number;
}
