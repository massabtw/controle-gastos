'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { api, DashboardSummary, ProjectionCycle, CardExpense } from '@/lib/api';
import { Header } from '@/components/Header';
import { QuickCommandBar } from '@/components/QuickCommandBar';
import { HeroBentoGrid } from '@/components/HeroBentoGrid';
import { PurchaseSimulator } from '@/components/PurchaseSimulator';
import { ProjectionTimeline } from '@/components/ProjectionTimeline';
import { ExpenseFeed } from '@/components/ExpenseFeed';
import { SubscriptionsTab } from '@/components/SubscriptionsTab';
import {
  NewExpenseModal,
  EditExpenseModal,
  DeleteConfirmModal,
  SettingsModal,
  SyncInvoiceModal,
  UpdateCaixinhaModal,
  PixCaixinhaModal,
  EditCardLimitModal,
  NewSubscriptionModal,
} from '@/components/Modals';

export default function Home() {
  const [selectedCycle, setSelectedCycle] = useState<string>('');
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [projections, setProjections] = useState<ProjectionCycle[]>([]);
  const [expenses, setExpenses] = useState<CardExpense[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [mainTab, setMainTab] = useState<'expenses' | 'subscriptions' | 'projections'>('expenses');

  // Modals state
  const [isNewExpenseOpen, setIsNewExpenseOpen] = useState(false);
  const [editingExpense, setEditingExpense] = useState<CardExpense | null>(null);
  const [deletingExpense, setDeletingExpense] = useState<CardExpense | null>(null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSyncInvoiceOpen, setIsSyncInvoiceOpen] = useState(false);
  const [isUpdateCaixinhaOpen, setIsUpdateCaixinhaOpen] = useState(false);
  const [isPixCaixinhaOpen, setIsPixCaixinhaOpen] = useState(false);
  const [isEditCardLimitOpen, setIsEditCardLimitOpen] = useState(false);
  const [isNewSubscriptionOpen, setIsNewSubscriptionOpen] = useState(false);

  const loadData = useCallback(async () => {
    try {
      const [sum, proj] = await Promise.all([
        api.getDashboard(selectedCycle || undefined),
        api.getProjections(6),
      ]);

      setSummary(sum);
      setProjections(proj);

      // If no cycle was selected yet, set it to the active cycle returned by dashboard
      if (!selectedCycle) {
        setSelectedCycle(sum.billingCycle);
      }

      // Fetch expenses for the cycle
      const exp = await api.getExpenses(selectedCycle || sum.billingCycle);
      setExpenses(exp);
    } catch (err) {
      console.error('Error loading dashboard data:', err);
    }
  }, [selectedCycle]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleCycleChange = (cycle: string) => {
    setSelectedCycle(cycle);
    setSelectedCategory(null);
  };

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100 font-sans selection:bg-emerald-500/30 selection:text-emerald-300">
      {/* Background ambient lighting */}
      <div className="fixed inset-0 overflow-hidden pointer-events-none">
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[350px] bg-emerald-600/5 blur-[120px] rounded-full" />
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-16">
        {/* Header */}
        <Header
          summary={summary}
          selectedCycle={selectedCycle || summary?.billingCycle || '...'}
          onCycleChange={handleCycleChange}
          onOpenNewExpense={() => setIsNewExpenseOpen(true)}
          onOpenPixCaixinha={() => setIsPixCaixinhaOpen(true)}
          onOpenSyncInvoice={() => setIsSyncInvoiceOpen(true)}
          onOpenSettings={() => setIsSettingsOpen(true)}
        />

        {/* AI Quick Command Bar */}
        <QuickCommandBar onRefresh={loadData} />

        {/* Core Hero Bento Grid */}
        <HeroBentoGrid
          summary={summary}
          onOpenUpdateCaixinha={() => setIsUpdateCaixinhaOpen(true)}
          onOpenEditCardLimit={() => setIsEditCardLimitOpen(true)}
          onCommitmentPaid={loadData}
          onInvoiceStatusChanged={loadData}
        />

        {/* Lack of Judgment Simulator */}
        <PurchaseSimulator summary={summary} />

        {/* Navigation Tabs */}
        <div className="flex items-center gap-2 mb-6 p-1.5 bg-zinc-900/80 rounded-2xl border border-zinc-800/80 backdrop-blur-md max-w-xl">
          <button
            onClick={() => setMainTab('expenses')}
            className={`flex-1 py-2 px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-2 cursor-pointer ${
              mainTab === 'expenses'
                ? 'bg-zinc-800 text-zinc-100 shadow-md border border-zinc-700/60'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <span>📋 Lançamentos do Cartão</span>
            <span className="text-[10px] font-mono px-1.5 py-0.5 rounded-full bg-zinc-700/50 text-zinc-300">
              {expenses.length}
            </span>
          </button>

          <button
            onClick={() => setMainTab('subscriptions')}
            className={`flex-1 py-2 px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-2 cursor-pointer ${
              mainTab === 'subscriptions'
                ? 'bg-purple-600/20 text-purple-300 border border-purple-500/30 shadow-md'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <span>🔄 Assinaturas Fixas</span>
            {summary?.activeSubscriptions && summary.activeSubscriptions.length > 0 && (
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded-full bg-purple-500/30 text-purple-200 font-bold">
                {summary.activeSubscriptions.length}
              </span>
            )}
          </button>

          <button
            onClick={() => setMainTab('projections')}
            className={`flex-1 py-2 px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-2 cursor-pointer ${
              mainTab === 'projections'
                ? 'bg-blue-600/20 text-blue-300 border border-blue-500/30 shadow-md'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <span>📅 Projeção 6 Meses</span>
          </button>
        </div>

        {/* Tab Content */}
        {mainTab === 'expenses' && (
          <ExpenseFeed
            expenses={expenses}
            categories={summary?.categoryBreakdown || []}
            selectedCategory={selectedCategory}
            onSelectCategory={setSelectedCategory}
            onEditExpense={(exp) => setEditingExpense(exp)}
            onDeleteExpense={(exp) => setDeletingExpense(exp)}
            onOpenNewExpense={() => setIsNewExpenseOpen(true)}
          />
        )}

        {mainTab === 'subscriptions' && (
          <SubscriptionsTab
            summary={summary}
            onOpenNewSubscription={() => setIsNewSubscriptionOpen(true)}
            onUpdate={loadData}
          />
        )}

        {mainTab === 'projections' && (
          <ProjectionTimeline
            projections={projections}
            selectedCycle={selectedCycle || summary?.billingCycle || ''}
            onSelectCycle={handleCycleChange}
          />
        )}
      </div>

      {/* Modals */}
      <NewExpenseModal
        isOpen={isNewExpenseOpen}
        currentCaixinhaBalance={summary?.nubankCaixinhaBalance ?? 0}
        onClose={() => setIsNewExpenseOpen(false)}
        onSuccess={loadData}
      />

      <EditExpenseModal
        expense={editingExpense}
        onClose={() => setEditingExpense(null)}
        onSuccess={loadData}
      />

      <DeleteConfirmModal
        expense={deletingExpense}
        onClose={() => setDeletingExpense(null)}
        onSuccess={loadData}
      />

      <SettingsModal
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        onSuccess={loadData}
      />

      <SyncInvoiceModal
        isOpen={isSyncInvoiceOpen}
        currentTotal={summary?.currentInvoiceTotal ?? 0}
        cycle={selectedCycle || summary?.billingCycle || ''}
        onClose={() => setIsSyncInvoiceOpen(false)}
        onSuccess={loadData}
      />

      <UpdateCaixinhaModal
        isOpen={isUpdateCaixinhaOpen}
        currentBalance={summary?.nubankCaixinhaBalance ?? 0}
        cdiAnnualRate={summary?.cdiAnnualRate ?? 11.25}
        onClose={() => setIsUpdateCaixinhaOpen(false)}
        onSuccess={loadData}
      />

      <PixCaixinhaModal
        isOpen={isPixCaixinhaOpen}
        currentCaixinhaBalance={summary?.nubankCaixinhaBalance ?? 0}
        onClose={() => setIsPixCaixinhaOpen(false)}
        onSuccess={loadData}
      />

      <EditCardLimitModal
        isOpen={isEditCardLimitOpen}
        currentCreditCardLimit={summary?.creditCardLimit ?? 2500}
        currentCustomCardCap={summary?.customCardCap ?? null}
        formulaCap={summary?.cardHardCapFormula ?? 1030}
        onClose={() => setIsEditCardLimitOpen(false)}
        onSuccess={loadData}
      />

      <NewSubscriptionModal
        isOpen={isNewSubscriptionOpen}
        currentCycle={selectedCycle || summary?.billingCycle}
        onClose={() => setIsNewSubscriptionOpen(false)}
        onSuccess={loadData}
      />
    </div>
  );
}
