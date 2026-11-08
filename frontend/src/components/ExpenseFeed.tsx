'use client';

import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { CreditCard, Tag, Calendar, Trash2, Edit2, Plus, Search, Download, X, Send } from 'lucide-react';
import { CardExpense, CategoryBreakdown } from '@/lib/api';

interface ExpenseFeedProps {
  expenses: CardExpense[];
  categories: CategoryBreakdown[];
  selectedCategory: string | null;
  onSelectCategory: (cat: string | null) => void;
  onEditExpense: (expense: CardExpense) => void;
  onDeleteExpense: (expense: CardExpense) => void;
  onOpenNewExpense: () => void;
}

export function ExpenseFeed({
  expenses,
  categories,
  selectedCategory,
  onSelectCategory,
  onEditExpense,
  onDeleteExpense,
  onOpenNewExpense,
}: ExpenseFeedProps) {
  const [searchQuery, setSearchQuery] = useState('');

  const filteredExpenses = expenses.filter((e) => {
    const matchesCategory = selectedCategory
      ? e.category.toLowerCase() === selectedCategory.toLowerCase()
      : true;

    const matchesSearch = searchQuery.trim()
      ? e.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
        e.category.toLowerCase().includes(searchQuery.toLowerCase()) ||
        e.amount.toString().includes(searchQuery)
      : true;

    return matchesCategory && matchesSearch;
  });

  const handleExportCsv = () => {
    if (expenses.length === 0) return;

    const headers = ['Data', 'Descricao', 'Categoria', 'Parcela', 'Valor_RS'];
    const rows = filteredExpenses.map((e) => [
      new Date(e.transactionDate).toLocaleDateString('pt-BR'),
      `"${e.description.replace(/"/g, '""')}"`,
      e.category,
      e.installmentTotal > 1 ? `${e.installmentCurrent}/${e.installmentTotal}` : 'A vista',
      e.amount.toFixed(2).replace('.', ','),
    ]);

    const csvContent = '\uFEFF' + [headers.join(';'), ...rows.map((r) => r.join(';'))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `fatura-gastos-${expenses[0]?.billingCycle || 'export'}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // Category badge color helper
  const getCategoryColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'alimentação':
        return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'transporte':
        return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      case 'lazer':
        return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
      case 'compras':
        return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'assinaturas':
        return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      case 'saúde':
        return 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20';
      default:
        return 'bg-zinc-800 text-zinc-300 border-zinc-700';
    }
  };

  return (
    <div className="rounded-3xl border border-zinc-800 bg-zinc-900/80 p-6 shadow-xl backdrop-blur-md mb-8">
      {/* Header & Category Filter Pills */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6 pb-4 border-b border-zinc-800/80">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-xl bg-emerald-500/10 text-emerald-400">
            <CreditCard className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-bold text-zinc-100 flex items-center gap-2">
              Lançamentos da Fatura
              <span className="text-xs font-mono font-medium text-zinc-400">
                ({filteredExpenses.length} itens)
              </span>
            </h2>
            <p className="text-xs text-zinc-400">
              Gastos do cartão computados para o fechamento deste ciclo.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 self-start sm:self-auto flex-wrap">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Buscar gasto..."
              className="bg-zinc-950/70 border border-zinc-700/80 rounded-xl py-1.5 pl-8 pr-7 text-xs text-zinc-200 placeholder-zinc-500 focus:outline-none focus:border-emerald-500 w-36 sm:w-44"
            />
            {searchQuery && (
              <button
                onClick={() => setSearchQuery('')}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-zinc-500 hover:text-zinc-300"
              >
                <X className="w-3 h-3" />
              </button>
            )}
          </div>

          <button
            onClick={handleExportCsv}
            title="Exportar lançamentos deste ciclo para planilha Excel/CSV"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-zinc-800/80 hover:bg-zinc-700 border border-zinc-700 text-zinc-300 hover:text-zinc-100 text-xs font-medium transition-all cursor-pointer"
          >
            <Download className="w-3.5 h-3.5" />
            <span className="hidden sm:inline">CSV</span>
          </button>

          <button
            onClick={onOpenNewExpense}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-xs font-bold transition-all cursor-pointer shadow-sm"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Lançar Manual</span>
          </button>
        </div>
      </div>

      {/* Category breakdown filter pills */}
      <div className="flex items-center gap-2 overflow-x-auto pb-3 mb-4 scrollbar-none">
        <button
          onClick={() => onSelectCategory(null)}
          className={`text-xs px-3 py-1.5 rounded-xl font-medium border whitespace-nowrap transition-all ${
            selectedCategory === null
              ? 'bg-zinc-100 text-zinc-950 border-zinc-100 font-bold'
              : 'bg-zinc-950/60 text-zinc-400 border-zinc-800 hover:text-zinc-200'
          }`}
        >
          Todos ({expenses.length})
        </button>

        {categories.map((cat) => {
          const isSelected = selectedCategory?.toLowerCase() === cat.category.toLowerCase();
          return (
            <button
              key={cat.category}
              onClick={() => onSelectCategory(isSelected ? null : cat.category)}
              className={`text-xs px-3 py-1.5 rounded-xl font-medium border whitespace-nowrap flex items-center gap-1.5 transition-all ${
                isSelected
                  ? 'bg-zinc-100 text-zinc-950 border-zinc-100 font-bold'
                  : 'bg-zinc-950/60 text-zinc-400 border-zinc-800 hover:text-zinc-200'
              }`}
            >
              <span>{cat.category}</span>
              <span className="text-[10px] font-mono opacity-80">
                (R$ {cat.amount.toFixed(0)})
              </span>
            </button>
          );
        })}
      </div>

      {/* Expenses List */}
      {filteredExpenses.length > 0 ? (
        <div className="space-y-2.5">
          <AnimatePresence>
            {filteredExpenses.map((expense) => {
              const isInstallment = expense.installmentTotal > 1;
              const isPix = expense.paymentMethod === 'PIX_CAIXINHA';

              return (
                <motion.div
                  key={expense.id}
                  initial={{ opacity: 0, y: 5 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -5 }}
                  className="flex items-center justify-between p-3.5 rounded-2xl bg-zinc-950/60 border border-zinc-800/80 hover:border-zinc-700 transition-all group"
                >
                  <div className="flex items-center gap-3.5">
                    <div className={`w-9 h-9 rounded-xl border flex items-center justify-center transition-colors ${
                      isPix ? 'bg-cyan-500/10 border-cyan-500/20 text-cyan-400' : 'bg-zinc-900 border-zinc-800 text-zinc-400 group-hover:text-zinc-200'
                    }`}>
                      {isPix ? <Send className="w-4 h-4" /> : <Tag className="w-4 h-4" />}
                    </div>

                    <div>
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-semibold text-sm text-zinc-100">
                          {expense.description}
                        </span>

                        {isPix ? (
                          <span className="text-[10px] font-mono font-semibold bg-cyan-500/10 text-cyan-400 border border-cyan-500/30 px-1.5 py-0.5 rounded-md flex items-center gap-1">
                            💸 Pix Caixinha
                          </span>
                        ) : (
                          <span className="text-[10px] font-mono font-medium bg-zinc-800 text-zinc-400 border border-zinc-700 px-1.5 py-0.5 rounded-md flex items-center gap-1">
                            💳 Cartão
                          </span>
                        )}

                        <span
                          className={`text-[10px] font-medium px-2 py-0.5 rounded-full border ${getCategoryColor(
                            expense.category
                          )}`}
                        >
                          {expense.category}
                        </span>

                        {isInstallment && (
                          <span className="text-[10px] font-mono font-semibold bg-zinc-800 text-amber-400 border border-amber-500/30 px-1.5 py-0.5 rounded-md">
                            {expense.installmentCurrent}/{expense.installmentTotal}x
                          </span>
                        )}
                      </div>

                      <div className="flex items-center gap-3 text-xs text-zinc-500 font-mono mt-0.5">
                        <span className="flex items-center gap-1">
                          <Calendar className="w-3 h-3 text-zinc-500" />
                          {new Date(expense.transactionDate).toLocaleDateString('pt-BR')}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <span className="font-mono font-bold text-base text-zinc-100">
                        R${' '}
                        {expense.amount.toLocaleString('pt-BR', {
                          minimumFractionDigits: 2,
                          maximumFractionDigits: 2,
                        })}
                      </span>
                      {isInstallment && (
                        <span className="block text-[10px] font-mono text-zinc-500">
                          Total: R$ {(expense.amount * expense.installmentTotal).toFixed(2)}
                        </span>
                      )}
                    </div>

                    <div className="flex items-center gap-1 opacity-80 group-hover:opacity-100 transition-opacity">
                      <button
                        onClick={() => onEditExpense(expense)}
                        title="Editar lançamento"
                        className="p-1.5 rounded-lg text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800 transition-colors"
                      >
                        <Edit2 className="w-4 h-4" />
                      </button>

                      <button
                        onClick={() => onDeleteExpense(expense)}
                        title="Excluir lançamento"
                        className="p-1.5 rounded-lg text-zinc-500 hover:text-rose-400 hover:bg-rose-500/10 transition-colors"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </motion.div>
              );
            })}
          </AnimatePresence>
        </div>
      ) : (
        <div className="py-12 flex flex-col items-center justify-center text-center">
          <div className="w-12 h-12 rounded-2xl bg-zinc-800/60 border border-zinc-700/60 flex items-center justify-center text-zinc-500 mb-3">
            <CreditCard className="w-6 h-6" />
          </div>
          <h3 className="text-sm font-semibold text-zinc-300">Nenhum gasto neste ciclo</h3>
          <p className="text-xs text-zinc-500 max-w-sm mt-1 mb-4">
            Use a barra rápida com IA acima ou clique no botão abaixo para adicionar lançamentos à fatura.
          </p>
          <button
            onClick={onOpenNewExpense}
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 text-xs font-bold transition-all shadow-md"
          >
            <Plus className="w-4 h-4" />
            <span>Adicionar Primeiro Gasto</span>
          </button>
        </div>
      )}
    </div>
  );
}
