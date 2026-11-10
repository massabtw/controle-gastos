const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5154';

export interface ActiveCommitment {
  id: number;
  title: string;
  amount: number;
  badge: string; // e.g. "1/5" or "Fixo" or "Mensal"
  commitmentType?: 'PIX_COMMITMENT' | 'SUBSCRIPTION';
  paymentSource?: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
  billingDay?: number | null;
  lastPaidCycle?: string | null;
  isPaidInCycle?: boolean;
}

export interface CategoryBreakdown {
  category: string;
  amount: number;
  percentage: number;
}

export interface DashboardSummary {
  billingCycle: string;
  currentActiveCycle?: string;
  cycleClosingDate: string;
  cycleDueDate: string;
  cycleState: 'CURRENT' | 'FUTURE' | 'CLOSED';
  totalIncome: number;
  incomeFirstTranche: number;
  incomeSecondTranche: number;
  monthlyInvestmentGoal: number;
  creditCardLimit: number;
  customCardCap: number | null;
  availableBankCreditLimit: number;
  totalUnpaidCardUsage?: number;
  activeFixedCommitments: ActiveCommitment[];
  totalFixedCommitments: number;
  activeSubscriptions: ActiveCommitment[];
  cardSubscriptionsTotal: number;
  cardHardCap: number;
  cardHardCapFormula: number;
  currentInvoiceTotal: number;
  pixCaixinhaUsedInCycle: number;
  remainingBudget: number;
  daysUntilClosing: number;
  availablePerDay: number;
  status: 'HEALTHY' | 'WARNING' | 'INVADING_INVESTMENT';
  cdiAnnualRate: number;
  phantomYieldEstimate: number;
  phantomYieldFullCap: number;

  // Nubank Caixinha (115% CDI) Tracker
  nubankCaixinhaBalance: number;
  nubankCaixinhaCdiRate: number;
  nubankCaixinhaEffectiveAnnualRate: number;
  nubankCaixinhaMonthlyYield: number;
  nubankCaixinhaDailyYield: number;

  // Pace Tracker
  daysElapsed: number;
  totalDaysInCycle: number;
  idealPaceAmount: number;
  paceStatus: 'AHEAD' | 'ON_TRACK' | 'OVERPACED';
  paceDifference: number;

  // Invoice Payment & 2nd Tranche Salary
  isInvoicePaid: boolean;
  invoicePaidAt?: string | null;
  invoicePaidAmount: number;
  secondTrancheAmount: number;
  secondTrancheSurplus: number;
  isSurplusInvested: boolean;

  categoryBreakdown: CategoryBreakdown[];
}

export interface CardExpense {
  id: number;
  description: string;
  category: string;
  amount: number;
  transactionDate: string;
  billingCycle: string;
  installmentCurrent: number;
  installmentTotal: number;
  purchaseGroupId: string | null;
  paymentMethod: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT' | 'PIX_INFLOW';
  deductFromCycleCap: boolean;
}

export interface ProjectionCycle {
  billingCycle: string;
  fixedCommitmentsTotal: number;
  committedCardInstallments: number;
  projectedFreeCap: number;
  projectedDailyBurnRate: number;
}

export interface SimulatorCycleImpact {
  billingCycle: string;
  projectedFreeCapBefore: number;
  projectedFreeCapAfter: number;
  installmentAmount: number;
  willInvade: boolean;
}

export interface SimulatorResponse {
  amount: number;
  installments: number;
  installmentValue: number;
  currentDailyBefore: number;
  currentDailyAfter: number;
  remainingBudgetBefore: number;
  remainingBudgetAfter: number;
  willInvadeInvestmentGoal: boolean;
  invadedAmount: number;
  affectedCycles: SimulatorCycleImpact[];
}

export interface ChatCommandResponse {
  reply: string;
  intent: string;
  dashboard: DashboardSummary;
}

export interface FinancialProfile {
  id: number;
  incomeFirstTranche: number;
  incomeSecondTranche: number;
  monthlyInvestmentGoal: number;
  closingDay: number;
  dueDay: number;
  cdiAnnualRate: number;
  nubankCaixinhaBalance: number;
  nubankCaixinhaCdiRate: number;
  creditCardLimit: number;
  customCardCap: number | null;
  totalIncome: number;
}

export interface FixedCommitment {
  id: number;
  title: string;
  amount: number;
  startCycle: string;
  totalInstallments: number | null;
  isActive: boolean;
  commitmentType?: 'PIX_COMMITMENT' | 'SUBSCRIPTION';
  paymentSource?: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
  billingDay?: number | null;
  lastPaidCycle?: string | null;
}

async function request<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  // Attach PIN if stored in localStorage
  const pinHeader: Record<string, string> = {};
  if (typeof window !== 'undefined') {
    const savedPin = localStorage.getItem('finance_app_pin');
    if (savedPin) {
      pinHeader['X-App-Pin'] = savedPin;
    }
  }

  // Detect slow first requests (Render cloud server spin-up detection)
  let timer: NodeJS.Timeout | null = null;
  if (typeof window !== 'undefined') {
    timer = setTimeout(() => {
      window.dispatchEvent(new CustomEvent('render_server_waking_up', { detail: true }));
    }, 3200);
  }

  try {
    const res = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...pinHeader,
        ...options?.headers,
      },
      cache: 'no-store',
    });

    if (timer) clearTimeout(timer);
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent('render_server_waking_up', { detail: false }));
    }

    if (res.status === 401) {
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new CustomEvent('finance_app_pin_required'));
      }
      throw new Error('Acesso negado: PIN inválido ou não configurado.');
    }

    if (!res.ok) {
      const errorText = await res.text();
      throw new Error(errorText || `API error: ${res.status}`);
    }

    return res.json();
  } catch (error) {
    if (timer) clearTimeout(timer);
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent('render_server_waking_up', { detail: false }));
    }
    throw error;
  }
}

export const api = {
  // System
  getSystemStatus: () => request<{ status: string; activeBillingCycle: string }>(`/api/system/status`),

  // Dashboard
  getDashboard: (cycle?: string) =>
    request<DashboardSummary>(`/api/dashboard${cycle ? `?cycle=${cycle}` : ''}`),

  getProjections: (months = 6) =>
    request<ProjectionCycle[]>(`/api/dashboard/projections?months=${months}`),

  // Expenses
  getExpenses: (cycle?: string) =>
    request<CardExpense[]>(`/api/expenses${cycle ? `?cycle=${cycle}` : ''}`),

  createExpense: (data: {
    description: string;
    category: string;
    totalAmount: number;
    installments: number;
    transactionDate?: string;
    paymentMethod?: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
    deductFromCycleCap?: boolean;
  }) => request<CardExpense[]>(`/api/expenses`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),

  updateCardLimit: (creditCardLimit: number, customCardCap: number | null) =>
    request<{ message: string; creditCardLimit: number; customCardCap: number | null }>(`/api/profile/card-limit`, {
      method: 'PATCH',
      body: JSON.stringify({ creditCardLimit, customCardCap }),
    }),

  toggleCommitmentPaid: (id: number, cycle?: string) =>
    request<{ message: string; isPaid: boolean; cycle: string; balanceAfter: number }>(`/api/commitments/${id}/toggle-paid${cycle ? `?cycle=${cycle}` : ''}`, {
      method: 'POST',
    }),

  payCommitmentFromCaixinha: (id: number, cycle?: string) =>
    request<{ message: string; isPaid: boolean; cycle: string; balanceAfter: number }>(`/api/commitments/${id}/toggle-paid${cycle ? `?cycle=${cycle}` : ''}`, {
      method: 'POST',
    }),

  createPixCaixinhaExpense: (data: {
    description: string;
    category: string;
    amount: number;
    deductFromCycleCap: boolean;
    transactionDate?: string;
  }) => request<CardExpense>(`/api/expenses/pix-caixinha`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),

  updateExpense: (id: number, data: {
    description: string;
    category: string;
    amount: number;
    transactionDate: string;
    updateAllGroupInstallments?: boolean;
  }) => request<{ message: string }>(`/api/expenses/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  }),

  deleteExpense: (id: number, deleteAllInstallments = false) =>
    request<{ message: string }>(`/api/expenses/${id}?deleteAllInstallments=${deleteAllInstallments}`, {
      method: 'DELETE',
    }),

  undoExpense: () =>
    request<{ message: string }>(`/api/expenses/undo`, {
      method: 'POST',
    }),

  syncInvoice: (targetTotal: number, cycle?: string) =>
    request<{ message: string; currentInvoiceTotal: number }>(`/api/expenses/sync-invoice`, {
      method: 'POST',
      body: JSON.stringify({ targetTotal, cycle }),
    }),

  updateCaixinhaBalance: (balance: number) =>
    request<{ message: string; balance: number }>(`/api/profile/caixinha`, {
      method: 'POST',
      body: JSON.stringify({ balance }),
    }),

  depositCaixinha: (amount: number, description?: string) =>
    request<{ message: string; balance: number; amount: number }>(`/api/profile/caixinha/deposit`, {
      method: 'POST',
      body: JSON.stringify({ amount, description }),
    }),

  receivePix: (data: {
    description: string;
    amount: number;
    category?: string;
    transactionDate?: string;
  }) =>
    request<CardExpense>(`/api/expenses/receive-pix`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  // Invoice Payment (2nd Tranche Salary Sync)
  toggleInvoicePaid: (cycle: string) =>
    request<{
      message: string;
      cycle: string;
      isPaid: boolean;
      paidAmount: number;
      surplusAmount: number;
      surplusInvested: boolean;
      paidAt?: string;
    }>(`/api/invoices/${cycle}/toggle-paid`, {
      method: 'POST',
    }),

  investInvoiceSurplus: (cycle: string) =>
    request<{
      message: string;
      cycle: string;
      investedAmount: number;
      newCaixinhaBalance: number;
    }>(`/api/invoices/${cycle}/invest-surplus`, {
      method: 'POST',
    }),

  // Simulator
  simulate: (data: { amount: number; installments: number }) =>
    request<SimulatorResponse>(`/api/simulator`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  // Chat Quick-Command
  sendCommand: (message: string) =>
    request<ChatCommandResponse>(`/api/chat/command`, {
      method: 'POST',
      body: JSON.stringify({ message }),
    }),

  // Profile & Commitments
  getProfile: () =>
    request<{ profile: FinancialProfile; commitments: FixedCommitment[] }>(`/api/profile`),

  updateProfile: (profile: Partial<FinancialProfile>) =>
    request<{ message: string; profile: FinancialProfile }>(`/api/profile`, {
      method: 'PUT',
      body: JSON.stringify(profile),
    }),

  addCommitment: (commitment: Omit<FixedCommitment, 'id'>) =>
    request<FixedCommitment>(`/api/profile/commitments`, {
      method: 'POST',
      body: JSON.stringify(commitment),
    }),

  updateCommitment: (id: number, commitment: Partial<FixedCommitment>) =>
    request<{ message: string }>(`/api/profile/commitments/${id}`, {
      method: 'PUT',
      body: JSON.stringify(commitment),
    }),

  deleteCommitment: (id: number) =>
    request<{ message: string }>(`/api/profile/commitments/${id}`, {
      method: 'DELETE',
    }),
};
