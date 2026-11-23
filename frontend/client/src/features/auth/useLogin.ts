import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

/** Estado del formulario de login; el acceso a la sesión sigue en AuthContext. */
export function useLogin() {
  const { login, user, loading } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@neoorders.com');
  const [password, setPassword] = useState('Admin123!');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email, password);
      navigate('/panel');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al iniciar sesión';
      setError(msg === 'UNAUTHORIZED' ? 'Credenciales inválidas. Verificá tu email y contraseña.' : msg);
    } finally {
      setSubmitting(false);
    }
  };

  return {
    user, loading,
    email, setEmail,
    password, setPassword,
    showPassword, setShowPassword,
    error, submitting, submit,
  };
}
