import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import type { CompanySettings, User } from '../../types';
import { configFeatureApi } from './api';
import {
  empresaFormFromCompany,
  formatClearSummary,
  perfilFormFromUser,
  type EmpresaFormValues,
  type PerfilFormValues,
} from './model';

/** Guardado del perfil del usuario en sesión. */
export function usePerfilForm(user: User | null, refresh: () => Promise<void>) {
  const [form, setForm] = useState<PerfilFormValues>(() => perfilFormFromUser(user));
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await configFeatureApi.perfil.update(form);
      await refresh();
      setMsg('Perfil actualizado');
      toast.success('Perfil actualizado');
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Error inesperado';
      setMsg(message);
      toast.error(message);
    } finally {
      setSaving(false);
    }
  };

  return { form, setForm, saving, msg, save };
}

/** Datos de la empresa (solo administradores). */
export function useEmpresaForm(
  company: CompanySettings | null,
  update: (data: Partial<CompanySettings>) => Promise<void>,
) {
  const [form, setForm] = useState<EmpresaFormValues>(() => empresaFormFromCompany(company));
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    setForm(empresaFormFromCompany(company));
  }, [company]);

  const save = async () => {
    setSaving(true);
    setMsg(null);
    try {
      await update(form);
      setMsg('Empresa actualizada');
      toast.success('Empresa actualizada');
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Error inesperado';
      setMsg(message);
      toast.error(message);
    } finally {
      setSaving(false);
    }
  };

  return { form, setForm, saving, msg, save };
}

/** Acciones de datos de ejemplo (seed) y borrado operativo (clear). */
export function useDatosConfig() {
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [clearing, setClearing] = useState(false);
  const [showClear, setShowClear] = useState(false);
  const [confirmText, setConfirmText] = useState('');
  const busy = loading || clearing;

  const runSeed = async () => {
    setLoading(true);
    setMsg(null);
    try {
      const r = await configFeatureApi.seed();
      setMsg(r.message || 'Datos verificados');
      toast.success(r.message || 'Datos verificados');
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Error inesperado';
      setMsg(message);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  const runClear = async () => {
    setClearing(true);
    setMsg(null);
    try {
      const r = await configFeatureApi.clear();
      setMsg(formatClearSummary(r.eliminados));
      toast.success(r.message || 'Datos operativos eliminados');
      setShowClear(false);
      setConfirmText('');
      window.setTimeout(() => window.location.reload(), 1200);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error inesperado';
      setMsg(message);
      toast.error(message);
    } finally {
      setClearing(false);
    }
  };

  return {
    msg, setMsg, loading, clearing, busy, showClear, setShowClear, confirmText, setConfirmText,
    runSeed, runClear,
  };
}
