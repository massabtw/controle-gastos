'use client';

import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Calculator, AlertTriangle, ShieldCheck, ArrowRight } from 'lucide-react';
import { api, SimulatorResponse, DashboardSummary } from '@/lib/api';

interface PurchaseSimulatorProps {
  summary: DashboardSummary | null;
}

export function PurchaseSimulator({ summary }: PurchaseSimulatorProps) {
  const [amount, setAmount] = useState<number>(300);
  const [installments, setInstallments] = useState<number>(3);
  const [simulation, setSimulation] = useState<SimulatorResponse | null>(null);

  useEffect(() => {
    if (amount <= 0) {
      setSimulation(null);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        const res = await api.simulate({ amount, installments });
        setSimulation(res);
      } catch (err) {
        console.error('Error simulating purchase:', err);
      }
    }, 200);

    return () => clearTimeout(timer);
  }, [amount, installments, summary]);

  const presetAmounts = [100, 250, 500, 1000];

  return (
    <div className="rounded-3xl border border-zinc-800 bg-zinc-900/80 p-6 shadow-xl backdrop-blur-md mb-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-6 pb-4 border-b border-zinc-800/80">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-xl bg-amber-500/10 text-amber-400">
            <Calculator className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-bold text-zinc-100 flex items-center gap-2">
              Simulador de Compras
              <span className="text-[10px] font-mono uppercase bg-amber-500/10 text-amber-400 border border-amber-500/20 px-2 py-0.5 rounded-full">
                Lack of Judgment Test
              </span>
            </h2>
            <p className="text-xs text-zinc-400">
              Veja o impacto imediato no seu limite diário e nos ciclos futuros antes de passar o cartão.
            </p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* Controls (5 cols) */}
        <div className="md:col-span-5 space-y-4">
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-2">
              Valor da Compra (R$)
            </label>
            <div className="relative">
              <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                R$
              </span>
              <input
                type="number"
                min="1"
                step="0.01"
                value={amount || ''}
                onChange={(e) => setAmount(Number(e.target.value))}
                placeholder="Ex: 300.00"
                className="w-full bg-zinc-950/70 border border-zinc-700/80 rounded-xl py-2.5 pl-10 pr-4 font-mono text-base text-zinc-100 focus:outline-none focus:border-amber-500/80 focus:ring-1 focus:ring-amber-500/50"
              />
            </div>

            {/* Quick preset pills */}
            <div className="flex gap-2 mt-2">
              {presetAmounts.map((preset) => (
                <button
                  key={preset}
                  onClick={() => setAmount(preset)}
                  type="button"
                  className={`text-xs font-mono px-2.5 py-1 rounded-lg border transition-all ${
                    amount === preset
                      ? 'bg-amber-500/20 border-amber-500/50 text-amber-300'
                      : 'bg-zinc-800/60 border-zinc-700/60 text-zinc-400 hover:text-zinc-200'
                  }`}
                >
                  R$ {preset}
                </button>
              ))}
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="text-xs font-semibold text-zinc-300 uppercase tracking-wider">
                Parcelas
              </label>
              <span className="text-xs font-mono font-bold text-amber-400 bg-amber-500/10 px-2 py-0.5 rounded border border-amber-500/20">
                {installments}x de R$ {(amount / installments || 0).toFixed(2)}
              </span>
            </div>

            <div className="flex items-center gap-1.5 flex-wrap">
              {[1, 2, 3, 4, 5, 6, 10, 12].map((num) => (
                <button
                  key={num}
                  type="button"
                  onClick={() => setInstallments(num)}
                  className={`text-xs font-mono px-3 py-1.5 rounded-xl border transition-all ${
                    installments === num
                      ? 'bg-amber-500 text-zinc-950 font-bold border-amber-400 shadow-sm'
                      : 'bg-zinc-800/80 border-zinc-700 text-zinc-300 hover:bg-zinc-700'
                  }`}
                >
                  {num}x
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Live Simulation Results (7 cols) */}
        <div className="md:col-span-7 flex flex-col justify-between bg-zinc-950/60 border border-zinc-800/80 rounded-2xl p-5">
          {simulation ? (
            <div className="space-y-4">
              {/* Daily Burn Rate Impact */}
              <div>
                <span className="text-xs text-zinc-400 block mb-1">Impacto no Limite Diário:</span>
                <div className="flex items-center gap-3 flex-wrap">
                  <div className="flex items-baseline gap-1 font-mono">
                    <span className="text-xs text-zinc-500">De:</span>
                    <span className="text-lg text-zinc-400 line-through">
                      R$ {simulation.currentDailyBefore.toFixed(2)}
                    </span>
                  </div>

                  <ArrowRight className="w-4 h-4 text-zinc-500" />

                  <div className="flex items-baseline gap-1 font-mono">
                    <span className="text-xs text-zinc-400">Para:</span>
                    <span
                      className={`text-2xl font-black ${
                        simulation.willInvadeInvestmentGoal
                          ? 'text-rose-400'
                          : simulation.currentDailyAfter < 20
                          ? 'text-amber-400'
                          : 'text-emerald-400'
                      }`}
                    >
                      R$ {simulation.currentDailyAfter.toFixed(2)}
                    </span>
                    <span className="text-xs text-zinc-400">/ dia</span>
                  </div>
                </div>
              </div>

              {/* Warning Banner */}
              <AnimatePresence>
                {simulation.willInvadeInvestmentGoal ? (
                  <motion.div
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    exit={{ opacity: 0, height: 0 }}
                    className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 text-xs flex items-start gap-2.5"
                  >
                    <AlertTriangle className="w-4 h-4 text-rose-400 shrink-0 mt-0.5" />
                    <div>
                      <span className="font-bold block">INVASÃO DA META DE INVESTIMENTO!</span>
                      Esta compra estoura seu teto mensal em{' '}
                      <span className="font-mono font-bold text-rose-200">
                        R$ {simulation.invadedAmount.toFixed(2)}
                      </span>
                      , forçando você a usar o dinheiro da sua meta de R$ 1.000.
                    </div>
                  </motion.div>
                ) : (
                  <div className="p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 text-xs flex items-center gap-2">
                    <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                    <span>
                      Compra segura dentro do teto. Sua meta de investimento de R$ 1.000 continuará intacta.
                    </span>
                  </div>
                )}
              </AnimatePresence>

              {/* Mini Future Cycles preview */}
              <div>
                <span className="text-[11px] font-semibold text-zinc-400 uppercase tracking-wider block mb-2">
                  Compromisso nos próximos {simulation.installments} ciclos:
                </span>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                  {simulation.affectedCycles.slice(0, 6).map((c) => (
                    <div
                      key={c.billingCycle}
                      className={`p-2.5 rounded-xl border text-xs font-mono ${
                        c.willInvade
                          ? 'bg-rose-950/20 border-rose-800/50 text-rose-300'
                          : 'bg-zinc-900 border-zinc-800 text-zinc-300'
                      }`}
                    >
                      <div className="flex items-center justify-between text-[11px] text-zinc-400 mb-1">
                        <span>{c.billingCycle}</span>
                        <span>-R$ {c.installmentAmount.toFixed(0)}</span>
                      </div>
                      <div className="text-xs font-bold">
                        Teto: R$ {c.projectedFreeCapAfter.toFixed(0)}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : (
            <div className="h-full flex items-center justify-center text-zinc-500 text-sm">
              Digite um valor para simular
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
