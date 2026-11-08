'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import {
  RefreshCw,
  Plus,
  CreditCard,
  PiggyBank,
  Trash2,
  CheckCircle2,
  PauseCircle,
  Calendar,
} from 'lucide-react';
import { api, DashboardSummary, FixedCommitment } from '@/lib/api';

interface SubscriptionsTabProps {
  summary: DashboardSummary | null;
  onOpenNewSubscription: () => void;
  onUpdate: () => void;
}

export function SubscriptionsTab({
  summary,
  onOpenNewSubscription,
  onUpdate,
}: SubscriptionsTabProps) {
  const [subscriptions, setSubscriptions] = useState<FixedCommitment[]>([]);
  const [loading, setLoading] = useState(false);
  const [togglingId, setTogglingId] = useState<number | null>(null);

  const fetchSubscriptions = async () => {
    setLoading(true);
    try {
      const data = await api.getProfile();
      // Filter commitments that are subscriptions (or indefinite recurring fixed commitments)
      const subs = data.commitments.filter(
        (c) => c.commitmentType === 'SUBSCRIPTION' || c.totalInstallments === null
      );
      setSubscriptions(subs);
    } catch (err) {
      console.error('Error fetching subscriptions:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSubscriptions();
  }, []);

  const handleToggleActive = async (sub: FixedCommitment) => {
    setTogglingId(sub.id);
    try {
      await api.updateCommitment(sub.id, {
        ...sub,
        isActive: !sub.isActive,
      });
      await fetchSubscriptions();
      onUpdate();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao alterar status';
      alert(`Erro: ${message}`);
    } finally {
      setTogglingId(null);
    }
  };

  const handleDelete = async (id: number, title: string) => {
    if (!confirm(`Deseja realmente remover a assinatura "${title}"?`)) return;

    try {
      await api.deleteCommitment(id);
      await fetchSubscriptions();
      onUpdate();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao remover assinatura';
      alert(`Erro: ${message}`);
    }
  };

  const activeSubs = subscriptions.filter((s) => s.isActive);
  const cardSubsTotal = activeSubs
    .filter((s) => s.paymentSource === 'CARD')
    .reduce((acc, s) => acc + s.amount, 0);
  const totalSubsMonthly = activeSubs.reduce((acc, s) => acc + s.amount, 0);

  const hardCap = summary?.cardHardCap || 1030;
  const cardPctOfCap = hardCap > 0 ? (cardSubsTotal / hardCap) * 100 : 0;

  return (
    <div className="space-y-6">
      {/* Top Metric Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Total Monthly Subscriptions */}
        <div className="p-5 rounded-3xl bg-zinc-900/80 border border-zinc-800 shadow-xl backdrop-blur-md">
          <div className="flex items-center justify-between text-zinc-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">
              Total em Assinaturas
            </span>
            <div className="p-2 rounded-xl bg-purple-500/10 text-purple-400">
              <RefreshCw className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-black font-mono text-zinc-100">
            R${' '}
            {totalSubsMonthly.toLocaleString('pt-BR', {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}
            <span className="text-xs font-sans text-zinc-400 font-normal"> / mês</span>
          </div>
          <span className="text-[11px] text-zinc-500 mt-1 block">
            {activeSubs.length} {activeSubs.length === 1 ? 'assinatura ativa' : 'assinaturas ativas'}
          </span>
        </div>

        {/* Card Subscriptions Impact on Hard Cap */}
        <div className="p-5 rounded-3xl bg-zinc-900/80 border border-zinc-800 shadow-xl backdrop-blur-md">
          <div className="flex items-center justify-between text-zinc-400 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">
              Comprometimento do Teto (Cartão)
            </span>
            <div className="p-2 rounded-xl bg-emerald-500/10 text-emerald-400">
              <CreditCard className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-black font-mono text-emerald-400">
            {cardPctOfCap.toFixed(1)}%
            <span className="text-xs font-sans text-zinc-400 font-normal"> do teto</span>
          </div>
          <span className="text-[11px] text-zinc-400 font-mono mt-1 block">
            R$ {cardSubsTotal.toFixed(2)} de R$ {hardCap.toFixed(2)} consumidos fixos todo mês
          </span>
        </div>

        {/* Action Button Card */}
        <div className="p-5 rounded-3xl bg-gradient-to-br from-purple-950/30 to-zinc-900/80 border border-purple-500/30 shadow-xl flex flex-col justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-purple-300 block mb-1">
              Gerenciar Recorrências
            </span>
            <p className="text-[11px] text-zinc-400 leading-tight">
              Assinaturas no Cartão já entram calculadas na fatura de cada mês automaticamente.
            </p>
          </div>
          <button
            onClick={onOpenNewSubscription}
            className="mt-3 w-full py-2.5 px-4 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold transition-all shadow-md flex items-center justify-center gap-1.5 cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            <span>+ Nova Assinatura</span>
          </button>
        </div>
      </div>

      {/* Subscriptions List */}
      <div className="rounded-3xl border border-zinc-800 bg-zinc-900/70 p-6 shadow-xl backdrop-blur-md">
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800 mb-4">
          <div className="flex items-center gap-2">
            <h3 className="text-base font-bold text-zinc-100">Assinaturas Cadastradas</h3>
            <span className="text-xs font-mono text-zinc-400 bg-zinc-800 px-2 py-0.5 rounded-full">
              {subscriptions.length}
            </span>
          </div>
          <button
            onClick={fetchSubscriptions}
            disabled={loading}
            className="p-1.5 rounded-lg text-zinc-400 hover:text-zinc-200 hover:bg-zinc-800 transition-colors"
            title="Atualizar lista"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>

        {subscriptions.length === 0 ? (
          <div className="py-12 text-center">
            <div className="w-12 h-12 rounded-2xl bg-zinc-800/80 text-zinc-500 flex items-center justify-center mx-auto mb-3">
              <RefreshCw className="w-6 h-6" />
            </div>
            <h4 className="text-sm font-bold text-zinc-300 mb-1">Nenhuma assinatura cadastrada</h4>
            <p className="text-xs text-zinc-500 max-w-sm mx-auto mb-4">
              Cadastre aqui serviços recorrentes como Netflix, Spotify, Google One, internet ou academia para mantê-los sob controle no teto mensal.
            </p>
            <button
              onClick={onOpenNewSubscription}
              className="py-2 px-4 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold transition-all shadow-md inline-flex items-center gap-1.5 cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Cadastrar Primeira Assinatura</span>
            </button>
          </div>
        ) : (
          <div className="space-y-2.5">
            {subscriptions.map((sub) => {
              const isCard = sub.paymentSource === 'CARD';
              const isCaixinha = sub.paymentSource === 'PIX_CAIXINHA';

              return (
                <motion.div
                  key={sub.id}
                  layout
                  initial={{ opacity: 0, y: 5 }}
                  animate={{ opacity: 1, y: 0 }}
                  className={`p-4 rounded-2xl border transition-all flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 ${
                    sub.isActive
                      ? 'bg-zinc-950/70 border-zinc-800/90'
                      : 'bg-zinc-950/30 border-zinc-900 opacity-60'
                  }`}
                >
                  {/* Left Info */}
                  <div className="flex items-center gap-3">
                    <div
                      className={`p-2.5 rounded-xl ${
                        isCard
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                          : isCaixinha
                          ? 'bg-cyan-500/10 text-cyan-400 border border-cyan-500/20'
                          : 'bg-blue-500/10 text-blue-400 border border-blue-500/20'
                      }`}
                    >
                      {isCard ? (
                        <CreditCard className="w-4 h-4" />
                      ) : (
                        <PiggyBank className="w-4 h-4" />
                      )}
                    </div>

                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-sm text-zinc-100">{sub.title}</span>
                        <span
                          className={`text-[10px] font-mono px-2 py-0.5 rounded-full font-semibold ${
                            isCard
                              ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
                              : 'bg-cyan-500/15 text-cyan-400 border border-cyan-500/30'
                          }`}
                        >
                          {isCard ? '💳 Fatura Cartão' : '🐷 Pix Caixinha'}
                        </span>
                        {!sub.isActive && (
                          <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-zinc-800 text-zinc-400">
                            Pausada
                          </span>
                        )}
                      </div>

                      <div className="flex items-center gap-3 text-xs text-zinc-400 mt-1 font-mono">
                        {sub.billingDay && (
                          <span className="flex items-center gap-1">
                            <Calendar className="w-3 h-3 text-zinc-500" />
                            Cobrança: dia {sub.billingDay}
                          </span>
                        )}
                        <span>•</span>
                        <span>Início: {sub.startCycle}</span>
                        {isCard && (
                          <>
                            <span>•</span>
                            <span className="text-emerald-400/90">Soma na fatura mensal</span>
                          </>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Right Actions */}
                  <div className="flex items-center gap-4 self-end sm:self-center">
                    <span className="text-base font-black font-mono text-zinc-100">
                      R${' '}
                      {sub.amount.toLocaleString('pt-BR', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })}
                      <span className="text-[10px] font-sans text-zinc-500 font-normal"> / mês</span>
                    </span>

                    {/* Toggle Active */}
                    <button
                      onClick={() => handleToggleActive(sub)}
                      disabled={togglingId === sub.id}
                      className={`p-1.5 rounded-xl border transition-colors cursor-pointer ${
                        sub.isActive
                          ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30 hover:bg-emerald-500/20'
                          : 'bg-zinc-800 text-zinc-500 border-zinc-700 hover:text-zinc-300'
                      }`}
                      title={sub.isActive ? 'Pausar assinatura' : 'Ativar assinatura'}
                    >
                      {sub.isActive ? (
                        <CheckCircle2 className="w-4 h-4" />
                      ) : (
                        <PauseCircle className="w-4 h-4" />
                      )}
                    </button>

                    {/* Delete */}
                    <button
                      onClick={() => handleDelete(sub.id, sub.title)}
                      className="p-1.5 rounded-xl text-zinc-500 hover:text-rose-400 hover:bg-rose-500/10 transition-colors cursor-pointer"
                      title="Excluir assinatura"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </motion.div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
