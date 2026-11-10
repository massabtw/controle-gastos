'use client';

import React from 'react';
import { ChevronLeft, ChevronRight, RotateCcw, Plus, Settings2, Flame, ShieldCheck, CreditCard, Send, ArrowDownLeft, Lock, Unlock } from 'lucide-react';
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
  isAdmin?: boolean;
  isGuest?: boolean;
  onLogoutAdmin?: () => void;
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
  isAdmin = false,
  isGuest = false,
  onLogoutAdmin,
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
    <header className="w-full flex flex-col lg:flex-row lg:items-center lg:justify-between gap-3.5 py-4 sm:py-6 border-b border-zinc-800/80 mb-5 sm:mb-8">
      {/* Top Row on Mobile: Brand & Utility Icons */}
      <div className="w-full lg:w-auto flex items-center justify-between gap-3">
        <div className="flex items-center gap-3 min-w-0">
          <div className="w-10 h-10 shrink-0 rounded-2xl bg-gradient-to-br from-emerald-500 to-emerald-700 flex items-center justify-center shadow-lg shadow-emerald-500/20 text-zinc-950">
            <Flame className="w-6 h-6 fill-zinc-950" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="text-lg sm:text-xl font-bold tracking-tight text-zinc-100 truncate">BurnTracker</h1>
              <span className="text-[10px] sm:text-[11px] font-mono uppercase bg-zinc-800 text-zinc-400 px-1.5 py-0.5 rounded border border-zinc-700 shrink-0">
                v1.0
              </span>
            </div>
            <p className="text-[11px] sm:text-xs text-zinc-400 flex items-center gap-1.5 mt-0.5 truncate">
              <ShieldCheck className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
              <span className="truncate">
                Fluxo Mensal & <span className="privacy-sensitive font-mono">Teto R$ {summary ? summary.cardHardCap.toFixed(0) : '1.030'}</span>
              </span>
            </p>
          </div>
        </div>

        {/* Mobile quick utility buttons: Admin/Visitante Lock Status, Regras */}
        <div className="flex lg:hidden items-center gap-1.5 shrink-0">
          <button
            onClick={isAdmin ? onLogoutAdmin : onOpenPinModal}
            title={
              isAdmin
                ? 'Você está autenticado como Administrador. Clique para bloquear.'
                : 'Modo Visitante (Valores Ocultos). Clique para desbloquear com PIN.'
            }
            className={`min-h-[44px] px-2.5 flex items-center gap-1 rounded-xl border text-[11px] font-semibold transition-all shadow-sm cursor-pointer ${
              isAdmin
                ? 'bg-emerald-500/15 border-emerald-500/30 text-emerald-300'
                : 'bg-zinc-900 border-zinc-800 text-zinc-400 hover:text-zinc-200'
            }`}
          >
            {isAdmin ? <Unlock className="w-3.5 h-3.5 text-emerald-400" /> : <Lock className="w-3.5 h-3.5 text-amber-400" />}
            <span>{isAdmin ? 'Admin' : 'Visitante'}</span>
          </button>

          <button
            onClick={onOpenSettings}
            title="Configurar Regras Financeiras"
            className="min-h-[44px] px-2.5 flex items-center gap-1.5 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-zinc-300 hover:text-zinc-100 text-xs font-medium transition-colors shadow-sm cursor-pointer"
          >
            <Settings2 className="w-4 h-4 text-zinc-400" />
            <span className="hidden sm:inline">Regras</span>
          </button>
        </div>
      </div>

      {/* Cycle Switcher Navigator */}
      <div className="w-full lg:w-auto flex items-center justify-between sm:justify-center gap-1.5 bg-zinc-900/90 border border-zinc-800 rounded-2xl p-1.5 shadow-md">
        <button
          onClick={handlePrevCycle}
          title="Ciclo Anterior"
          className="min-h-[44px] min-w-[44px] flex items-center justify-center rounded-xl text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 active:scale-95 transition-all cursor-pointer shrink-0"
        >
          <ChevronLeft className="w-5 h-5" />
        </button>

        <div className="flex flex-col items-center px-2 sm:px-4 min-w-0 flex-1 sm:flex-initial">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm sm:text-base text-zinc-100 tracking-wide font-mono whitespace-nowrap">
              Ciclo {selectedCycle}
            </span>
            {summary?.isInvoicePaid ? (
              <span className="text-[10px] font-semibold uppercase bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-1.5 py-0.5 rounded-md whitespace-nowrap">
                Paga ✅
              </span>
            ) : summary?.cycleState === 'CURRENT' ? (
              <span className="text-[10px] font-semibold uppercase bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-1.5 py-0.5 rounded-md whitespace-nowrap">
                Aberto
              </span>
            ) : summary?.cycleState === 'CLOSED' ? (
              <span className="text-[10px] font-semibold uppercase bg-zinc-800 text-zinc-400 border border-zinc-700 px-1.5 py-0.5 rounded-md whitespace-nowrap">
                Fechado
              </span>
            ) : (
              <span className="text-[10px] font-semibold uppercase bg-indigo-500/20 text-indigo-400 border border-indigo-500/30 px-1.5 py-0.5 rounded-md whitespace-nowrap">
                Projeção
              </span>
            )}
          </div>
          <span className="text-[11px] text-zinc-400 font-sans truncate max-w-full">
            {summary?.isInvoicePaid && summary.invoicePaidAt
              ? `Quitada em ${new Date(summary.invoicePaidAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })} • Vence dia 16`
              : 'Fecha dia 08 • Vence dia 16'}
          </span>
        </div>

        <div className="flex items-center gap-1 shrink-0">
          <button
            onClick={handleNextCycle}
            title="Próximo Ciclo"
            className="min-h-[44px] min-w-[44px] flex items-center justify-center rounded-xl text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 active:scale-95 transition-all cursor-pointer"
          >
            <ChevronRight className="w-5 h-5" />
          </button>

          {!isCurrent && (
            <button
              onClick={() => onCycleChange('')}
              title="Voltar ao ciclo atual"
              className="min-h-[44px] px-2.5 rounded-xl text-emerald-400 hover:bg-emerald-500/10 border border-emerald-500/20 text-xs flex items-center gap-1 font-medium transition-colors cursor-pointer"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">Atual</span>
            </button>
          )}
        </div>
      </div>

      {/* Quick Action Buttons */}
      <div className="w-full lg:w-auto flex flex-wrap items-center justify-start lg:justify-end gap-2 py-1">
        <button
          onClick={onOpenNewExpense}
          className="min-h-[40px] shrink-0 whitespace-nowrap flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-xs sm:text-sm font-bold transition-all shadow-md shadow-emerald-500/20 active:scale-[0.98] cursor-pointer"
        >
          <Plus className="w-4 h-4 stroke-[2.5]" />
          <span>Novo Gasto</span>
        </button>

        {onOpenReceivePix && (
          <button
            onClick={onOpenReceivePix}
            title="Registrar recebimento de Pix (vai diretamente para a Caixinha Nubank)"
            className="min-h-[40px] shrink-0 whitespace-nowrap flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-emerald-950/40 hover:bg-emerald-900/50 border border-emerald-500/40 text-emerald-200 text-xs sm:text-sm font-medium transition-colors shadow-sm cursor-pointer"
          >
            <ArrowDownLeft className="w-4 h-4 text-emerald-400 shrink-0" />
            <span>Pix Recebido</span>
          </button>
        )}

        {onOpenPixCaixinha && (
          <button
            onClick={onOpenPixCaixinha}
            title="Retirar dinheiro da Caixinha para pagar no Pix"
            className="min-h-[40px] shrink-0 whitespace-nowrap flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-cyan-950/40 hover:bg-cyan-900/50 border border-cyan-500/40 text-cyan-200 text-xs sm:text-sm font-medium transition-colors shadow-sm cursor-pointer"
          >
            <Send className="w-4 h-4 text-cyan-400 shrink-0" />
            <span>Retirar p/ Pix</span>
          </button>
        )}

        {onOpenSyncInvoice && (
          <button
            onClick={onOpenSyncInvoice}
            title="Ajustar fatura com base no app Nubank"
            className="min-h-[40px] shrink-0 whitespace-nowrap flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-purple-950/40 hover:bg-purple-900/50 border border-purple-500/40 text-purple-200 text-xs sm:text-sm font-medium transition-colors shadow-sm cursor-pointer"
          >
            <CreditCard className="w-4 h-4 text-purple-400 shrink-0" />
            <span>Ajustar Fatura</span>
          </button>
        )}

        {/* Status button on desktop: Guest vs Admin */}
        {isGuest ? (
          <button
            onClick={onOpenPinModal}
            title="Modo Visitante (Valores Ocultos). Clique para entrar com o PIN de Administrador."
            className="hidden lg:flex min-h-[40px] shrink-0 whitespace-nowrap items-center gap-1.5 px-3 py-1.5 rounded-xl border border-amber-500/30 bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 text-xs sm:text-sm font-semibold transition-all shadow-sm cursor-pointer"
          >
            <Lock className="w-4 h-4 text-amber-400" />
            <span>🔒 Modo Visitante (Entrar)</span>
          </button>
        ) : isAdmin ? (
          <button
            onClick={onLogoutAdmin}
            title="Administrador autenticado. Clique para bloquear."
            className="hidden lg:flex min-h-[40px] shrink-0 whitespace-nowrap items-center gap-1.5 px-3 py-1.5 rounded-xl border border-emerald-500/30 hover:border-rose-500/40 bg-emerald-500/15 hover:bg-rose-500/15 text-emerald-300 hover:text-rose-300 text-xs sm:text-sm font-semibold transition-all shadow-sm cursor-pointer group"
          >
            <Lock className="w-4 h-4 text-emerald-400 group-hover:text-rose-400" />
            <span className="group-hover:hidden">Admin 🔓</span>
            <span className="hidden group-hover:inline">🔒 Bloquear</span>
          </button>
        ) : null}

        <button
          onClick={onOpenSettings}
          title="Configurar Regras Financeiras"
          className="hidden lg:flex min-h-[40px] shrink-0 whitespace-nowrap items-center gap-1.5 px-3 py-1.5 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-zinc-300 hover:text-zinc-100 text-xs sm:text-sm font-medium transition-colors shadow-sm cursor-pointer"
        >
          <Settings2 className="w-4 h-4 text-zinc-400" />
          <span>Regras</span>
        </button>
      </div>
    </header>
  );
}
