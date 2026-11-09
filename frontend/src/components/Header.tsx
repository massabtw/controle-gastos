'use client';

import React from 'react';
import { ChevronLeft, ChevronRight, RotateCcw, Plus, Settings2, Flame, ShieldCheck, CreditCard, Send, ArrowDownLeft, Lock } from 'lucide-react';
import { DashboardSummary } from '@/lib/api';

interface HeaderProps {
  summary: DashboardSummary | null;
  selectedCycle: string;
  onCycleChange: (cycle: string) => void;
  onOpenNewExpense: () => void;
  onOpenPixCaixinha?: () => void;
  onOpenReceivePix?: () => void;
  onOpenSyncInvoice?: () => void;
  onOpenSettings: () => void;
  onOpenPinModal?: () => void;
}

export function Header({
  summary,
  selectedCycle,
  onCycleChange,
  onOpenNewExpense,
  onOpenPixCaixinha,
  onOpenReceivePix,
  onOpenSyncInvoice,
  onOpenSettings,
  onOpenPinModal,
}: HeaderProps) {
  // Helper to step cycles
  const handlePrevCycle = () => {
    if (!selectedCycle) return;
    const [year, month] = selectedCycle.split('-').map(Number);
    const prevDate = new Date(year, month - 2, 1);
    const prevCycle = `${prevDate.getFullYear()}-${String(prevDate.getMonth() + 1).padStart(2, '0')}`;
    onCycleChange(prevCycle);
  };

  const handleNextCycle = () => {
    if (!selectedCycle) return;
    const [year, month] = selectedCycle.split('-').map(Number);
    const nextDate = new Date(year, month, 1);
    const nextCycle = `${nextDate.getFullYear()}-${String(nextDate.getMonth() + 1).padStart(2, '0')}`;
    onCycleChange(nextCycle);
  };

  const isCurrent = summary?.cycleState === 'CURRENT';

  return (
    <header className="w-full flex flex-col md:flex-row items-center justify-between gap-4 py-6 border-b border-zinc-800/80 mb-8">
      {/* Brand & North Star Info */}
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-emerald-500 to-emerald-700 flex items-center justify-center shadow-lg shadow-emerald-500/20 text-zinc-950">
          <Flame className="w-6 h-6 fill-zinc-950" />
        </div>
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-bold tracking-tight text-zinc-100">BurnTracker</h1>
            <span className="text-[11px] font-mono uppercase bg-zinc-800 text-zinc-400 px-2 py-0.5 rounded border border-zinc-700">
              v1.0
            </span>
          </div>
          <p className="text-xs text-zinc-400 flex items-center gap-1.5 mt-0.5">
            <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
            Estratégia de Fluxo Mensal & Teto R$ 1.030
          </p>
        </div>
      </div>

      {/* Cycle Switcher Navigator */}
      <div className="flex items-center gap-2 bg-zinc-900/90 border border-zinc-800 rounded-2xl p-1.5 shadow-md">
        <button
          onClick={handlePrevCycle}
          title="Ciclo Anterior"
          className="p-1.5 rounded-xl text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 transition-colors"
        >
          <ChevronLeft className="w-5 h-5" />
        </button>

        <div className="flex flex-col items-center px-4">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm sm:text-base text-zinc-100 tracking-wide font-mono">
              Ciclo {selectedCycle}
            </span>
            {summary?.isInvoicePaid ? (
              <span className="text-[10px] font-semibold uppercase bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-1.5 py-0.2 rounded-md">
                Paga ✅
              </span>
            ) : summary?.cycleState === 'CURRENT' ? (
              <span className="text-[10px] font-semibold uppercase bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-1.5 py-0.2 rounded-md">
                Aberto
              </span>
            ) : summary?.cycleState === 'CLOSED' ? (
              <span className="text-[10px] font-semibold uppercase bg-zinc-800 text-zinc-400 border border-zinc-700 px-1.5 py-0.2 rounded-md">
                Fechado
              </span>
            ) : (
              <span className="text-[10px] font-semibold uppercase bg-indigo-500/20 text-indigo-400 border border-indigo-500/30 px-1.5 py-0.2 rounded-md">
                Projeção
              </span>
            )}
          </div>
          <span className="text-[11px] text-zinc-400 font-sans">
            {summary?.isInvoicePaid && summary.invoicePaidAt
              ? `Quitada em ${new Date(summary.invoicePaidAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })} • Venceu/Vence dia 16`
              : 'Fecha dia 08 • Vence dia 16'}
          </span>
        </div>

        <button
          onClick={handleNextCycle}
          title="Próximo Ciclo"
          className="p-1.5 rounded-xl text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 transition-colors"
        >
          <ChevronRight className="w-5 h-5" />
        </button>

        {!isCurrent && (
          <button
            onClick={() => onCycleChange('')}
            title="Voltar ao ciclo atual"
            className="ml-1 p-1.5 rounded-xl text-emerald-400 hover:bg-emerald-500/10 border border-emerald-500/20 text-xs flex items-center gap-1 font-medium px-2.5 transition-colors"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span className="hidden sm:inline">Atual</span>
          </button>
        )}
      </div>

      {/* Action Buttons */}
      <div className="flex items-center gap-2">
        {onOpenReceivePix && (
          <button
            onClick={onOpenReceivePix}
            title="Registrar recebimento de Pix (vai diretamente para a Caixinha Nubank)"
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-emerald-950/40 hover:bg-emerald-900/50 border border-emerald-500/40 text-emerald-200 text-xs sm:text-sm font-medium transition-colors shadow-sm cursor-pointer"
          >
            <ArrowDownLeft className="w-4 h-4 text-emerald-400" />
            <span className="hidden sm:inline">Pix Recebido</span>
          </button>
        )}

        {onOpenPixCaixinha && (
          <button
            onClick={onOpenPixCaixinha}
            title="Retirar dinheiro da Caixinha para pagar no Pix"
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-cyan-950/40 hover:bg-cyan-900/50 border border-cyan-500/40 text-cyan-200 text-xs sm:text-sm font-medium transition-colors shadow-sm cursor-pointer"
          >
            <Send className="w-4 h-4 text-cyan-400" />
            <span className="hidden sm:inline">Retirar p/ Pix</span>
          </button>
        )}

        {onOpenSyncInvoice && (
          <button
            onClick={onOpenSyncInvoice}
            title="Ajustar fatura com base no app Nubank"
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-purple-950/40 hover:bg-purple-900/50 border border-purple-500/40 text-purple-200 text-xs sm:text-sm font-medium transition-colors shadow-sm"
          >
            <CreditCard className="w-4 h-4 text-purple-400" />
            <span className="hidden sm:inline">Ajustar Fatura</span>
          </button>
        )}

        {onOpenPinModal && (
          <button
            onClick={onOpenPinModal}
            title="Configurar PIN de Segurança"
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-zinc-400 hover:text-zinc-200 text-sm font-medium transition-colors shadow-sm cursor-pointer"
          >
            <Lock className="w-4 h-4 text-zinc-400" />
            <span className="hidden sm:inline">PIN</span>
          </button>
        )}

        <button
          onClick={onOpenSettings}
          className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-zinc-300 hover:text-zinc-100 text-sm font-medium transition-colors shadow-sm"
        >
          <Settings2 className="w-4 h-4 text-zinc-400" />
          <span className="hidden sm:inline">Regras</span>
        </button>

        <button
          onClick={onOpenNewExpense}
          className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-sm font-semibold transition-all shadow-md shadow-emerald-500/20 hover:scale-[1.02] active:scale-[0.98]"
        >
          <Plus className="w-4 h-4 stroke-[2.5]" />
          <span>Novo Gasto</span>
        </button>
      </div>
    </header>
  );
}
