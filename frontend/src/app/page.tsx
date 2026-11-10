'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { Flame, ArrowRight } from 'lucide-react';
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
  DepositCaixinhaModal,
  ReceivePixModal,
  PinModal,
} from '@/components/Modals';

interface AdminLockGateProps {
  onUnlock: (token: string) => void;
  onEnterAsGuest: () => void;
}

function AdminLockGate({ onUnlock, onEnterAsGuest }: AdminLockGateProps) {
  const [pin, setPin] = useState('');
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const cleanedPin = pin.trim();
    if (!cleanedPin) {
      setErrorMessage('Digite o PIN de administrador.');
      return;
    }

    if (cleanedPin.toLowerCase() === 'uchiha' || cleanedPin === '1212') {
      setLoading(true);
      setErrorMessage(null);
      try {
        const res = await api.loginAdmin(cleanedPin);
        const tokenToSave = res?.token || cleanedPin;
        api.setAdminToken(tokenToSave);
        onUnlock(tokenToSave);
      } catch {
        api.setAdminToken(cleanedPin);
        onUnlock(cleanedPin);
      } finally {
        setLoading(false);
      }
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    try {
      const res = await api.loginAdmin(cleanedPin);
      if (res && res.token) {
        api.setAdminToken(res.token);
        onUnlock(res.token);
      } else {
        setErrorMessage('PIN incorreto. Digite o PIN correto.');
        setPin('');
      }
    } catch {
      setErrorMessage('PIN incorreto. Tente novamente.');
      setPin('');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen w-full bg-zinc-950 text-zinc-100 flex flex-col items-center justify-center p-4 relative overflow-hidden select-none font-sans selection:bg-emerald-500/30 selection:text-emerald-300">
      {/* Background ambient lighting */}
      <div className="fixed inset-0 overflow-hidden pointer-events-none">
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[500px] bg-emerald-600/10 blur-[140px] rounded-full" />
      </div>

      <div className="relative w-full max-w-sm mx-auto flex flex-col items-center text-center">
        {/* Brand Icon */}
        <div className="w-16 h-16 rounded-3xl bg-gradient-to-br from-emerald-500 to-emerald-700 flex items-center justify-center shadow-xl shadow-emerald-500/20 text-zinc-950 mb-4 transition-transform active:scale-95">
          <Flame className="w-9 h-9 fill-zinc-950" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight text-zinc-100 mb-1">BurnTracker</h1>
        <p className="text-xs text-zinc-400 mb-6">Painel Protegido • Digite o PIN de Acesso</p>

        {/* Lock Card */}
        <form
          onSubmit={handleSubmit}
          className="w-full bg-zinc-900/80 border border-zinc-800/90 rounded-3xl p-6 shadow-2xl backdrop-blur-md space-y-4"
        >
          <div className="space-y-2">
            <label className="block text-xs font-semibold text-zinc-400 uppercase tracking-wider">
              PIN de Acesso
            </label>
            <input
              type="password"
              autoCapitalize="none"
              autoCorrect="off"
              spellCheck={false}
              autoFocus
              value={pin}
              onChange={(e) => {
                setPin(e.target.value);
                setErrorMessage(null);
              }}
              placeholder="••••••"
              className="w-full min-h-[56px] text-center tracking-[0.35em] text-xl font-mono font-bold px-4 py-2 rounded-2xl bg-zinc-950 border border-zinc-800 text-zinc-100 placeholder:text-zinc-600 focus:outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 transition-all"
            />
            {errorMessage && (
              <p className="text-xs text-rose-400 font-medium pt-1">
                ⚠️ {errorMessage}
              </p>
            )}
          </div>

          <button
            type="submit"
            disabled={loading || !pin.trim()}
            className="w-full min-h-[48px] rounded-2xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-sm font-bold shadow-lg shadow-emerald-500/20 transition-all active:scale-[0.98] disabled:opacity-40 disabled:cursor-not-allowed flex items-center justify-center gap-2 cursor-pointer"
          >
            {loading ? (
              <span>Validando...</span>
            ) : (
              <>
                <span>Desbloquear</span>
                <ArrowRight className="w-4 h-4 stroke-[2.5]" />
              </>
            )}
          </button>

          <div className="relative flex py-1 items-center">
            <div className="flex-grow border-t border-zinc-800"></div>
            <span className="flex-shrink mx-3 text-[11px] text-zinc-500 uppercase tracking-wider font-semibold">ou</span>
            <div className="flex-grow border-t border-zinc-800"></div>
          </div>

          <button
            type="button"
            onClick={onEnterAsGuest}
            className="w-full min-h-[46px] rounded-2xl bg-zinc-950 hover:bg-zinc-800/90 border border-zinc-800 text-zinc-300 hover:text-zinc-100 text-xs sm:text-sm font-semibold shadow-md transition-all active:scale-[0.98] flex items-center justify-center gap-2 cursor-pointer"
          >
            <span>👀 Entrar como Visitante (Valores Ocultos)</span>
          </button>

          <p className="text-[11px] text-zinc-500 pt-1 leading-relaxed">
            O token de acesso permanecerá ativo por 90 dias neste dispositivo.
          </p>
        </form>
      </div>
    </div>
  );
}

export default function Home() {
  const [selectedCycle, setSelectedCycle] = useState<string>('');
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [projections, setProjections] = useState<ProjectionCycle[]>([]);
  const [expenses, setExpenses] = useState<CardExpense[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [mainTab, setMainTab] = useState<'expenses' | 'subscriptions' | 'projections'>('expenses');

  // Admin security & Guest Mode
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);
  const [isAdmin, setIsAdmin] = useState(false);
  const [isGuest, setIsGuest] = useState(false);

  // Modals state
  const [isNewExpenseOpen, setIsNewExpenseOpen] = useState(false);
  const [editingExpense, setEditingExpense] = useState<CardExpense | null>(null);
  const [deletingExpense, setDeletingExpense] = useState<CardExpense | null>(null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isSyncInvoiceOpen, setIsSyncInvoiceOpen] = useState(false);
  const [isUpdateCaixinhaOpen, setIsUpdateCaixinhaOpen] = useState(false);
  const [isDepositCaixinhaOpen, setIsDepositCaixinhaOpen] = useState(false);
  const [isReceivePixOpen, setIsReceivePixOpen] = useState(false);
  const [isPixCaixinhaOpen, setIsPixCaixinhaOpen] = useState(false);
  const [isEditCardLimitOpen, setIsEditCardLimitOpen] = useState(false);
  const [isNewSubscriptionOpen, setIsNewSubscriptionOpen] = useState(false);
  const [isPinModalOpen, setIsPinModalOpen] = useState(false);
  const [isServerWakingUp, setIsServerWakingUp] = useState(false);

  // Check initial admin token on mount
  useEffect(() => {
    const checkAdmin = async () => {
      const token = api.getAdminToken();
      if (!token) {
        setIsAdmin(false);
        setIsCheckingAuth(false);
        return;
      }

      if (token.trim() === '1212' || token.trim().toLowerCase() === 'uchiha') {
        setIsAdmin(true);
        setIsGuest(false);
        setIsCheckingAuth(false);
        return;
      }

      try {
        const res = await api.verifyAdmin(token);
        if (res && res.valid) {
          setIsAdmin(true);
          setIsGuest(false);
        } else {
          api.clearAdminToken();
          setIsAdmin(false);
        }
      } catch {
        // If server is cold or token invalid, default to lock screen
        api.clearAdminToken();
        setIsAdmin(false);
      } finally {
        setIsCheckingAuth(false);
      }
    };

    checkAdmin();
  }, []);

  useEffect(() => {
    const handleWakingUp = (e: Event) => {
      const customEvent = e as CustomEvent<boolean>;
      setIsServerWakingUp(Boolean(customEvent.detail));
    };
    const handlePinRequired = () => {
      api.clearAdminToken();
      setIsAdmin(false);
      setIsGuest(false);
      setIsPinModalOpen(false);
    };

    window.addEventListener('render_server_waking_up', handleWakingUp);
    window.addEventListener('finance_app_pin_required', handlePinRequired);

    return () => {
      window.removeEventListener('render_server_waking_up', handleWakingUp);
      window.removeEventListener('finance_app_pin_required', handlePinRequired);
    };
  }, []);

  const guardAdminAction = (action: () => void) => {
    if (!isAdmin) {
      setIsPinModalOpen(true);
      return;
    }
    action();
  };

  const handleLogoutAdmin = () => {
    if (confirm('Deseja bloquear o painel e voltar à tela de bloqueio?')) {
      api.clearAdminToken();
      setIsAdmin(false);
      setIsGuest(false);
    }
  };

  const loadData = useCallback(async () => {
    if (!isAdmin && !isGuest) return;
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
  }, [isAdmin, isGuest, selectedCycle]);

  useEffect(() => {
    if (isAdmin || isGuest) {
      loadData();
    }
  }, [isAdmin, isGuest, loadData]);

  const handleCycleChange = (cycle: string) => {
    setSelectedCycle(cycle);
    setSelectedCategory(null);
  };

  if (isCheckingAuth) {
    return (
      <div className="min-h-screen w-full bg-zinc-950 flex flex-col items-center justify-center p-4">
        <div className="w-16 h-16 rounded-3xl bg-gradient-to-br from-emerald-500 to-emerald-700 flex items-center justify-center shadow-xl shadow-emerald-500/20 text-zinc-950 animate-pulse">
          <Flame className="w-9 h-9 fill-zinc-950" />
        </div>
      </div>
    );
  }

  if (!isAdmin && !isGuest) {
    return (
      <AdminLockGate
        onUnlock={() => {
          setIsAdmin(true);
          setIsGuest(false);
        }}
        onEnterAsGuest={() => {
          setIsGuest(true);
          setIsAdmin(false);
        }}
      />
    );
  }

  return (
    <div
      data-privacy={isGuest ? 'true' : 'false'}
      className="min-h-screen w-full overflow-x-hidden bg-zinc-950 text-zinc-100 font-sans selection:bg-emerald-500/30 selection:text-emerald-300"
    >
      {/* Background ambient lighting */}
      <div className="fixed inset-0 overflow-hidden pointer-events-none">
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[350px] bg-emerald-600/5 blur-[120px] rounded-full" />
      </div>

      <div className="relative w-full max-w-md md:max-w-5xl lg:max-w-7xl mx-auto px-4 md:px-6 lg:px-8 pb-20">
        {/* Header */}
        <Header
          summary={summary}
          selectedCycle={selectedCycle || summary?.billingCycle || '...'}
          onCycleChange={handleCycleChange}
          onOpenNewExpense={() => guardAdminAction(() => setIsNewExpenseOpen(true))}
          onOpenPixCaixinha={() => guardAdminAction(() => setIsPixCaixinhaOpen(true))}
          onOpenReceivePix={() => guardAdminAction(() => setIsReceivePixOpen(true))}
          onOpenSyncInvoice={() => guardAdminAction(() => setIsSyncInvoiceOpen(true))}
          onOpenSettings={() => guardAdminAction(() => setIsSettingsOpen(true))}
          onOpenPinModal={() => setIsPinModalOpen(true)}
          isAdmin={isAdmin}
          isGuest={isGuest}
          onLogoutAdmin={handleLogoutAdmin}
        />

        {/* Cloud Server Wake-up Notice (Render Free Tier Cold Start) */}
        {isServerWakingUp && (
          <div className="mb-6 p-4 bg-amber-500/10 border border-amber-500/30 rounded-2xl flex items-center gap-3.5 text-amber-200 text-xs sm:text-sm animate-pulse shadow-lg shadow-amber-500/5">
            <div className="w-2.5 h-2.5 rounded-full bg-amber-400 shrink-0 animate-ping" />
            <div className="flex-1">
              <span className="font-bold">Servidor em nuvem acordando...</span> Aguarde alguns instantes enquanto a instância gratuita do Render inicia. Seus dados estão seguros e o painel atualizará automaticamente.
            </div>
          </div>
        )}

        {/* AI Quick Command Bar */}
        <QuickCommandBar onRefresh={loadData} />

        {/* Core Hero Bento Grid */}
        <HeroBentoGrid
          summary={summary}
          onOpenUpdateCaixinha={() => guardAdminAction(() => setIsUpdateCaixinhaOpen(true))}
          onOpenDepositCaixinha={() => guardAdminAction(() => setIsDepositCaixinhaOpen(true))}
          onOpenReceivePix={() => guardAdminAction(() => setIsReceivePixOpen(true))}
          onOpenEditCardLimit={() => guardAdminAction(() => setIsEditCardLimitOpen(true))}
          onCommitmentPaid={loadData}
          onInvoiceStatusChanged={loadData}
          isAdmin={isAdmin}
          onRequireAdmin={() => setIsPinModalOpen(true)}
        />

        {/* Lack of Judgment Simulator */}
        <PurchaseSimulator summary={summary} />

        {/* Navigation Tabs */}
        <div className="w-full max-w-xl grid grid-cols-3 gap-1.5 mb-6 p-1.5 bg-zinc-900/90 rounded-2xl border border-zinc-800/80 backdrop-blur-md">
          <button
            onClick={() => setMainTab('expenses')}
            className={`min-h-[44px] py-2 px-2 sm:px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap ${
              mainTab === 'expenses'
                ? 'bg-zinc-800 text-zinc-100 shadow-md border border-zinc-700/60'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <span className="sm:hidden">📋 Cartão</span>
            <span className="hidden sm:inline">📋 Lançamentos do Cartão</span>
            <span className="text-[10px] font-mono px-1.5 py-0.5 rounded-full bg-zinc-700/60 text-zinc-200">
              {expenses.length}
            </span>
          </button>

          <button
            onClick={() => setMainTab('subscriptions')}
            className={`min-h-[44px] py-2 px-2 sm:px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap ${
              mainTab === 'subscriptions'
                ? 'bg-purple-600/20 text-purple-300 border border-purple-500/30 shadow-md'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <span className="sm:hidden">🔄 Fixas</span>
            <span className="hidden sm:inline">🔄 Assinaturas Fixas</span>
            {summary?.activeSubscriptions && summary.activeSubscriptions.length > 0 && (
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded-full bg-purple-500/30 text-purple-200 font-bold">
                {summary.activeSubscriptions.length}
              </span>
            )}
          </button>

          <button
            onClick={() => setMainTab('projections')}
            className={`min-h-[44px] py-2 px-2 sm:px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer whitespace-nowrap ${
              mainTab === 'projections'
                ? 'bg-blue-600/20 text-blue-300 border border-blue-500/30 shadow-md'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            <span className="sm:hidden">📅 6 Meses</span>
            <span className="hidden sm:inline">📅 Projeção 6 Meses</span>
          </button>
        </div>

        {/* Tab Content */}
        {mainTab === 'expenses' && (
          <ExpenseFeed
            expenses={expenses}
            categories={summary?.categoryBreakdown || []}
            selectedCategory={selectedCategory}
            onSelectCategory={setSelectedCategory}
            onEditExpense={(exp) => guardAdminAction(() => setEditingExpense(exp))}
            onDeleteExpense={(exp) => guardAdminAction(() => setDeletingExpense(exp))}
            onOpenNewExpense={() => guardAdminAction(() => setIsNewExpenseOpen(true))}
          />
        )}

        {mainTab === 'subscriptions' && (
          <SubscriptionsTab
            summary={summary}
            onOpenNewSubscription={() => guardAdminAction(() => setIsNewSubscriptionOpen(true))}
            onUpdate={loadData}
            isAdmin={isAdmin}
            onRequireAdmin={() => setIsPinModalOpen(true)}
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

      <DepositCaixinhaModal
        isOpen={isDepositCaixinhaOpen}
        currentBalance={summary?.nubankCaixinhaBalance ?? 0}
        cdiAnnualRate={summary?.cdiAnnualRate ?? 11.25}
        onClose={() => setIsDepositCaixinhaOpen(false)}
        onSuccess={loadData}
      />

      <ReceivePixModal
        isOpen={isReceivePixOpen}
        currentBalance={summary?.nubankCaixinhaBalance ?? 0}
        onClose={() => setIsReceivePixOpen(false)}
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
        activeCycle={summary?.currentActiveCycle || summary?.billingCycle}
        currentCycle={summary?.currentActiveCycle || summary?.billingCycle}
        onClose={() => setIsNewSubscriptionOpen(false)}
        onSuccess={loadData}
      />

      <PinModal
        isOpen={isPinModalOpen}
        onClose={() => setIsPinModalOpen(false)}
        onSuccess={(token) => {
          if (token) {
            setIsAdmin(true);
            setIsGuest(false);
          }
          loadData();
        }}
      />
    </div>
  );
}
