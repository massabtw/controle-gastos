'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { CalendarRange, Sparkles, ArrowUpRight, Flame } from 'lucide-react';
import { ProjectionCycle } from '@/lib/api';

interface ProjectionTimelineProps {
  projections: ProjectionCycle[];
  selectedCycle: string;
  onSelectCycle: (cycle: string) => void;
}

export function ProjectionTimeline({
  projections,
  selectedCycle,
  onSelectCycle,
}: ProjectionTimelineProps) {
  if (!projections || projections.length === 0) return null;

  return (
    <div className="rounded-3xl border border-zinc-800 bg-zinc-900/80 p-4 sm:p-6 shadow-xl backdrop-blur-md mb-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-5 sm:mb-6 pb-4 border-b border-zinc-800/80">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-xl bg-indigo-500/10 text-indigo-400 shrink-0">
            <CalendarRange className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-bold text-zinc-100 flex items-center gap-2 flex-wrap">
              <span>Projeção dos Próximos 6 Ciclos</span>
              <span className="text-[10px] font-mono uppercase bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 px-2 py-0.5 rounded-full">
                Cash-Flow Evolution
              </span>
            </h2>
            <p className="text-xs text-zinc-400">
              Acompanhe a liberação de teto conforme as parcelas fixas do Monitor terminam.
            </p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3.5">
        {projections.map((proj, idx) => {
          const isSelected = proj.billingCycle === selectedCycle;
          // When fixed commitments drop from 170 to 50 (Monitor ends!)
          const isCapJump = proj.fixedCommitmentsTotal <= 50;

          return (
            <motion.div
              key={proj.billingCycle}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.05 }}
              onClick={() => onSelectCycle(proj.billingCycle)}
              className={`relative cursor-pointer rounded-2xl p-4 border transition-all duration-200 flex flex-col justify-between ${
                isSelected
                  ? 'bg-zinc-800/90 border-emerald-500 shadow-lg ring-1 ring-emerald-500/50'
                  : isCapJump
                  ? 'bg-indigo-950/20 border-indigo-500/40 hover:bg-indigo-950/30'
                  : 'bg-zinc-950/60 border-zinc-800/80 hover:bg-zinc-800/50'
              }`}
            >
              {isCapJump && (
                <div className="absolute -top-2.5 right-2 bg-gradient-to-r from-indigo-500 to-purple-500 text-zinc-950 text-[9px] font-black uppercase px-2 py-0.5 rounded-full shadow-sm flex items-center gap-0.5">
                  <Sparkles className="w-2.5 h-2.5" />
                  +R$ 120 livre
                </div>
              )}

              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="font-mono text-xs font-bold text-zinc-200">
                    {proj.billingCycle}
                  </span>
                  <ArrowUpRight className="w-3.5 h-3.5 text-zinc-500" />
                </div>

                <div className="my-2">
                  <span className="text-[10px] text-zinc-400 block">Teto Projetado:</span>
                  <span className="font-mono text-base font-bold text-emerald-400">
                    R$ {proj.projectedFreeCap.toLocaleString('pt-BR', { minimumFractionDigits: 0 })}
                  </span>
                </div>
              </div>

              <div className="pt-2 mt-2 border-t border-zinc-800/80 space-y-1 text-[11px] font-mono">
                <div className="flex items-center justify-between text-zinc-400">
                  <span>Pix Mãe:</span>
                  <span className={isCapJump ? 'text-indigo-300 font-bold' : 'text-zinc-300'}>
                    R$ {proj.fixedCommitmentsTotal.toFixed(0)}
                  </span>
                </div>
                <div className="flex items-center justify-between text-zinc-400">
                  <span>Parcelas:</span>
                  <span className="text-zinc-300">
                    R$ {proj.committedCardInstallments.toFixed(0)}
                  </span>
                </div>
                <div className="flex items-center justify-between pt-1 border-t border-zinc-800/60 text-zinc-200 font-bold">
                  <span className="flex items-center gap-1 text-[10px] text-zinc-400">
                    <Flame className="w-3 h-3 text-amber-400" /> Queima:
                  </span>
                  <span className="text-amber-400">
                    R$ {proj.projectedDailyBurnRate.toFixed(2)}/d
                  </span>
                </div>
              </div>
            </motion.div>
          );
        })}
      </div>
    </div>
  );
}
