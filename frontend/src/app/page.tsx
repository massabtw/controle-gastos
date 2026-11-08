'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { api, DashboardSummary, ProjectionCycle, CardExpense } from '@/lib/api';
import { Header } from '@/components/Header';
import { QuickCommandBar } from '@/components/QuickCommandBar';
import { HeroBentoGrid } from '@/components/HeroBentoGrid';
import { PurchaseSimulator } from '@/components/PurchaseSimulator';
import { ProjectionTimeline } from '@/components/ProjectionTimeline';
import { ExpenseFeed } from '@/components/ExpenseFeed';
import {
  NewExpenseModal,
  EditExpenseModal,
  DeleteConfirmModal,
  SettingsModal,
  SyncInvoiceModal,
  UpdateCaixinhaModal,
  PixCaixinhaModal,
} from '@/components/Modals';

export default function Home() {
  const [selectedCycle, setSelectedCycle] = useState<string>('');
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [projections, setProjections] = useState<ProjectionCycle[]>([]);
  const [expenses, setExpenses] = useState<CardExpense[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  // Modals state
  const [isNewExpenseOpen, setIsNewExpenseOpen] = useState(false);
  const [editingExpense, setEditingExpense] = useState<CardExpense | null>(null);
  const [deletingExpense, setDeletingExpense] = useState<CardExpense | null>(null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSyncInvoiceOpen, setIsSyncInvoiceOpen] = useState(false);
  const [isUpdateCaixinhaOpen, setIsUpdateCaixinhaOpen] = useState(false);
  const [isPixCaixinhaOpen, setIsPixCaixinhaOpen] = useState(false);

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
        />

        {/* Lack of Judgment Simulator */}
        <PurchaseSimulator summary={summary} />

        {/* 6-Month Projection Timeline */}
        <ProjectionTimeline
          projections={projections}
          selectedCycle={selectedCycle || summary?.billingCycle || ''}
          onSelectCycle={handleCycleChange}
        />

        {/* Expense Feed */}
        <ExpenseFeed
          expenses={expenses}
          categories={summary?.categoryBreakdown || []}
          selectedCategory={selectedCategory}
          onSelectCategory={setSelectedCategory}
          onEditExpense={(exp) => setEditingExpense(exp)}
          onDeleteExpense={(exp) => setDeletingExpense(exp)}
          onOpenNewExpense={() => setIsNewExpenseOpen(true)}
        />
      </div>

      {/* Modals */}
      <NewExpenseModal
        isOpen={isNewExpenseOpen}
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
    </div>
  );
}
