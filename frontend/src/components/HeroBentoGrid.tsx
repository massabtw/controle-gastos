'use client';

import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Flame, Shield, TrendingUp, AlertTriangle, ShieldCheck, Zap, PiggyBank, Edit3, RefreshCw, CheckCircle2 } from 'lucide-react';
import { api, DashboardSummary } from '@/lib/api';

interface HeroBentoGridProps {
  summary: DashboardSummary | null;
  onOpenUpdateCaixinha?: () => void;
  onOpenEditCardLimit?: () => void;
  onCommitmentPaid?: () => void;
}

export function HeroBentoGrid({
  summary,
  onOpenUpdateCaixinha,
  onOpenEditCardLimit,
  onCommitmentPaid,
}: HeroBentoGridProps) {
  const [payingId, setPayingId] = useState<number | null>(null);

  if (!summary) return null;

  const handlePayFromCaixinha = async (commitmentId: number) => {
    setPayingId(commitmentId);
    try {
      await api.payCommitmentFromCaixinha(commitmentId, summary.billingCycle);
      onCommitmentPaid?.();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao baixar compromisso';
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
    <section className="grid grid-cols-1 lg:grid-cols-12 gap-5 mb-8">
      {/* CARD 1: NORTH STAR - DAILY BURN RATE (5 cols) */}
      <motion.div
        initial={{ opacity: 0, y: 15 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3 }}
        className={`lg:col-span-5 relative overflow-hidden rounded-3xl border p-6 flex flex-col justify-between shadow-xl backdrop-blur-md ${
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
            <div className="flex items-baseline gap-1.5">
              <span className="text-xs sm:text-sm text-zinc-400 font-medium">R$</span>
              <span
                className={`text-4xl sm:text-5xl font-black tracking-tight font-mono ${
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
          </div>
        </div>

        {/* Footer info inside Card 1 */}
        <div className="pt-4 mt-4 border-t border-zinc-800/80 flex items-center justify-between text-xs">
          <span className="text-zinc-400">Saldo restante no teto:</span>
          <span
            className={`font-mono font-bold text-sm ${
              summary.remainingBudget < 0 ? 'text-rose-400' : 'text-zinc-100'
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
        className="lg:col-span-4 rounded-3xl border border-zinc-800 bg-zinc-900/80 p-6 flex flex-col justify-between shadow-xl backdrop-blur-md"
      >
        <div>
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <div className="p-2 rounded-xl bg-blue-500/10 text-blue-400">
                <Shield className="w-5 h-5" />
              </div>
              <div>
                <span className="text-xs font-semibold tracking-wider uppercase text-zinc-400 block">
                  Teto do Cartão
                </span>
                {summary.customCardCap && (
                  <span className="text-[10px] font-mono text-blue-400 font-semibold">
                    Personalizado
                  </span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2">
              {onOpenEditCardLimit && (
                <button
                  onClick={onOpenEditCardLimit}
                  className="flex items-center gap-1 text-[10px] text-blue-400 hover:text-blue-300 font-semibold bg-blue-500/10 hover:bg-blue-500/20 border border-blue-500/30 px-2 py-0.5 rounded-lg transition-colors cursor-pointer"
                  title="Ajustar Limite Bancário ou Teto Mensal"
                >
                  <Edit3 className="w-3 h-3" />
                  <span>Ajustar</span>
                </button>
              )}
              <span className="text-xs font-mono font-bold text-zinc-300">
                {capConsumedPct.toFixed(0)}% Usado
              </span>
            </div>
          </div>

          <div className="my-2">
            <div className="flex items-baseline justify-between mb-2">
              <span className="text-2xl font-bold font-mono text-zinc-100">
                R${' '}
                {totalCapUsed.toLocaleString('pt-BR', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </span>
              <span className="text-xs font-mono text-zinc-400">
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
              <span className="font-bold text-emerald-400">
                R$ {summary.availableBankCreditLimit.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                <span className="text-zinc-500 font-normal"> / R$ {summary.creditCardLimit.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
              </span>
            </div>

            {/* Card Subscriptions Pill if any */}
            {summary.cardSubscriptionsTotal > 0 && (
              <div className="mt-1.5 p-2 rounded-xl bg-purple-950/30 border border-purple-500/30 text-[11px] font-mono flex items-center justify-between text-purple-300">
                <span className="flex items-center gap-1">
                  <RefreshCw className="w-3 h-3 text-purple-400" />
                  Assinaturas no Cartão:
                </span>
                <span className="font-bold">
                  R$ {summary.cardSubscriptionsTotal.toFixed(2)} / mês
                </span>
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

        {/* Active fixed commitments list with Pay from Caixinha action */}
        <div className="pt-3 mt-3 border-t border-zinc-800/80 space-y-1.5">
          <span className="text-[10px] font-semibold text-zinc-400 uppercase tracking-wider block">
            Compromissos do Ciclo:
          </span>
          <div className="flex flex-wrap gap-1.5">
            {summary.activeFixedCommitments.map((c) => (
              <div
                key={c.id}
                className="text-[11px] font-sans bg-zinc-800/80 text-zinc-300 border border-zinc-700/80 px-2 py-1 rounded-xl flex items-center gap-1.5"
              >
                <span className="text-zinc-400">{c.title}:</span>
                <span className="font-mono font-medium">R$ {c.amount.toFixed(0)}</span>
                <span className="text-[10px] font-mono bg-zinc-700 text-zinc-300 px-1 rounded">
                  {c.badge}
                </span>

                {/* Status & action for Caixinha commitments */}
                {c.paymentSource === 'PIX_CAIXINHA' && (
                  c.isPaidInCycle ? (
                    <span className="flex items-center gap-0.5 text-[10px] font-mono font-semibold text-emerald-400 bg-emerald-500/10 px-1.5 py-0.5 rounded border border-emerald-500/20">
                      <CheckCircle2 className="w-2.5 h-2.5" /> Pago
                    </span>
                  ) : (
                    <button
                      onClick={() => handlePayFromCaixinha(c.id)}
                      disabled={payingId === c.id}
                      className="text-[10px] font-mono font-bold text-cyan-300 hover:text-cyan-200 bg-cyan-500/20 hover:bg-cyan-500/30 px-1.5 py-0.5 rounded transition-colors cursor-pointer border border-cyan-500/30"
                      title="Debitar valor da Caixinha Nubank para este ciclo"
                    >
                      {payingId === c.id ? 'Baixando...' : 'Dar Baixa 🐷'}
                    </button>
                  )
                )}
              </div>
            ))}
          </div>
        </div>
      </motion.div>

      {/* CARD 3: CAIXINHA NUBANK (115% CDI) & LUCRO FANTASMA (4 cols) */}
      <motion.div
        initial={{ opacity: 0, y: 15 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3, delay: 0.2 }}
        className="lg:col-span-3 rounded-3xl border border-zinc-800 bg-zinc-900/80 p-5 flex flex-col justify-between shadow-xl backdrop-blur-md"
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
          <div className="p-3 rounded-2xl bg-zinc-950/70 border border-zinc-800/80 mb-3">
            <div className="flex items-center justify-between text-[11px] text-zinc-400 mb-0.5">
              <span>Saldo Investido:</span>
              {onOpenUpdateCaixinha && (
                <button
                  onClick={onOpenUpdateCaixinha}
                  className="flex items-center gap-1 text-[10px] text-purple-400 hover:text-purple-300 hover:underline"
                >
                  <Edit3 className="w-3 h-3" />
                  <span>Ajustar</span>
                </button>
              )}
            </div>
            <div className="flex items-baseline gap-1 text-purple-300 font-mono">
              <span className="text-xs">R$</span>
              <span className="text-2xl font-black tracking-tight">
                {summary.nubankCaixinhaBalance.toLocaleString('pt-BR', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </span>
            </div>
            <div className="flex items-center justify-between text-[10px] text-zinc-400 font-mono mt-1 pt-1 border-t border-zinc-800/60">
              <span>Rende: +R$ {summary.nubankCaixinhaMonthlyYield.toFixed(2)}/mês</span>
              <span className="text-emerald-400 font-medium">(+R$ {summary.nubankCaixinhaDailyYield.toFixed(2)}/dia)</span>
            </div>
          </div>

          {/* Lucro Fantasma da Fatura */}
          <div className="pt-2 border-t border-zinc-800/80">
            <div className="flex items-center justify-between text-[11px] text-zinc-400 mb-1">
              <span className="flex items-center gap-1">
                <TrendingUp className="w-3.5 h-3.5 text-emerald-400" />
                Lucro Fantasma Fatura:
              </span>
              <span className="font-mono text-xs font-bold text-emerald-400">
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
