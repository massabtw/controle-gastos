'use client';

import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Flame, Shield, TrendingUp, AlertTriangle, ShieldCheck, Zap, PiggyBank, Edit3, RefreshCw, CheckCircle2, CreditCard, Plus, ArrowDownLeft } from 'lucide-react';
import { api, DashboardSummary } from '@/lib/api';

interface HeroBentoGridProps {
  summary: DashboardSummary | null;
  onOpenUpdateCaixinha?: () => void;
  onOpenDepositCaixinha?: () => void;
  onOpenReceivePix?: () => void;
  onOpenEditCardLimit?: () => void;
  onCommitmentPaid?: () => void;
  onInvoiceStatusChanged?: () => void;
  isAdmin?: boolean;
  onRequireAdmin?: () => void;
}

export function HeroBentoGrid({
  summary,
  onOpenUpdateCaixinha,
  onOpenDepositCaixinha,
  onOpenReceivePix,
  onOpenEditCardLimit,
  onCommitmentPaid,
  onInvoiceStatusChanged,
  isAdmin = false,
  onRequireAdmin,
}: HeroBentoGridProps) {
  const [payingId, setPayingId] = useState<number | null>(null);
  const [togglingInvoice, setTogglingInvoice] = useState(false);
  const [quickDepositing, setQuickDepositing] = useState<number | null>(null);

  if (!summary) return null;

  const handleToggleInvoicePaid = async () => {
    if (!isAdmin) {
      onRequireAdmin?.();
      return;
    }
    setTogglingInvoice(true);
    try {
      await api.toggleInvoicePaid(summary.billingCycle);
      onInvoiceStatusChanged?.();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao alterar status da fatura';
      alert(`Erro: ${message}`);
    } finally {
      setTogglingInvoice(false);
    }
  };

  const handleQuickDeposit = async (amount: number) => {
    if (!isAdmin) {
      onRequireAdmin?.();
      return;
    }
    setQuickDepositing(amount);
    try {
      await api.depositCaixinha(amount);
      onInvoiceStatusChanged?.();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao adicionar na Caixinha';
      alert(`Erro: ${message}`);
    } finally {
      setQuickDepositing(null);
    }
  };

  const handleToggleCommitmentPaid = async (commitmentId: number) => {
    if (!isAdmin) {
      onRequireAdmin?.();
      return;
    }
    setPayingId(commitmentId);
    try {
      await api.toggleCommitmentPaid(commitmentId, summary.billingCycle);
      onCommitmentPaid?.();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao alterar status do compromisso';
      alert(`Erro: ${message}`);
    } finally {
      setPayingId(null);
    }
  };

  const isWarning = summary.status === 'WARNING';
  const isCritical = summary.status === 'INVADING_INVESTMENT';

  // Percentage of card hard cap consumed (including Pix Caixinha)
  const totalCapUsed = summary.currentInvoiceTotal + (summary.pixCaixinhaUsedInCycle || 0);
  const capConsumedPct = summary.cardHardCap > 0
    ? Math.min(100, Math.max(0, (totalCapUsed / summary.cardHardCap) * 100))
    : 100;

  return (
    <section className="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-5 mb-6 sm:mb-8">
      {/* CARD 1: NORTH STAR - DAILY BURN RATE (5 cols) */}
      <motion.div
        initial={{ opacity: 0, y: 15 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3 }}
        className={`lg:col-span-5 relative overflow-hidden rounded-3xl border p-5 sm:p-6 flex flex-col justify-between shadow-xl backdrop-blur-md ${
          isCritical
            ? 'bg-rose-950/20 border-rose-500/40 text-rose-100 shadow-rose-950/30'
            : isWarning
            ? 'bg-amber-950/20 border-amber-500/40 text-amber-100 shadow-amber-950/30'
            : 'bg-zinc-900/80 border-emerald-500/30 text-zinc-100 shadow-emerald-950/20'
        }`}
      >
        {/* Subtle background glow */}
        <div
          className={`absolute -right-12 -top-12 w-44 h-44 rounded-full blur-3xl opacity-20 pointer-events-none ${
            isCritical ? 'bg-rose-500' : isWarning ? 'bg-amber-500' : 'bg-emerald-500'
          }`}
        />

        <div>
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <div
                className={`p-2 rounded-xl ${
                  isCritical
                    ? 'bg-rose-500/20 text-rose-400'
                    : isWarning
                    ? 'bg-amber-500/20 text-amber-400'
                    : 'bg-emerald-500/20 text-emerald-400'
                }`}
              >
                <Flame className="w-5 h-5 fill-current" />
              </div>
              <span className="text-xs font-semibold tracking-wider uppercase text-zinc-400">
                Limite Diário (Burn Rate)
              </span>
            </div>

            <span
              className={`text-[11px] font-mono px-2.5 py-1 rounded-full border flex items-center gap-1.5 font-semibold ${
                isCritical
                  ? 'bg-rose-500/10 text-rose-400 border-rose-500/30'
                  : isWarning
                  ? 'bg-amber-500/10 text-amber-400 border-amber-500/30'
                  : 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30'
              }`}
            >
              {isCritical ? (
                <>
                  <AlertTriangle className="w-3 h-3" /> Meta Invadida
                </>
              ) : isWarning ? (
                <>
                  <AlertTriangle className="w-3 h-3" /> Atenção &lt; R$ 20/d
                </>
              ) : (
                <>
                  <ShieldCheck className="w-3 h-3" /> Ritmo Seguro
                </>
              )}
            </span>
          </div>

          <div className="my-3">
            <div className="flex items-baseline gap-1.5 flex-wrap">
              <span className="text-xs sm:text-sm text-zinc-400 font-medium">R$</span>
              <span
                className={`text-3xl sm:text-5xl font-black tracking-tight font-mono privacy-sensitive ${
                  isCritical ? 'text-rose-400' : isWarning ? 'text-amber-400' : 'text-emerald-400'
                }`}
              >
                {summary.availablePerDay.toLocaleString('pt-BR', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </span>
              <span className="text-sm sm:text-base text-zinc-400 font-medium">/ dia</span>
            </div>

            <p className="text-xs text-zinc-400 mt-2 flex items-center gap-2">
              <span className="inline-block w-2 h-2 rounded-full bg-zinc-500 animate-ping" />
              <span>
                {summary.daysUntilClosing > 0
                  ? `${summary.daysUntilClosing} dias restantes até o fechamento (dia 08)`
                  : 'Fatura fechada'}
              </span>
            </p>

            {/* Spending Pace Indicator */}
            {summary.totalDaysInCycle > 0 && summary.cycleState === 'CURRENT' && (
              <div className="mt-3 p-2.5 rounded-xl bg-zinc-950/60 border border-zinc-800/80 text-[11px] font-mono">
                <div className="flex items-center justify-between text-zinc-400 mb-1">
                  <span>Ritmo de Gasto:</span>
                  <span
                    className={
                      summary.paceStatus === 'AHEAD'
                        ? 'text-emerald-400 font-bold'
                        : summary.paceStatus === 'OVERPACED'
                        ? 'text-amber-400 font-bold'
                        : 'text-blue-400'
                    }
                  >
                    {summary.paceStatus === 'AHEAD' && 'Cushion Seguro'}
                    {summary.paceStatus === 'OVERPACED' && 'Gasto Acelerado'}
                    {summary.paceStatus === 'ON_TRACK' && 'Dentro do Esperado'}
                  </span>
                </div>
                <div className="text-zinc-500 text-[10px]">
                  Dia {summary.daysElapsed} de {summary.totalDaysInCycle} • Esperado até hoje: R$ {summary.idealPaceAmount.toFixed(0)}
                  {summary.paceDifference > 0 ? (
                    <span className="text-amber-400 ml-1">(+R$ {summary.paceDifference.toFixed(0)})</span>
                  ) : (
                    <span className="text-emerald-400 ml-1">(-R$ {Math.abs(summary.paceDifference).toFixed(0)})</span>
                  )}
                </div>
              </div>
            )}

            {/* Quick Budget Health Breakdown */}
            <div className="mt-3 p-3 rounded-2xl bg-zinc-950/60 border border-zinc-800/80 grid grid-cols-2 gap-2 text-xs font-mono">
              <div>
                <span className="text-[10px] text-zinc-400 block uppercase tracking-wider font-sans">Teto Mensal</span>
                <span className="font-bold text-zinc-100 privacy-sensitive">
                  R$ {summary.cardHardCap.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-zinc-400 block uppercase tracking-wider font-sans">Total Gasto</span>
                <span className="font-bold text-amber-400 privacy-sensitive">
                  R$ {totalCapUsed.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-zinc-400 block uppercase tracking-wider font-sans">Fatura Cartão</span>
                <span className="font-bold text-blue-400 privacy-sensitive">
                  R$ {summary.currentInvoiceTotal.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-zinc-400 block uppercase tracking-wider font-sans">Pix Caixinha</span>
                <span className="font-bold text-cyan-400 privacy-sensitive">
                  R$ {(summary.pixCaixinhaUsedInCycle || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Footer info inside Card 1 */}
        <div className="pt-3 mt-4 border-t border-zinc-800/80 flex items-center justify-between text-xs">
          <span className="text-zinc-400">Saldo restante no teto:</span>
          <span
            className={`font-mono font-bold text-sm sm:text-base privacy-sensitive ${
              summary.remainingBudget < 0 ? 'text-rose-400' : 'text-emerald-400'
            }`}
          >
            R${' '}
            {summary.remainingBudget.toLocaleString('pt-BR', {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}
          </span>
        </div>
      </motion.div>

      {/* CARD 2: HARD CAP PROGRESS & EQUATION BREAKDOWN (4 cols) */}
      <motion.div
        initial={{ opacity: 0, y: 15 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3, delay: 0.1 }}
        className="lg:col-span-4 rounded-3xl border border-zinc-800 bg-zinc-900/80 p-5 sm:p-6 flex flex-col justify-between shadow-xl backdrop-blur-md"
      >
        <div>
          <div className="flex items-center justify-between gap-2 mb-4">
            <div className="flex items-center gap-2 min-w-0">
              <div className="p-2 rounded-xl bg-blue-500/10 text-blue-400 shrink-0">
                <Shield className="w-5 h-5" />
              </div>
              <div className="min-w-0">
                <span className="text-xs font-semibold tracking-wider uppercase text-zinc-400 block truncate">
                  Teto do Cartão
                </span>
                {summary.customCardCap && (
                  <span className="text-[10px] font-mono text-blue-400 font-semibold">
                    Personalizado
                  </span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 shrink-0">
              {onOpenEditCardLimit && (
                <button
                  onClick={isAdmin ? onOpenEditCardLimit : onRequireAdmin}
                  className="min-h-[44px] flex items-center gap-1.5 text-xs text-blue-400 hover:text-blue-300 font-semibold bg-blue-500/10 hover:bg-blue-500/20 border border-blue-500/30 px-3 py-1.5 rounded-xl transition-colors cursor-pointer"
                  title="Ajustar Limite Bancário ou Teto Mensal"
                >
                  <Edit3 className="w-3.5 h-3.5" />
                  <span>Ajustar</span>
                </button>
              )}
              <span className="text-xs font-mono font-bold text-zinc-300 whitespace-nowrap">
                {capConsumedPct.toFixed(0)}% Usado
              </span>
            </div>
          </div>

          <div className="my-2">
            <div className="flex items-baseline justify-between gap-2 mb-2 flex-wrap">
              <span className="text-2xl sm:text-3xl font-bold font-mono text-zinc-100 whitespace-nowrap privacy-sensitive">
                R${' '}
                {totalCapUsed.toLocaleString('pt-BR', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </span>
              <span className="text-xs font-mono text-zinc-400 whitespace-nowrap privacy-sensitive">
                de R${' '}
                {summary.cardHardCap.toLocaleString('pt-BR', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </span>
            </div>

            {/* Progress bar */}
            <div className="w-full h-2.5 bg-zinc-800 rounded-full overflow-hidden p-0.5">
              <motion.div
                initial={{ width: 0 }}
                animate={{ width: `${capConsumedPct}%` }}
                transition={{ duration: 0.6, ease: 'easeOut' }}
                className={`h-full rounded-full ${
                  capConsumedPct > 90
                    ? 'bg-rose-500'
                    : capConsumedPct > 70
                    ? 'bg-amber-500'
                    : 'bg-emerald-500'
                }`}
              />
            </div>

            {/* Pill if Pix Caixinha used */}
            {summary.pixCaixinhaUsedInCycle > 0 && (
              <div className="mt-2.5 text-[11px] font-mono flex items-center justify-between p-2 rounded-xl bg-cyan-950/40 border border-cyan-500/30 text-cyan-300">
                <span>💳 Cartão: R$ {summary.currentInvoiceTotal.toFixed(2)}</span>
                <span className="text-zinc-500">+</span>
                <span>💸 Pix Caixinha: R$ {summary.pixCaixinhaUsedInCycle.toFixed(2)}</span>
              </div>
            )}

            {/* Bank Credit Limit Indicator */}
            <div className="mt-2.5 p-2 rounded-xl bg-zinc-950/60 border border-zinc-800 text-[11px] font-mono flex items-center justify-between">
              <span className="text-zinc-400">Limite Banco Livre:</span>
              <span className="font-bold text-emerald-400 privacy-sensitive">
                R$ {summary.availableBankCreditLimit.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                <span className="text-zinc-500 font-normal"> / R$ {summary.creditCardLimit.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
              </span>
            </div>

            {/* Card Subscriptions Pill if any */}
            {summary.cardSubscriptionsTotal > 0 && (
              <div className="mt-1.5 p-2 rounded-xl bg-purple-950/30 border border-purple-500/30 text-[11px] font-mono space-y-1 text-purple-300">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-1">
                    <RefreshCw className="w-3 h-3 text-purple-400" />
                    Assinaturas no Cartão:
                  </span>
                  <span className="font-bold privacy-sensitive">
                    R$ {summary.cardSubscriptionsTotal.toFixed(2)} / mês
                  </span>
                </div>
                {(summary.scheduledSubscriptionsTotal ?? 0) > 0 && (
                  <div className="flex items-center justify-between text-[10px] text-zinc-400 pt-1 border-t border-purple-500/20 font-mono">
                    <span className="text-emerald-400 privacy-sensitive">Cobrado: R$ {(summary.chargedSubscriptionsTotal ?? 0).toFixed(2)}</span>
                    <span className="text-purple-300 privacy-sensitive">Agendado: R$ {(summary.scheduledSubscriptionsTotal ?? 0).toFixed(2)}</span>
                  </div>
                )}
              </div>
            )}

            {/* Status da Fatura do Ciclo */}
            {summary.isInvoicePaid ? (
              <div className="mt-3 p-3 rounded-2xl bg-emerald-950/25 border border-emerald-500/40 text-xs font-mono space-y-1.5">
                <div className="flex items-center justify-between gap-2">
                  <span className="flex items-center gap-1.5 text-emerald-400 font-bold">
                    <CheckCircle2 className="w-4 h-4 shrink-0" />
                    <span>Fatura Paga! ✅</span>
                  </span>
                  <button
                    onClick={handleToggleInvoicePaid}
                    disabled={togglingInvoice}
                    className="min-h-[44px] px-3 rounded-xl bg-zinc-900/80 border border-zinc-700 text-xs text-zinc-300 hover:text-rose-400 transition-colors cursor-pointer"
                    title="Desfazer marcação de fatura paga"
                  >
                    {togglingInvoice ? '...' : 'Desfazer'}
                  </button>
                </div>
                <div className="flex items-center justify-between text-zinc-300 text-[11px]">
                  <span>Fatura Quitada:</span>
                  <span className="font-bold text-zinc-100 font-mono privacy-sensitive">
                    R$ {summary.invoicePaidAmount.toFixed(2)}
                  </span>
                </div>
              </div>
            ) : (
              <div className="mt-3 p-3 rounded-2xl bg-zinc-950/70 border border-zinc-800 text-xs font-mono flex items-center justify-between gap-2">
                <div className="flex items-center gap-1.5 text-zinc-300 min-w-0">
                  <CreditCard className="w-4 h-4 text-amber-400 shrink-0" />
                  <span className="truncate">Fatura (Vence {new Date(summary.cycleDueDate).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })})</span>
                </div>
                <button
                  onClick={handleToggleInvoicePaid}
                  disabled={togglingInvoice}
                  className="min-h-[44px] shrink-0 whitespace-nowrap px-3.5 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-xs font-bold transition-all shadow-md flex items-center gap-1.5 cursor-pointer"
                  title="Marcar fatura como paga"
                >
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>{togglingInvoice ? 'Processando...' : 'Marcar Paga 💳'}</span>
                </button>
              </div>
            )}
          </div>

          {/* Visual Equation Formula */}
          <div className="mt-4 p-3 rounded-2xl bg-zinc-950/60 border border-zinc-800/80 text-[11px] leading-relaxed">
            <div className="text-zinc-400 font-semibold mb-1 flex items-center gap-1">
              <Zap className="w-3.5 h-3.5 text-amber-400" />
              {summary.customCardCap ? 'Teto Mensal Manual Configurado:' : 'Fórmula do Teto Mensal:'}
            </div>
            {summary.customCardCap ? (
              <div className="font-mono text-zinc-300 flex items-center flex-wrap gap-1">
                <span className="text-blue-400 font-bold">R$ {summary.customCardCap.toFixed(2)} (Manual)</span>
                <span className="text-zinc-500 text-[10px]">(Fórmula padrão: R$ {summary.cardHardCapFormula.toFixed(2)})</span>
              </div>
            ) : (
              <div className="font-mono text-zinc-300 flex items-center flex-wrap gap-1">
                <span>R$ {summary.totalIncome.toFixed(0)} (Renda)</span>
                <span className="text-rose-400">− R$ 1.000 (Meta 🔒)</span>
                <span className="text-blue-400">− R$ {summary.totalFixedCommitments.toFixed(0)} (Pix)</span>
                <span>=</span>
                <span className="text-emerald-400 font-bold">R$ {summary.cardHardCap.toFixed(0)} Teto</span>
              </div>
            )}
          </div>
        </div>

        {/* Active fixed commitments & subscriptions list with status and Pay from Caixinha action */}
        <div className="pt-3 mt-3 border-t border-zinc-800/80 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-[10px] font-semibold text-zinc-400 uppercase tracking-wider block">
              Compromissos do Ciclo ({summary.activeFixedCommitments.length + (summary.activeSubscriptions?.length || 0)}):
            </span>
            {(summary.activeFixedCommitments.length + (summary.activeSubscriptions?.length || 0)) > 3 && (
              <span className="text-[10px] font-mono text-zinc-500">role para ver ↓</span>
            )}
          </div>
          <div className="flex flex-col gap-1.5 max-h-52 overflow-y-auto pr-1">
            {[...summary.activeFixedCommitments, ...(summary.activeSubscriptions || [])].map((c) => {
              const isSubscription = c.commitmentType === 'SUBSCRIPTION';
              const isCharged = c.isCharged || c.badge === 'Cobrado';

              return (
                <div
                  key={`${c.commitmentType}-${c.id}`}
                  className="min-h-[44px] text-xs font-sans bg-zinc-800/80 text-zinc-300 border border-zinc-700/80 px-3 py-1.5 rounded-xl flex items-center justify-between gap-2"
                >
                  <div className="flex items-center gap-1.5 min-w-0 flex-wrap">
                    {isSubscription && (
                      <RefreshCw className="w-3 h-3 text-purple-400 shrink-0" />
                    )}
                    <span className="text-zinc-300 font-medium truncate">{c.title}:</span>
                    <span className="font-mono font-bold text-zinc-100 whitespace-nowrap privacy-sensitive">
                      R$ {c.amount.toFixed(0)}
                    </span>
                    {isSubscription ? (
                      <span
                        className={`text-[10px] font-mono font-semibold px-2 py-0.5 rounded-md border whitespace-nowrap ${
                          isCharged
                            ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                            : 'bg-purple-500/15 text-purple-300 border-purple-500/30'
                        }`}
                      >
                        {isCharged ? '✅ Cobrado' : `🕒 ${c.badge}`}
                      </span>
                    ) : (
                      <span className="text-[10px] font-mono bg-zinc-700 text-zinc-300 px-1.5 py-0.5 rounded whitespace-nowrap">
                        {c.badge}
                      </span>
                    )}
                  </div>

                  {/* Status & interactive toggle action for Caixinha commitments */}
                  {!isSubscription && c.paymentSource === 'PIX_CAIXINHA' && (
                    <button
                      onClick={() => handleToggleCommitmentPaid(c.id)}
                      disabled={payingId === c.id}
                      title={
                        c.isPaidInCycle
                          ? 'Clique para desmarcar e estornar valor de volta para a Caixinha'
                          : 'Clique para marcar como pago e debitar da Caixinha Nubank'
                      }
                      className={`min-h-[36px] shrink-0 whitespace-nowrap flex items-center gap-1 text-[11px] font-mono font-semibold px-2.5 py-1.5 rounded-lg transition-all cursor-pointer border group ${
                        c.isPaidInCycle
                          ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30 hover:bg-rose-500/20 hover:text-rose-300 hover:border-rose-500/40'
                          : 'bg-cyan-500/20 text-cyan-300 border-cyan-500/30 hover:bg-cyan-500/30 hover:text-cyan-100'
                      }`}
                    >
                      {payingId === c.id ? (
                        <span>Aguarde...</span>
                      ) : c.isPaidInCycle ? (
                        <>
                          <CheckCircle2 className="w-3 h-3 group-hover:hidden" />
                          <span className="group-hover:hidden">✅ Pago</span>
                          <span className="hidden group-hover:inline">Desmarcar ↩</span>
                        </>
                      ) : (
                        <span>Dar Baixa 🐷</span>
                      )}
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </motion.div>

      {/* CARD 3: CAIXINHA NUBANK (115% CDI) & LUCRO FANTASMA (3 cols) */}
      <motion.div
        initial={{ opacity: 0, y: 15 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3, delay: 0.2 }}
        className="lg:col-span-3 rounded-3xl border border-zinc-800 bg-zinc-900/80 p-5 sm:p-6 flex flex-col justify-between shadow-xl backdrop-blur-md"
      >
        <div>
          {/* Nubank Caixinha Header */}
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <div className="p-1.5 rounded-xl bg-purple-500/20 text-purple-400">
                <PiggyBank className="w-4 h-4" />
              </div>
              <span className="text-xs font-semibold tracking-wider uppercase text-zinc-300">
                Caixinha Nubank
              </span>
            </div>
            <span className="text-[10px] font-mono bg-purple-500/10 text-purple-300 border border-purple-500/20 px-2 py-0.5 rounded-full font-bold">
              115% CDI
            </span>
          </div>

          {/* Balance & Edit Button */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-zinc-800/80 mb-3">
            <div className="flex items-center justify-between text-xs text-zinc-400 mb-1">
              <span>Saldo Investido:</span>
              {onOpenUpdateCaixinha && (
                <button
                  onClick={isAdmin ? onOpenUpdateCaixinha : onRequireAdmin}
                  className="min-h-[36px] px-2 flex items-center gap-1 text-xs text-purple-400 hover:text-purple-300 hover:underline cursor-pointer"
                  title="Editar Saldo Total Manualmente"
                >
                  <Edit3 className="w-3.5 h-3.5" />
                  <span>Editar Total</span>
                </button>
              )}
            </div>
            <div className="flex items-baseline gap-1 text-purple-300 font-mono">
              <span className="text-xs">R$</span>
              <span className="text-2xl sm:text-3xl font-bold tracking-tight privacy-sensitive">
                {summary.nubankCaixinhaBalance.toLocaleString('pt-BR', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </span>
            </div>
            <div className="flex items-center justify-between text-[11px] text-zinc-400 font-mono mt-1.5 pt-1.5 border-t border-zinc-800/60">
              <span className="privacy-sensitive">Rende: +R$ {summary.nubankCaixinhaMonthlyYield.toFixed(2)}/mês</span>
              <span className="text-emerald-400 font-medium privacy-sensitive">(+R$ {summary.nubankCaixinhaDailyYield.toFixed(2)}/dia)</span>
            </div>

            {/* Quick Add Chips */}
            <div className="mt-3 pt-2.5 border-t border-zinc-800/80">
              <div className="flex items-center justify-between text-xs text-zinc-400 mb-2 font-medium">
                <span>+ Adicionar na Caixinha:</span>
                {onOpenDepositCaixinha && (
                  <button
                    onClick={isAdmin ? onOpenDepositCaixinha : onRequireAdmin}
                    className="text-purple-400 hover:text-purple-300 hover:underline text-xs cursor-pointer py-1"
                  >
                    Outro valor...
                  </button>
                )}
              </div>
              <div className="grid grid-cols-4 gap-1.5">
                {[10, 20, 50, 100].map((amt) => (
                  <button
                    key={amt}
                    onClick={() => handleQuickDeposit(amt)}
                    disabled={quickDepositing !== null}
                    className="min-h-[44px] py-1.5 rounded-xl bg-purple-500/10 hover:bg-purple-500/25 border border-purple-500/20 text-purple-300 hover:text-purple-100 text-xs font-mono font-bold transition-all disabled:opacity-50 cursor-pointer text-center"
                    title={`Adicionar R$ ${amt} imediatamente à Caixinha`}
                  >
                    {quickDepositing === amt ? '...' : `+R$ ${amt}`}
                  </button>
                ))}
              </div>
            </div>

            {/* Direct Action Buttons: Pix Recebido & + Guardar */}
            <div className="mt-2.5 grid grid-cols-2 gap-2">
              {onOpenReceivePix && (
                <button
                  onClick={isAdmin ? onOpenReceivePix : onRequireAdmin}
                  className="min-h-[44px] flex items-center justify-center gap-1.5 py-2 px-2.5 rounded-xl bg-emerald-500/15 hover:bg-emerald-500/25 border border-emerald-500/30 text-emerald-300 text-xs font-semibold transition-all cursor-pointer shadow-sm whitespace-nowrap"
                  title="Registrar dinheiro recebido externamente via Pix que vai para a Caixinha"
                >
                  <ArrowDownLeft className="w-4 h-4 shrink-0" />
                  <span>Pix Recebido</span>
                </button>
              )}

              {onOpenDepositCaixinha && (
                <button
                  onClick={isAdmin ? onOpenDepositCaixinha : onRequireAdmin}
                  className="min-h-[44px] flex items-center justify-center gap-1.5 py-2 px-2.5 rounded-xl bg-purple-500/20 hover:bg-purple-500/30 border border-purple-500/40 text-purple-200 text-xs font-semibold transition-all cursor-pointer shadow-sm whitespace-nowrap"
                  title="Guardar valor personalizado na Caixinha"
                >
                  <Plus className="w-4 h-4 shrink-0" />
                  <span>+ Guardar</span>
                </button>
              )}
            </div>
          </div>

          {/* Lucro Fantasma da Fatura */}
          <div className="pt-2 border-t border-zinc-800/80">
            <div className="flex items-center justify-between text-[11px] text-zinc-400 mb-1">
              <span className="flex items-center gap-1">
                <TrendingUp className="w-3.5 h-3.5 text-emerald-400" />
                Lucro Fantasma Fatura:
              </span>
              <span className="font-mono text-xs font-bold text-emerald-400 privacy-sensitive">
                +R$ {summary.phantomYieldEstimate.toFixed(2)}
              </span>
            </div>
            <p className="text-[10px] text-zinc-500 leading-tight">
              Dinheiro da fatura rendendo até dia 16 a {summary.cdiAnnualRate}% a.a.
            </p>
          </div>
        </div>

        <div className="pt-2 mt-2 border-t border-zinc-800/80 flex items-center justify-between text-[11px] text-zinc-400 font-mono">
          <span>Taxa Efetiva:</span>
          <span className="text-purple-300 font-bold">
            {summary.nubankCaixinhaEffectiveAnnualRate}% a.a.
          </span>
        </div>
      </motion.div>
    </section>
  );
}
