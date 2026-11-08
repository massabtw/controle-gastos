'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { X, Trash2, Settings2, CreditCard, PiggyBank } from 'lucide-react';
import { api, CardExpense, FinancialProfile, FixedCommitment } from '@/lib/api';

const CATEGORIES = ['Alimentação', 'Transporte', 'Lazer', 'Compras', 'Assinaturas', 'Saúde', 'Outros'];

// 1. NEW EXPENSE MODAL
interface NewExpenseModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function NewExpenseModal({ isOpen, onClose, onSuccess }: NewExpenseModalProps) {
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('Alimentação');
  const [amount, setAmount] = useState('');
  const [installments, setInstallments] = useState(1);
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const numAmount = parseFloat(amount.replace(',', '.'));
    if (!description.trim() || isNaN(numAmount) || numAmount <= 0) return;

    setLoading(true);
    try {
      await api.createExpense({
        description: description.trim(),
        category,
        totalAmount: numAmount,
        installments: Number(installments),
        transactionDate: date ? new Date(date).toISOString() : undefined,
      });
      onSuccess();
      onClose();
      setDescription('');
      setAmount('');
      setInstallments(1);
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
          <h3 className="text-base font-bold text-zinc-100">Novo Lançamento no Cartão</h3>
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
              placeholder="Ex: Supermercado, Uber, Teclado..."
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
          </div>

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
              className="px-5 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-sm font-bold shadow-md transition-all cursor-pointer"
            >
              {loading ? 'Salvando...' : 'Salvar Gasto'}
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
            Compromissos Pix Mãe ({commitments.length})
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
              {commitments.map((c) => (
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
