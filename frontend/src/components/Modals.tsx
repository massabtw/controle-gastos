'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { X, Trash2, Settings2, CreditCard, PiggyBank, Send, RefreshCw, Sliders, Plus, ArrowDownLeft } from 'lucide-react';
import { api, CardExpense, FinancialProfile, FixedCommitment } from '@/lib/api';

const CATEGORIES = ['Alimentação', 'Transporte', 'Lazer', 'Compras', 'Assinaturas', 'Saúde', 'Outros'];

// 1. NEW EXPENSE MODAL (WITH PAYMENT SOURCE SELECTOR)
interface NewExpenseModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  currentCaixinhaBalance?: number;
}

export function NewExpenseModal({
  isOpen,
  onClose,
  onSuccess,
  currentCaixinhaBalance = 0,
}: NewExpenseModalProps) {
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('Alimentação');
  const [amount, setAmount] = useState('');
  const [installments, setInstallments] = useState(1);
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [paymentMethod, setPaymentMethod] = useState<'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT'>('CARD');
  const [deductFromCycleCap, setDeductFromCycleCap] = useState(true);
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const numAmount = parseFloat(amount.replace(',', '.')) || 0;
  const balanceAfterPix = Math.max(0, currentCaixinhaBalance - numAmount);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim() || isNaN(numAmount) || numAmount <= 0) return;

    setLoading(true);
    try {
      await api.createExpense({
        description: description.trim(),
        category,
        totalAmount: numAmount,
        installments: paymentMethod === 'CARD' ? Number(installments) : 1,
        transactionDate: date ? new Date(date).toISOString() : undefined,
        paymentMethod,
        deductFromCycleCap: paymentMethod === 'CARD' ? true : deductFromCycleCap,
      });
      onSuccess();
      onClose();
      setDescription('');
      setAmount('');
      setInstallments(1);
      setPaymentMethod('CARD');
      setDeductFromCycleCap(true);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao salvar';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2">
            <h3 className="text-base font-bold text-zinc-100">Novo Lançamento</h3>
            <span className={`text-[10px] font-mono px-2 py-0.5 rounded-full font-semibold ${
              paymentMethod === 'CARD' ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30' :
              paymentMethod === 'PIX_CAIXINHA' ? 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30' :
              'bg-blue-500/20 text-blue-400 border border-blue-500/30'
            }`}>
              {paymentMethod === 'CARD' ? 'Fatura Cartão' : paymentMethod === 'PIX_CAIXINHA' ? 'Pix Caixinha' : 'Pix Saldo Conta'}
            </span>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          {/* Segmented Payment Source Selector */}
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-2">
              De onde saiu o dinheiro?
            </label>
            <div className="grid grid-cols-3 gap-1.5 p-1 bg-zinc-950/80 rounded-2xl border border-zinc-800">
              <button
                type="button"
                onClick={() => setPaymentMethod('CARD')}
                className={`py-2 px-1 text-center rounded-xl text-xs font-semibold transition-all flex flex-col items-center gap-0.5 ${
                  paymentMethod === 'CARD'
                    ? 'bg-emerald-500 text-zinc-950 shadow-md font-bold'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <span>💳 Cartão</span>
                <span className="text-[10px] opacity-80 font-normal">Fatura</span>
              </button>
              <button
                type="button"
                onClick={() => setPaymentMethod('PIX_CAIXINHA')}
                className={`py-2 px-1 text-center rounded-xl text-xs font-semibold transition-all flex flex-col items-center gap-0.5 ${
                  paymentMethod === 'PIX_CAIXINHA'
                    ? 'bg-cyan-500 text-zinc-950 shadow-md font-bold'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <span>🐷 Caixinha</span>
                <span className="text-[10px] opacity-80 font-normal">Pix 115%</span>
              </button>
              <button
                type="button"
                onClick={() => setPaymentMethod('PIX_ACCOUNT')}
                className={`py-2 px-1 text-center rounded-xl text-xs font-semibold transition-all flex flex-col items-center gap-0.5 ${
                  paymentMethod === 'PIX_ACCOUNT'
                    ? 'bg-blue-500 text-zinc-950 shadow-md font-bold'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <span>🏦 Saldo Conta</span>
                <span className="text-[10px] opacity-80 font-normal">Pix / Débito</span>
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Descrição
            </label>
            <input
              type="text"
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Ex: Supermercado, Uber, Barbearia..."
              className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-emerald-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Valor Total (R$)
              </label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="0.00"
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm font-mono text-zinc-100 focus:outline-none focus:border-emerald-500"
              />
            </div>

            {paymentMethod === 'CARD' ? (
              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Parcelas
                </label>
                <select
                  value={installments}
                  onChange={(e) => setInstallments(Number(e.target.value))}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm font-mono text-zinc-100 focus:outline-none focus:border-emerald-500"
                >
                  {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map((n) => (
                    <option key={n} value={n}>
                      {n}x {amount && n > 1 ? `(R$ ${(parseFloat(amount) / n || 0).toFixed(2)})` : ''}
                    </option>
                  ))}
                </select>
              </div>
            ) : (
              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Pagamento
                </label>
                <div className="w-full bg-zinc-950/70 border border-zinc-800 rounded-xl px-3.5 py-2.5 text-xs text-zinc-400 flex items-center gap-1.5 font-medium">
                  <span>À vista (1x)</span>
                </div>
              </div>
            )}
          </div>

          {/* Contextual notes & balance preview based on Payment Source */}
          {paymentMethod === 'PIX_CAIXINHA' && (
            <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-cyan-500/30 text-xs font-mono space-y-1.5">
              <div className="flex items-center justify-between text-zinc-400">
                <span>Saldo atual na Caixinha:</span>
                <span className="text-purple-300 font-bold">R$ {currentCaixinhaBalance.toFixed(2)}</span>
              </div>
              <div className="flex items-center justify-between text-zinc-200">
                <span>Saldo após este Pix:</span>
                <span className={`font-bold ${balanceAfterPix < 0 ? 'text-rose-400' : 'text-cyan-300'}`}>
                  R$ {balanceAfterPix.toFixed(2)}
                </span>
              </div>
              <label className="flex items-center gap-2 pt-2 border-t border-zinc-800/80 cursor-pointer text-[11px] text-zinc-300 font-sans">
                <input
                  type="checkbox"
                  checked={deductFromCycleCap}
                  onChange={(e) => setDeductFromCycleCap(e.target.checked)}
                  className="rounded text-cyan-500 focus:ring-cyan-400 bg-zinc-900 border-zinc-700"
                />
                <span>Abater do Teto Livre deste ciclo (proteger meta de R$ 1.000)</span>
              </label>
            </div>
          )}

          {paymentMethod === 'PIX_ACCOUNT' && (
            <div className="p-3 rounded-xl bg-blue-950/30 border border-blue-500/30 text-xs text-blue-300 leading-relaxed font-sans">
              ℹ️ Este valor saiu do saldo em conta corrente. Ele consome seu teto livre mensal para proteger a meta de R$ 1.000, sem subtrair do saldo guardado na Caixinha.
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Categoria
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-emerald-500"
              >
                {CATEGORIES.map((cat) => (
                  <option key={cat} value={cat}>
                    {cat}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Data do Gasto
              </label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm font-mono text-zinc-100 focus:outline-none focus:border-emerald-500"
              />
            </div>
          </div>

          <div className="pt-4 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-sm font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-sm font-bold shadow-md transition-all cursor-pointer"
            >
              {loading ? 'Salvando...' : 'Salvar Lançamento'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 2. EDIT EXPENSE MODAL
interface EditExpenseModalProps {
  expense: CardExpense | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function EditExpenseModal({ expense, onClose, onSuccess }: EditExpenseModalProps) {
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('');
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState('');
  const [updateAll, setUpdateAll] = useState(true);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (expense) {
      setDescription(expense.description);
      setCategory(expense.category);
      setAmount(expense.amount.toString());
      setDate(expense.transactionDate ? expense.transactionDate.split('T')[0] : '');
      setUpdateAll(true);
    }
  }, [expense]);

  if (!expense) return null;

  const isGroup = Boolean(expense.purchaseGroupId && expense.installmentTotal > 1);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const numAmount = parseFloat(amount.replace(',', '.'));
    if (!description.trim() || isNaN(numAmount) || numAmount <= 0) return;

    setLoading(true);
    try {
      await api.updateExpense(expense.id, {
        description: description.trim(),
        category,
        amount: numAmount,
        transactionDate: new Date(date).toISOString(),
        updateAllGroupInstallments: updateAll,
      });
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao atualizar';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div>
            <h3 className="text-base font-bold text-zinc-100">Editar Gasto</h3>
            {isGroup && (
              <span className="text-xs font-mono text-amber-400">
                Parcela {expense.installmentCurrent}/{expense.installmentTotal}
              </span>
            )}
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Descrição
            </label>
            <input
              type="text"
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-emerald-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Valor da Parcela (R$)
              </label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm font-mono text-zinc-100 focus:outline-none focus:border-emerald-500"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Categoria
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-emerald-500"
              >
                {CATEGORIES.map((cat) => (
                  <option key={cat} value={cat}>
                    {cat}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Data do Gasto
            </label>
            <input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm font-mono text-zinc-100 focus:outline-none focus:border-emerald-500"
            />
          </div>

          {isGroup && (
            <label className="flex items-center gap-2 p-3 rounded-xl bg-zinc-950/60 border border-zinc-800 text-xs text-zinc-300 cursor-pointer">
              <input
                type="checkbox"
                checked={updateAll}
                onChange={(e) => setUpdateAll(e.target.checked)}
                className="rounded border-zinc-700 text-emerald-500 focus:ring-0"
              />
              <span>Atualizar título e categoria em todas as {expense.installmentTotal} parcelas</span>
            </label>
          )}

          <div className="pt-4 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-sm font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-sm font-bold shadow-md transition-all cursor-pointer"
            >
              {loading ? 'Salvando...' : 'Salvar Alterações'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 3. DELETE CONFIRM MODAL
interface DeleteConfirmModalProps {
  expense: CardExpense | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function DeleteConfirmModal({ expense, onClose, onSuccess }: DeleteConfirmModalProps) {
  const [loading, setLoading] = useState(false);

  if (!expense) return null;

  const isGroup = Boolean(expense.purchaseGroupId && expense.installmentTotal > 1);

  const handleDelete = async (deleteAll: boolean) => {
    setLoading(true);
    try {
      await api.deleteExpense(expense.id, deleteAll);
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao excluir';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-sm rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="w-10 h-10 rounded-2xl bg-rose-500/10 text-rose-400 flex items-center justify-center mb-4">
          <Trash2 className="w-5 h-5" />
        </div>

        <h3 className="text-base font-bold text-zinc-100">Excluir Lançamento</h3>
        <p className="text-xs text-zinc-400 mt-1 mb-4">
          Você tem certeza que deseja remover <span className="text-zinc-200 font-semibold">{expense.description}</span> (R$ {expense.amount.toFixed(2)})?
        </p>

        {isGroup ? (
          <div className="space-y-2">
            <button
              onClick={() => handleDelete(false)}
              disabled={loading}
              className="w-full py-2.5 rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 text-xs font-semibold transition-colors"
            >
              Excluir apenas esta parcela ({expense.installmentCurrent}/{expense.installmentTotal})
            </button>
            <button
              onClick={() => handleDelete(true)}
              disabled={loading}
              className="w-full py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition-colors shadow-md"
            >
              Excluir todas as {expense.installmentTotal} parcelas da compra
            </button>
            <button
              onClick={onClose}
              className="w-full py-2 text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
          </div>
        ) : (
          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              onClick={() => handleDelete(false)}
              disabled={loading}
              className="px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition-colors shadow-md"
            >
              {loading ? 'Excluindo...' : 'Sim, Excluir'}
            </button>
          </div>
        )}
      </motion.div>
    </div>
  );
}

// 4. SETTINGS MODAL (Financial Profile & Fixed Commitments)
interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function SettingsModal({ isOpen, onClose, onSuccess }: SettingsModalProps) {
  const [activeTab, setActiveTab] = useState<'profile' | 'commitments'>('profile');
  const [profile, setProfile] = useState<FinancialProfile | null>(null);
  const [commitments, setCommitments] = useState<FixedCommitment[]>([]);
  const [loading, setLoading] = useState(false);

  // New commitment form state
  const [newTitle, setNewTitle] = useState('');
  const [newAmount, setNewAmount] = useState('');
  const [newStartCycle, setNewStartCycle] = useState('2026-12');
  const [isFixedIndefinite, setIsFixedIndefinite] = useState(true);
  const [newTotalInstallments, setNewTotalInstallments] = useState(5);

  const fetchProfileData = async () => {
    try {
      const data = await api.getProfile();
      setProfile(data.profile);
      setCommitments(data.commitments);
    } catch (err) {
      console.error('Error fetching profile:', err);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchProfileData();
    }
  }, [isOpen]);

  if (!isOpen || !profile) return null;

  const pixCommitments = commitments.filter((c) => c.commitmentType !== 'SUBSCRIPTION');

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await api.updateProfile(profile);
      onSuccess();
      alert('Perfil financeiro atualizado com sucesso!');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao salvar';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleAddCommitment = async (e: React.FormEvent) => {
    e.preventDefault();
    const numAmount = parseFloat(newAmount.replace(',', '.'));
    if (!newTitle.trim() || isNaN(numAmount) || numAmount <= 0) return;

    setLoading(true);
    try {
      await api.addCommitment({
        title: newTitle.trim(),
        amount: numAmount,
        startCycle: newStartCycle,
        totalInstallments: isFixedIndefinite ? null : Number(newTotalInstallments),
        isActive: true,
        commitmentType: 'PIX_COMMITMENT',
        paymentSource: 'PIX_CAIXINHA',
        billingDay: 1,
      });
      setNewTitle('');
      setNewAmount('');
      await fetchProfileData();
      onSuccess();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao adicionar';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteCommitment = async (id: number) => {
    if (!confirm('Deseja realmente remover esta obrigação fixa?')) return;
    try {
      await api.deleteCommitment(id);
      await fetchProfileData();
      onSuccess();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao excluir';
      alert(`Erro: ${message}`);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-xl rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100 max-h-[90vh] overflow-y-auto"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <Settings2 className="w-5 h-5 text-emerald-400" />
            <h3 className="text-base font-bold text-zinc-100">Configurações & Parâmetros</h3>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Tab switch */}
        <div className="flex border-b border-zinc-800 mt-4 mb-5">
          <button
            onClick={() => setActiveTab('profile')}
            className={`pb-2.5 text-xs font-bold uppercase tracking-wider px-4 border-b-2 transition-all ${
              activeTab === 'profile'
                ? 'border-emerald-500 text-emerald-400'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            Perfil & Regras Financeiras
          </button>
          <button
            onClick={() => setActiveTab('commitments')}
            className={`pb-2.5 text-xs font-bold uppercase tracking-wider px-4 border-b-2 transition-all ${
              activeTab === 'commitments'
                ? 'border-emerald-500 text-emerald-400'
                : 'border-transparent text-zinc-400 hover:text-zinc-200'
            }`}
          >
            Compromissos Fixos Pix ({pixCommitments.length})
          </button>
        </div>

        {activeTab === 'profile' ? (
          <form onSubmit={handleSaveProfile} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  1ª Parcela Salário (Dia 5 útil)
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={profile.incomeFirstTranche}
                  onChange={(e) => setProfile({ ...profile, incomeFirstTranche: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  2ª Parcela Salário (Dia 14)
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={profile.incomeSecondTranche}
                  onChange={(e) => setProfile({ ...profile, incomeSecondTranche: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Meta Investimento CDI 🔒
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={profile.monthlyInvestmentGoal}
                  onChange={(e) => setProfile({ ...profile, monthlyInvestmentGoal: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Taxa CDI Anual (%)
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={profile.cdiAnnualRate}
                  onChange={(e) => setProfile({ ...profile, cdiAnnualRate: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Dia de Fechamento do Cartão
                </label>
                <input
                  type="number"
                  min="1"
                  max="31"
                  value={profile.closingDay}
                  onChange={(e) => setProfile({ ...profile, closingDay: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Dia de Vencimento do Cartão
                </label>
                <input
                  type="number"
                  min="1"
                  max="31"
                  value={profile.dueDay}
                  onChange={(e) => setProfile({ ...profile, dueDay: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>
            </div>

            {/* Nubank Caixinha settings */}
            <div className="grid grid-cols-2 gap-3 p-3 rounded-2xl bg-purple-950/20 border border-purple-500/20">
              <div>
                <label className="block text-xs font-semibold text-purple-300 uppercase tracking-wider mb-1">
                  Saldo Caixinha Nubank (R$)
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={profile.nubankCaixinhaBalance ?? 0}
                  onChange={(e) => setProfile({ ...profile, nubankCaixinhaBalance: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-purple-500/40 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-purple-300 uppercase tracking-wider mb-1">
                  Rendimento (% do CDI)
                </label>
                <input
                  type="number"
                  step="1"
                  value={profile.nubankCaixinhaCdiRate ?? 115}
                  onChange={(e) => setProfile({ ...profile, nubankCaixinhaCdiRate: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-purple-500/40 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>
            </div>

            {/* Credit Card Bank Limit & Custom Cap */}
            <div className="grid grid-cols-2 gap-3 p-3 rounded-2xl bg-blue-950/20 border border-blue-500/20">
              <div>
                <label className="block text-xs font-semibold text-blue-300 uppercase tracking-wider mb-1">
                  Limite Total do Banco (R$)
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={profile.creditCardLimit ?? 2500}
                  onChange={(e) => setProfile({ ...profile, creditCardLimit: Number(e.target.value) })}
                  className="w-full bg-zinc-950/70 border border-blue-500/40 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-blue-300 uppercase tracking-wider mb-1">
                  Teto Manual (Opcional R$)
                </label>
                <input
                  type="number"
                  step="0.01"
                  placeholder="Automático (Fórmula)"
                  value={profile.customCardCap ?? ''}
                  onChange={(e) => setProfile({ ...profile, customCardCap: e.target.value ? Number(e.target.value) : null })}
                  className="w-full bg-zinc-950/70 border border-blue-500/40 rounded-xl px-3.5 py-2 text-sm font-mono text-zinc-100"
                />
              </div>
            </div>

            <div className="pt-4 flex justify-end">
              <button
                type="submit"
                disabled={loading}
                className="px-5 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-xs font-bold shadow-md transition-all cursor-pointer"
              >
                {loading ? 'Salvando...' : 'Salvar Regras'}
              </button>
            </div>
          </form>
        ) : (
          <div className="space-y-6">
            {/* List */}
            <div className="space-y-2">
              <span className="text-xs font-semibold text-zinc-400 uppercase tracking-wider">
                Compromissos Cadastrados:
              </span>
              {pixCommitments.map((c) => (
                <div
                  key={c.id}
                  className="flex items-center justify-between p-3 rounded-xl bg-zinc-950/70 border border-zinc-800 text-xs"
                >
                  <div>
                    <span className="font-semibold text-zinc-200">{c.title}</span>
                    <span className="block text-[11px] text-zinc-400 font-mono">
                      Início: {c.startCycle} • {c.totalInstallments ? `${c.totalInstallments} meses` : 'Recorrente Fixo'}
                    </span>
                  </div>

                  <div className="flex items-center gap-3">
                    <span className="font-mono font-bold text-sm text-zinc-100">
                      R$ {c.amount.toFixed(2)}
                    </span>
                    <button
                      onClick={() => handleDeleteCommitment(c.id)}
                      className="p-1 rounded-lg text-zinc-500 hover:text-rose-400 hover:bg-rose-500/10 transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Add new commitment */}
            <form onSubmit={handleAddCommitment} className="p-4 rounded-2xl bg-zinc-950/40 border border-zinc-800/80 space-y-3">
              <span className="text-xs font-bold text-zinc-300 block">
                Adicionar Novo Pix/Compromisso Externo
              </span>

              <div className="grid grid-cols-2 gap-3">
                <input
                  type="text"
                  required
                  placeholder="Título (ex: Seguro Mãe)"
                  value={newTitle}
                  onChange={(e) => setNewTitle(e.target.value)}
                  className="w-full bg-zinc-900 border border-zinc-700 rounded-xl px-3 py-1.5 text-xs text-zinc-100"
                />
                <input
                  type="number"
                  step="0.01"
                  required
                  placeholder="Valor (R$)"
                  value={newAmount}
                  onChange={(e) => setNewAmount(e.target.value)}
                  className="w-full bg-zinc-900 border border-zinc-700 rounded-xl px-3 py-1.5 text-xs font-mono text-zinc-100"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <input
                  type="text"
                  required
                  placeholder="Ciclo Início (YYYY-MM)"
                  value={newStartCycle}
                  onChange={(e) => setNewStartCycle(e.target.value)}
                  className="w-full bg-zinc-900 border border-zinc-700 rounded-xl px-3 py-1.5 text-xs font-mono text-zinc-100"
                />

                <select
                  value={isFixedIndefinite ? 'indefinite' : 'fixed'}
                  onChange={(e) => setIsFixedIndefinite(e.target.value === 'indefinite')}
                  className="w-full bg-zinc-900 border border-zinc-700 rounded-xl px-3 py-1.5 text-xs text-zinc-100"
                >
                  <option value="indefinite">Fixo Recorrente</option>
                  <option value="fixed">Duração em Meses</option>
                </select>
              </div>

              {!isFixedIndefinite && (
                <div>
                  <label className="text-[11px] text-zinc-400 block mb-1">Quantidade de Meses:</label>
                  <input
                    type="number"
                    min="1"
                    max="60"
                    value={newTotalInstallments}
                    onChange={(e) => setNewTotalInstallments(Number(e.target.value))}
                    className="w-full bg-zinc-900 border border-zinc-700 rounded-xl px-3 py-1.5 text-xs font-mono text-zinc-100"
                  />
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                className="w-full py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 text-xs font-bold transition-colors cursor-pointer"
              >
                {loading ? 'Adicionando...' : '+ Adicionar Compromisso'}
              </button>
            </form>
          </div>
        )}
      </motion.div>
    </div>
  );
}

// 5. SYNC INVOICE MODAL (Ajustar Fatura Atual)
interface SyncInvoiceModalProps {
  isOpen: boolean;
  currentTotal: number;
  cycle: string;
  onClose: () => void;
  onSuccess: () => void;
}

export function SyncInvoiceModal({
  isOpen,
  currentTotal,
  cycle,
  onClose,
  onSuccess,
}: SyncInvoiceModalProps) {
  const [targetTotalStr, setTargetTotalStr] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setTargetTotalStr(currentTotal > 0 ? currentTotal.toFixed(2) : '');
    }
  }, [isOpen, currentTotal]);

  if (!isOpen) return null;

  const targetTotal = parseFloat(targetTotalStr.replace(',', '.')) || 0;
  const diff = targetTotal - currentTotal;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isNaN(targetTotal) || targetTotal < 0) return;

    setLoading(true);
    try {
      await api.syncInvoice(targetTotal, cycle);
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao sincronizar fatura';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-purple-500/20 text-purple-400">
              <CreditCard className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Ajustar Fatura Atual</h3>
              <span className="text-xs text-zinc-400 font-mono">Ciclo {cycle}</span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <p className="text-xs text-zinc-400 leading-relaxed">
            Informe o valor exato exibido no app do Nubank agora. O sistema criará automaticamente uma linha de ajuste para calibrar o total sem que você precise lançar comprovantes antigos.
          </p>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
              Valor Real da Fatura no Nubank (R$)
            </label>
            <div className="relative">
              <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                R$
              </span>
              <input
                type="number"
                step="0.01"
                min="0"
                required
                value={targetTotalStr}
                onChange={(e) => setTargetTotalStr(e.target.value)}
                placeholder="Ex: 450.90"
                className="w-full bg-zinc-950/70 border border-purple-500/50 rounded-xl py-2.5 pl-10 pr-4 font-mono text-base text-zinc-100 focus:outline-none focus:ring-1 focus:ring-purple-500"
              />
            </div>
          </div>

          {/* Difference Preview */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-zinc-800 text-xs font-mono space-y-1.5">
            <div className="flex items-center justify-between text-zinc-400">
              <span>Fatura computada atual:</span>
              <span>R$ {currentTotal.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between text-zinc-100 font-bold">
              <span>Nova fatura no tracker:</span>
              <span className="text-purple-300">R$ {targetTotal.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between pt-1 border-t border-zinc-800 text-[11px]">
              <span className="text-zinc-500">Ajuste gerado:</span>
              <span className={diff >= 0 ? 'text-amber-400' : 'text-emerald-400'}>
                {diff >= 0 ? `+R$ ${diff.toFixed(2)}` : `-R$ ${Math.abs(diff).toFixed(2)}`}
              </span>
            </div>
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold transition-all shadow-md cursor-pointer"
            >
              {loading ? 'Sincronizando...' : 'Calibrar Fatura'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 6. UPDATE CAIXINHA MODAL (Nubank 115% CDI)
interface UpdateCaixinhaModalProps {
  isOpen: boolean;
  currentBalance: number;
  cdiAnnualRate: number;
  onClose: () => void;
  onSuccess: () => void;
}

export function UpdateCaixinhaModal({
  isOpen,
  currentBalance,
  cdiAnnualRate,
  onClose,
  onSuccess,
}: UpdateCaixinhaModalProps) {
  const [balanceStr, setBalanceStr] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setBalanceStr(currentBalance > 0 ? currentBalance.toFixed(2) : '');
    }
  }, [isOpen, currentBalance]);

  if (!isOpen) return null;

  const balance = parseFloat(balanceStr.replace(',', '.')) || 0;
  const effectiveAnnual = cdiAnnualRate * 1.15;
  const monthlyYield = balance * ((effectiveAnnual / 100) / 12);
  const dailyYield = monthlyYield / 30;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isNaN(balance) || balance < 0) return;

    setLoading(true);
    try {
      await api.updateCaixinhaBalance(balance);
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao atualizar saldo';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-purple-500/20 text-purple-400">
              <PiggyBank className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Saldo da Caixinha Nubank</h3>
              <span className="text-[10px] font-mono text-purple-400">Rendimento a 115% do CDI</span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <p className="text-xs text-zinc-400 leading-relaxed">
            Atualize o valor total que você tem guardado na Caixinha do Nubank para acompanhar seu rendimento líquido mensal e diário.
          </p>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
              Saldo Total Guardado (R$)
            </label>
            <div className="relative">
              <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                R$
              </span>
              <input
                type="number"
                step="0.01"
                min="0"
                required
                value={balanceStr}
                onChange={(e) => setBalanceStr(e.target.value)}
                placeholder="Ex: 5000.00"
                className="w-full bg-zinc-950/70 border border-purple-500/50 rounded-xl py-2.5 pl-10 pr-4 font-mono text-base text-zinc-100 focus:outline-none focus:ring-1 focus:ring-purple-500"
              />
            </div>
          </div>

          {/* Live Yield Preview */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-zinc-800 text-xs font-mono space-y-1.5">
            <div className="flex items-center justify-between text-zinc-400">
              <span>Taxa efetiva anual:</span>
              <span className="text-purple-300 font-bold">{effectiveAnnual.toFixed(2)}% a.a.</span>
            </div>
            <div className="flex items-center justify-between text-zinc-200">
              <span>Rendimento estimado:</span>
              <span className="text-emerald-400 font-bold">+R$ {monthlyYield.toFixed(2)} / mês</span>
            </div>
            <div className="flex items-center justify-between text-zinc-400 text-[11px]">
              <span>Rendimento por dia:</span>
              <span className="text-emerald-400">+R$ {dailyYield.toFixed(2)} / dia</span>
            </div>
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold transition-all shadow-md cursor-pointer"
            >
              {loading ? 'Salvando...' : 'Salvar Saldo'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 7. PIX CAIXINHA MODAL
interface PixCaixinhaModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  currentCaixinhaBalance: number;
}

export function PixCaixinhaModal({
  isOpen,
  onClose,
  onSuccess,
  currentCaixinhaBalance,
}: PixCaixinhaModalProps) {
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('Outros');
  const [amountStr, setAmountStr] = useState('');
  const [deductFromCycleCap, setDeductFromCycleCap] = useState(true);
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const amount = parseFloat(amountStr.replace(',', '.')) || 0;
  const balanceAfterPix = Math.max(0, currentCaixinhaBalance - amount);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim() || isNaN(amount) || amount <= 0) return;

    setLoading(true);
    try {
      await api.createPixCaixinhaExpense({
        description: description.trim(),
        category,
        amount,
        deductFromCycleCap,
        transactionDate: date ? new Date(date).toISOString() : undefined,
      });
      onSuccess();
      onClose();
      setDescription('');
      setAmountStr('');
      setDeductFromCycleCap(true);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao registrar Pix';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-cyan-500/20 text-cyan-400">
              <Send className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Retirar da Caixinha para Pix</h3>
              <span className="text-[10px] font-mono text-cyan-400">Pagar no Pix fora do cartão</span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Descrição
            </label>
            <input
              type="text"
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Ex: Corte de cabelo, Lanche, Pix dentista..."
              className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-cyan-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Valor do Pix (R$)
              </label>
              <div className="relative">
                <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                  R$
                </span>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  required
                  value={amountStr}
                  onChange={(e) => setAmountStr(e.target.value)}
                  placeholder="0.00"
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl py-2.5 pl-10 pr-3 text-sm font-mono text-zinc-100 focus:outline-none focus:border-cyan-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Categoria
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-cyan-500"
              >
                {CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Data do Pagamento
            </label>
            <input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-cyan-500"
            />
          </div>

          {/* Toggle for deducting from cycle cap */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-zinc-800 space-y-2">
            <label className="flex items-start gap-2.5 cursor-pointer">
              <input
                type="checkbox"
                checked={deductFromCycleCap}
                onChange={(e) => setDeductFromCycleCap(e.target.checked)}
                className="mt-1 w-4 h-4 rounded text-cyan-500 focus:ring-cyan-400 bg-zinc-900 border-zinc-700 cursor-pointer"
              />
              <div>
                <span className="text-xs font-semibold text-zinc-200">
                  Abater do Teto Livre deste ciclo
                </span>
                <p className="text-[11px] text-zinc-400 leading-tight mt-0.5">
                  {deductFromCycleCap
                    ? '🛡️ Protege sua meta de guardar R$ 1.000 ao abater o gasto do limite mensal.'
                    : '⚠️ O gasto sairá da Caixinha mas não consumirá o teto do cartão neste mês.'}
                </p>
              </div>
            </label>
          </div>

          {/* Balance Preview */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-cyan-500/30 text-xs font-mono space-y-1.5">
            <div className="flex items-center justify-between text-zinc-400">
              <span>Saldo atual na Caixinha:</span>
              <span className="text-purple-300 font-bold">R$ {currentCaixinhaBalance.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between text-zinc-200">
              <span>Saldo após este Pix:</span>
              <span className={`font-bold ${balanceAfterPix < 0 ? 'text-rose-400' : 'text-cyan-300'}`}>
                R$ {balanceAfterPix.toFixed(2)}
              </span>
            </div>
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading || amount <= 0}
              className="px-5 py-2.5 rounded-xl bg-cyan-600 hover:bg-cyan-500 disabled:opacity-50 text-white text-xs font-bold transition-all shadow-md cursor-pointer flex items-center gap-1.5"
            >
              <Send className="w-3.5 h-3.5" />
              {loading ? 'Processando...' : 'Confirmar Retirada Pix'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 8. EDIT CARD LIMIT & CUSTOM CAP MODAL
interface EditCardLimitModalProps {
  isOpen: boolean;
  currentCreditCardLimit: number;
  currentCustomCardCap: number | null;
  formulaCap: number;
  onClose: () => void;
  onSuccess: () => void;
}

export function EditCardLimitModal({
  isOpen,
  currentCreditCardLimit,
  currentCustomCardCap,
  formulaCap,
  onClose,
  onSuccess,
}: EditCardLimitModalProps) {
  const [bankLimitStr, setBankLimitStr] = useState('');
  const [isCustomCap, setIsCustomCap] = useState(false);
  const [customCapStr, setCustomCapStr] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setBankLimitStr(currentCreditCardLimit > 0 ? currentCreditCardLimit.toString() : '2500');
      const hasCustom = currentCustomCardCap !== null && currentCustomCardCap > 0;
      setIsCustomCap(hasCustom);
      setCustomCapStr(hasCustom ? currentCustomCardCap!.toString() : formulaCap.toFixed(2));
    }
  }, [isOpen, currentCreditCardLimit, currentCustomCardCap, formulaCap]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const bankLimit = parseFloat(bankLimitStr.replace(',', '.'));
    if (isNaN(bankLimit) || bankLimit <= 0) {
      alert('Informe um limite de cartão válido.');
      return;
    }

    let finalCustomCap: number | null = null;
    if (isCustomCap) {
      const parsedCap = parseFloat(customCapStr.replace(',', '.'));
      if (isNaN(parsedCap) || parsedCap <= 0) {
        alert('Informe um valor de teto válido.');
        return;
      }
      finalCustomCap = parsedCap;
    }

    setLoading(true);
    try {
      await api.updateCardLimit(bankLimit, finalCustomCap);
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao salvar limites';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-blue-500/20 text-blue-400">
              <Sliders className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Limites & Teto do Cartão</h3>
              <span className="text-[10px] font-mono text-blue-400">Configuração de Teto e Limite Bancário</span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          {/* 1. Bank Limit */}
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Limite Total Concedido pelo Nubank (R$)
            </label>
            <div className="relative">
              <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                R$
              </span>
              <input
                type="number"
                step="0.01"
                min="1"
                required
                value={bankLimitStr}
                onChange={(e) => setBankLimitStr(e.target.value)}
                placeholder="2500.00"
                className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl py-2.5 pl-10 pr-4 font-mono text-sm text-zinc-100 focus:outline-none focus:border-blue-500"
              />
            </div>
            <span className="text-[11px] text-zinc-500 mt-1 block">
              Usado para calcular seu limite de crédito livre real considerando faturas atuais e parcelas futuras.
            </span>
          </div>

          {/* 2. Cap mode toggle */}
          <div className="pt-2 border-t border-zinc-800/80 space-y-3">
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider">
              Modo do Teto Mensal do Cartão
            </label>

            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => setIsCustomCap(false)}
                className={`p-3 rounded-2xl border text-left transition-all ${
                  !isCustomCap
                    ? 'bg-emerald-950/30 border-emerald-500/50 text-emerald-300 shadow-sm'
                    : 'bg-zinc-950/60 border-zinc-800 text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <div className="flex items-center gap-1.5 font-bold text-xs mb-1">
                  <span>⚡ Automático</span>
                </div>
                <div className="text-[11px] font-mono text-zinc-300 font-semibold">
                  R$ {formulaCap.toFixed(2)}
                </div>
                <div className="text-[10px] text-zinc-500 mt-0.5 leading-tight">
                  Regra de Ouro (Renda − Meta − Pix)
                </div>
              </button>

              <button
                type="button"
                onClick={() => setIsCustomCap(true)}
                className={`p-3 rounded-2xl border text-left transition-all ${
                  isCustomCap
                    ? 'bg-blue-950/30 border-blue-500/50 text-blue-300 shadow-sm'
                    : 'bg-zinc-950/60 border-zinc-800 text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <div className="flex items-center gap-1.5 font-bold text-xs mb-1">
                  <span>✏️ Personalizado</span>
                </div>
                <div className="text-[11px] font-mono text-zinc-300 font-semibold">
                  Manual
                </div>
                <div className="text-[10px] text-zinc-500 mt-0.5 leading-tight">
                  Defina um valor fixo de teto
                </div>
              </button>
            </div>

            {isCustomCap && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                className="pt-2"
              >
                <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                  Valor do Teto Manual (R$)
                </label>
                <div className="relative">
                  <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                    R$
                  </span>
                  <input
                    type="number"
                    step="0.01"
                    min="1"
                    required
                    value={customCapStr}
                    onChange={(e) => setCustomCapStr(e.target.value)}
                    placeholder="1030.00"
                    className="w-full bg-zinc-950/70 border border-blue-500/60 rounded-xl py-2.5 pl-10 pr-4 font-mono text-sm text-zinc-100 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                </div>
                <span className="text-[11px] text-blue-400 mt-1 block">
                  Sobrescreve a fórmula automática no cálculo do burn rate e ritmo diário.
                </span>
              </motion.div>
            )}
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold transition-all shadow-md cursor-pointer"
            >
              {loading ? 'Salvando...' : 'Salvar Limites'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 9. NEW RECURRING SUBSCRIPTION MODAL
interface NewSubscriptionModalProps {
  isOpen: boolean;
  currentCycle?: string;
  onClose: () => void;
  onSuccess: () => void;
}

export function NewSubscriptionModal({
  isOpen,
  currentCycle,
  onClose,
  onSuccess,
}: NewSubscriptionModalProps) {
  const [title, setTitle] = useState('');
  const [amountStr, setAmountStr] = useState('');
  const [billingDay, setBillingDay] = useState(10);
  const [paymentSource, setPaymentSource] = useState<'CARD' | 'PIX_CAIXINHA'>('CARD');
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amount = parseFloat(amountStr.replace(',', '.'));
    if (!title.trim() || isNaN(amount) || amount <= 0) return;

    setLoading(true);
    try {
      const cycle = currentCycle || new Date().toISOString().substring(0, 7);
      await api.addCommitment({
        title: title.trim(),
        amount,
        startCycle: cycle,
        totalInstallments: null,
        isActive: true,
        commitmentType: 'SUBSCRIPTION',
        paymentSource,
        billingDay: Number(billingDay),
      });
      onSuccess();
      onClose();
      setTitle('');
      setAmountStr('');
      setBillingDay(10);
      setPaymentSource('CARD');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao adicionar assinatura';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-purple-500/20 text-purple-400">
              <RefreshCw className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Nova Assinatura Fixa</h3>
              <span className="text-[10px] font-mono text-purple-400">Lançamento Recorrente Mensal</span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
              Nome do Serviço
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Ex: Google One, Spotify, Netflix, Academia..."
              className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl px-3.5 py-2.5 text-sm text-zinc-100 focus:outline-none focus:border-purple-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Valor Mensal (R$)
              </label>
              <div className="relative">
                <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                  R$
                </span>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  required
                  value={amountStr}
                  onChange={(e) => setAmountStr(e.target.value)}
                  placeholder="29.90"
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl py-2.5 pl-10 pr-3 font-mono text-sm text-zinc-100 focus:outline-none focus:border-purple-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1">
                Dia de Cobrança
              </label>
              <div className="relative">
                <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                  Dia
                </span>
                <input
                  type="number"
                  min="1"
                  max="31"
                  required
                  value={billingDay}
                  onChange={(e) => setBillingDay(Number(e.target.value))}
                  className="w-full bg-zinc-950/70 border border-zinc-700 rounded-xl py-2.5 pl-12 pr-3 font-mono text-sm text-zinc-100 focus:outline-none focus:border-purple-500"
                />
              </div>
            </div>
          </div>

          {/* Payment Source Selector */}
          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-2">
              Forma de Pagamento Recorrente
            </label>
            <div className="grid grid-cols-2 gap-2 p-1 bg-zinc-950/80 rounded-2xl border border-zinc-800">
              <button
                type="button"
                onClick={() => setPaymentSource('CARD')}
                className={`py-2 px-2 text-center rounded-xl text-xs font-semibold transition-all flex items-center justify-center gap-1.5 ${
                  paymentSource === 'CARD'
                    ? 'bg-emerald-500 text-zinc-950 shadow-md font-bold'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <CreditCard className="w-3.5 h-3.5" />
                <span>💳 Cartão de Crédito</span>
              </button>
              <button
                type="button"
                onClick={() => setPaymentSource('PIX_CAIXINHA')}
                className={`py-2 px-2 text-center rounded-xl text-xs font-semibold transition-all flex items-center justify-center gap-1.5 ${
                  paymentSource === 'PIX_CAIXINHA'
                    ? 'bg-cyan-500 text-zinc-950 shadow-md font-bold'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <PiggyBank className="w-3.5 h-3.5" />
                <span>🐷 Pix Caixinha</span>
              </button>
            </div>
            <p className="text-[11px] text-zinc-500 mt-1.5 leading-tight">
              {paymentSource === 'CARD'
                ? '🔒 Assinaturas no cartão são computadas na fatura do ciclo e projetadas automaticamente para os próximos meses.'
                : '🐷 Assinaturas debitadas via Pix da Caixinha deduzem do saldo investido.'}
            </p>
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold transition-all shadow-md cursor-pointer flex items-center gap-1.5"
            >
              <Plus className="w-3.5 h-3.5" />
              {loading ? 'Adicionando...' : 'Cadastrar Assinatura'}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 9. DEPOSIT CAIXINHA MODAL (Adicionar Valor na Caixinha)
interface DepositCaixinhaModalProps {
  isOpen: boolean;
  currentBalance: number;
  cdiAnnualRate: number;
  onClose: () => void;
  onSuccess: () => void;
}

export function DepositCaixinhaModal({
  isOpen,
  currentBalance,
  cdiAnnualRate,
  onClose,
  onSuccess,
}: DepositCaixinhaModalProps) {
  const [amountStr, setAmountStr] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setAmountStr('');
      setDescription('');
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const amount = parseFloat(amountStr.replace(',', '.')) || 0;
  const newBalance = currentBalance + amount;
  const effectiveAnnual = cdiAnnualRate * 1.15;
  const newMonthlyYield = newBalance * ((effectiveAnnual / 100) / 12);
  const newDailyYield = newMonthlyYield / 30;

  const handlePreset = (val: number) => {
    const current = parseFloat(amountStr.replace(',', '.')) || 0;
    setAmountStr((current + val).toFixed(2));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isNaN(amount) || amount <= 0) return;

    setLoading(true);
    try {
      await api.depositCaixinha(amount, description.trim() || undefined);
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao adicionar valor na Caixinha';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-purple-500/20 text-purple-400">
              <PiggyBank className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Adicionar à Caixinha Nubank</h3>
              <span className="text-[10px] font-mono text-purple-400">Rendimento a 115% do CDI</span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <p className="text-xs text-zinc-400 leading-relaxed">
            Adicione qualquer valor para aumentar seu saldo na Caixinha. O valor adicionado rende imediatamente 115% do CDI.
          </p>

          {/* Quick Presets */}
          <div>
            <label className="block text-xs font-semibold text-zinc-400 mb-1.5">
              Adição Rápida:
            </label>
            <div className="grid grid-cols-4 gap-2">
              {[10, 20, 50, 100].map((val) => (
                <button
                  type="button"
                  key={val}
                  onClick={() => handlePreset(val)}
                  className="py-1.5 rounded-xl bg-purple-500/10 hover:bg-purple-500/20 border border-purple-500/30 text-purple-300 text-xs font-mono font-bold transition-all cursor-pointer"
                >
                  +R$ {val}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
              Valor a Guardar (R$)
            </label>
            <div className="relative">
              <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                R$
              </span>
              <input
                type="number"
                step="0.01"
                min="0.01"
                required
                value={amountStr}
                onChange={(e) => setAmountStr(e.target.value)}
                placeholder="0,00"
                className="w-full rounded-2xl bg-zinc-950 border border-zinc-800 pl-10 pr-4 py-3 text-lg font-mono font-bold text-purple-300 focus:outline-none focus:border-purple-500/60"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
              Motivo / Descrição (Opcional)
            </label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Ex: Sobra da semana, Depósito extra"
              className="w-full rounded-2xl bg-zinc-950 border border-zinc-800 px-4 py-2.5 text-sm text-zinc-200 focus:outline-none focus:border-purple-500/60"
            />
          </div>

          {/* Impact preview */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-zinc-800/80 text-xs space-y-2">
            <div className="flex items-center justify-between text-zinc-400">
              <span>Saldo Atual:</span>
              <span className="font-mono text-zinc-300">R$ {currentBalance.toFixed(2)}</span>
            </div>
            {amount > 0 && (
              <>
                <div className="flex items-center justify-between text-purple-300 font-semibold">
                  <span>+ Valor a Adicionar:</span>
                  <span className="font-mono">+ R$ {amount.toFixed(2)}</span>
                </div>
                <div className="pt-2 border-t border-zinc-800 flex items-center justify-between font-bold text-zinc-100">
                  <span>Novo Saldo:</span>
                  <span className="font-mono text-purple-300 text-sm">
                    R$ {newBalance.toFixed(2)}
                  </span>
                </div>
                <div className="text-[11px] font-mono text-emerald-400 flex items-center justify-between pt-1">
                  <span>Novo rendimento estimado:</span>
                  <span>+R$ {newMonthlyYield.toFixed(2)}/mês (+R$ {newDailyYield.toFixed(2)}/dia)</span>
                </div>
              </>
            )}
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading || amount <= 0}
              className="px-5 py-2.5 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold transition-all shadow-md disabled:opacity-50 cursor-pointer flex items-center gap-1.5"
            >
              <Plus className="w-3.5 h-3.5" />
              {loading ? 'Adicionando...' : `Adicionar R$ ${amount.toFixed(2)}`}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}

// 10. RECEIVE PIX MODAL (Receber Pix Externo / Reembolso -> Direto pra Caixinha)
interface ReceivePixModalProps {
  isOpen: boolean;
  currentBalance: number;
  onClose: () => void;
  onSuccess: () => void;
}

export function ReceivePixModal({
  isOpen,
  currentBalance,
  onClose,
  onSuccess,
}: ReceivePixModalProps) {
  const [description, setDescription] = useState('');
  const [amountStr, setAmountStr] = useState('');
  const [category, setCategory] = useState('Entrada');
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setDescription('');
      setAmountStr('');
      setCategory('Entrada');
      setDate(new Date().toISOString().split('T')[0]);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const amount = parseFloat(amountStr.replace(',', '.')) || 0;
  const newBalance = currentBalance + amount;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isNaN(amount) || amount <= 0 || !description.trim()) return;

    setLoading(true);
    try {
      await api.receivePix({
        description: description.trim(),
        amount,
        category: category.trim() || 'Entrada',
        transactionDate: date ? new Date(date).toISOString() : undefined,
      });
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Falha ao registrar Pix recebido';
      alert(`Erro: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="w-full max-w-md rounded-3xl bg-zinc-900 border border-zinc-800 p-6 shadow-2xl text-zinc-100"
      >
        <div className="flex items-center justify-between pb-4 border-b border-zinc-800">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-emerald-500/20 text-emerald-400">
              <ArrowDownLeft className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-zinc-100">Receber Pix / Reembolso</h3>
              <span className="text-[10px] font-mono text-emerald-400">
                Vai automaticamente para a Caixinha (115% CDI) 🐷
              </span>
            </div>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-200 p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-4">
          <div className="p-3 rounded-2xl bg-emerald-950/20 border border-emerald-500/30 text-xs text-emerald-300 leading-relaxed flex items-start gap-2">
            <PiggyBank className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" />
            <span>
              Pagou algo para alguém e a pessoa te fez um Pix de volta? Registre aqui e o dinheiro será automaticamente guardado na sua Caixinha do Nubank!
            </span>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
              De quem / Motivo
            </label>
            <input
              type="text"
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Ex: Pix do João almoço, Reembolso Pedro"
              className="w-full rounded-2xl bg-zinc-950 border border-zinc-800 px-4 py-3 text-sm text-zinc-100 focus:outline-none focus:border-emerald-500/60"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
              Valor Recebido (R$)
            </label>
            <div className="relative">
              <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm font-mono text-zinc-400">
                R$
              </span>
              <input
                type="number"
                step="0.01"
                min="0.01"
                required
                value={amountStr}
                onChange={(e) => setAmountStr(e.target.value)}
                placeholder="0,00"
                className="w-full rounded-2xl bg-zinc-950 border border-zinc-800 pl-10 pr-4 py-3 text-lg font-mono font-bold text-emerald-400 focus:outline-none focus:border-emerald-500/60"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
                Categoria
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full rounded-2xl bg-zinc-950 border border-zinc-800 px-3 py-2.5 text-xs text-zinc-200 focus:outline-none focus:border-emerald-500/60"
              >
                <option value="Entrada">Entrada / Reembolso</option>
                <option value="Alimentação">Alimentação</option>
                <option value="Transporte">Transporte</option>
                <option value="Lazer">Lazer</option>
                <option value="Compras">Compras</option>
                <option value="Outros">Outros</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-zinc-300 uppercase tracking-wider mb-1.5">
                Data do Pix
              </label>
              <input
                type="date"
                required
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full rounded-2xl bg-zinc-950 border border-zinc-800 px-3 py-2.5 text-xs text-zinc-200 focus:outline-none focus:border-emerald-500/60"
              />
            </div>
          </div>

          {/* Impact preview */}
          <div className="p-3.5 rounded-2xl bg-zinc-950/70 border border-zinc-800/80 text-xs space-y-1.5">
            <div className="flex items-center justify-between text-zinc-400">
              <span>Saldo Atual da Caixinha:</span>
              <span className="font-mono text-zinc-300">R$ {currentBalance.toFixed(2)}</span>
            </div>
            {amount > 0 && (
              <>
                <div className="flex items-center justify-between text-emerald-400 font-semibold">
                  <span>+ Pix Recebido:</span>
                  <span className="font-mono">+ R$ {amount.toFixed(2)}</span>
                </div>
                <div className="pt-1.5 border-t border-zinc-800 flex items-center justify-between font-bold text-zinc-100">
                  <span>Novo Saldo Caixinha:</span>
                  <span className="font-mono text-emerald-400 text-sm">
                    R$ {newBalance.toFixed(2)}
                  </span>
                </div>
              </>
            )}
          </div>

          <div className="pt-2 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-medium text-zinc-400 hover:text-zinc-200"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading || amount <= 0 || !description.trim()}
              className="px-5 py-2.5 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-xs font-bold transition-all shadow-md disabled:opacity-50 cursor-pointer flex items-center gap-1.5"
            >
              <ArrowDownLeft className="w-3.5 h-3.5" />
              {loading ? 'Registrando...' : `Confirmar Pix (+ R$ ${amount.toFixed(2)})`}
            </button>
          </div>
        </form>
      </motion.div>
    </div>
  );
}
