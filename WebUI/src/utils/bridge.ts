declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: any) => void;
        addEventListener: (type: string, listener: (event: any) => void) => void;
        removeEventListener: (type: string, listener: (event: any) => void) => void;
      };
    };
  }
}

type EventListener = (data: any) => void;

class NativeBridge {
  private callbacks: Map<string, { resolve: (val: any) => void; reject: (err: any) => void }> = new Map();
  private eventListeners: Map<string, Set<EventListener>> = new Map();
  private callbackIdCounter = 0;

  constructor() {
    if (window.chrome?.webview) {
      window.chrome.webview.addEventListener('message', this.handleNativeMessage.bind(this));
    }
  }

  private handleNativeMessage(event: any) {
    const message = event.data;
    if (!message) return;

    if (message.type === 'response' && message.callbackId) {
      const pending = this.callbacks.get(message.callbackId);
      if (pending) {
        this.callbacks.delete(message.callbackId);
        if (message.success) {
          pending.resolve(message.result);
        } else {
          pending.reject(new Error(message.error || 'Unknown native error'));
        }
      }
    } else if (message.type === 'event' && message.eventName) {
      const listeners = this.eventListeners.get(message.eventName);
      if (listeners) {
        listeners.forEach(fn => fn(message.data));
      }
    }
  }

  public invoke<T = any>(action: string, payload?: any): Promise<T> {
    return new Promise((resolve, reject) => {
      const callbackId = `cb_${++this.callbackIdCounter}_${Date.now()}`;
      this.callbacks.set(callbackId, { resolve, reject });

      if (window.chrome?.webview) {
        window.chrome.webview.postMessage({
          action,
          payload,
          callbackId
        });
      } else {
        console.warn(`[Bridge] window.chrome.webview not available. Mocking action: ${action}`);
        // 研发模式下的 Mock 数据支持
        setTimeout(() => resolve(null as unknown as T), 100);
      }
    });
  }

  public on(eventName: string, listener: EventListener): () => void {
    if (!this.eventListeners.has(eventName)) {
      this.eventListeners.set(eventName, new Set());
    }
    this.eventListeners.get(eventName)!.add(listener);

    return () => {
      const set = this.eventListeners.get(eventName);
      if (set) {
        set.delete(listener);
      }
    };
  }
}

export const bridge = new NativeBridge();
