'use client';

import React, { useState, useRef, useEffect } from 'react';
import { Sparkles, Loader2, X, CornerDownLeft } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { api, ChatCommandResponse, DashboardSummary } from '@/lib/api';

interface QuickCommandBarProps {
  onRefresh: () => void;
}

export function QuickCommandBar({ onRefresh }: QuickCommandBarProps) {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<ChatCommandResponse | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Keyboard shortcut '/' or Ctrl+K to focus
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.key === '/' && document.activeElement !== inputRef.current) ||
          ((e.ctrlKey || e.metaKey) && e.key === 'k')) {
        e.preventDefault();
        inputRef.current?.focus();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || loading) return;

    setLoading(true);
    try {
      const res = await api.sendCommand(input.trim());
      setLastResponse(res);
      setInput('');
      onRefresh();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha na conexão';
      setLastResponse({
        reply: `❌ Erro ao processar comando: ${message}`,
        intent: 'ERROR',
        dashboard: null as unknown as DashboardSummary,
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="w-full max-w-4xl mx-auto mb-6 sm:mb-8">
      <form onSubmit={handleSubmit} className="relative">
        <div className="relative flex items-center bg-zinc-900/90 border border-zinc-700/80 rounded-2xl shadow-2xl backdrop-blur-md px-3.5 sm:px-4 py-2 transition-all duration-200 focus-within:border-emerald-500/80 focus-within:ring-2 focus-within:ring-emerald-500/20">
          <div className="flex items-center gap-2 mr-2.5 sm:mr-3 text-emerald-400 shrink-0">
            <Sparkles className="w-5 h-5 animate-pulse" />
            <span className="hidden sm:inline-block text-xs font-semibold uppercase tracking-wider bg-emerald-500/10 text-emerald-400 px-2 py-0.5 rounded-full border border-emerald-500/20">
              IA / Natural
            </span>
          </div>

          <input
            ref={inputRef}
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Ex: 'uber 25', 'pix 40 corte', '/status'..."
            className="w-full min-h-[44px] bg-transparent text-base sm:text-sm text-zinc-100 placeholder-zinc-500 focus:outline-none"
            disabled={loading}
          />

          <div className="flex items-center gap-2 ml-2 shrink-0">
            {!input && (
              <span className="hidden sm:inline-flex items-center gap-1 text-[11px] font-mono text-zinc-500 bg-zinc-800/80 px-2 py-0.5 rounded border border-zinc-700">
                press <kbd>/</kbd>
              </span>
            )}

            <button
              type="submit"
              disabled={loading || !input.trim()}
              className="min-h-[44px] min-w-[44px] flex items-center justify-center p-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 disabled:bg-zinc-800 disabled:text-zinc-600 text-zinc-950 font-medium transition-all shadow-md cursor-pointer disabled:cursor-not-allowed"
            >
              {loading ? (
                <Loader2 className="w-4 h-4 animate-spin text-zinc-400" />
              ) : (
                <CornerDownLeft className="w-4 h-4" />
              )}
            </button>
          </div>
        </div>
      </form>

      {/* Assistant Reply Card */}
      <AnimatePresence>
        {lastResponse && (
          <motion.div
            initial={{ opacity: 0, y: -10, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -10, scale: 0.98 }}
            transition={{ duration: 0.2 }}
            className={`mt-3 p-4 rounded-xl border backdrop-blur-md shadow-lg flex items-start justify-between gap-3 ${
              lastResponse.intent === 'ERROR'
                ? 'bg-rose-950/40 border-rose-800/60 text-rose-200'
                : 'bg-zinc-900/90 border-emerald-500/30 text-zinc-200'
            }`}
          >
            <div className="flex items-start gap-3">
              <span className="text-xl">
                {lastResponse.intent === 'ADD_EXPENSE' ? '💳' :
                 lastResponse.intent === 'SIMULATE' ? '🔮' :
                 lastResponse.intent === 'STATUS' ? '📊' :
                 lastResponse.intent === 'SYNC_INVOICE' ? '🔄' :
                 lastResponse.intent === 'UPDATE_CAIXINHA' ? '🐷' :
                 lastResponse.intent === 'UNDO' ? '↩️' : '🤖'}
              </span>
              <div className="whitespace-pre-line text-sm leading-relaxed font-sans">
                {lastResponse.reply}
              </div>
            </div>
            <button
              onClick={() => setLastResponse(null)}
              className="text-zinc-500 hover:text-zinc-300 p-1 rounded-lg hover:bg-zinc-800/50 transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
