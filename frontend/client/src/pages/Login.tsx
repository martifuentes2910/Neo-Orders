import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { api } from '../api';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@neoorders.com');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await api.antiforgery();
      await login(email, password);
      navigate('/panel');
    } catch (err: any) {
      setError(err.message === 'UNAUTHORIZED' ? 'Credenciales inválidas' : err.message || 'Error al iniciar sesión');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-brand">
          <svg viewBox="0 0 40 40" width="44" height="44">
            <defs>
              <linearGradient id="lg2" x1="0" y1="0" x2="1" y2="1">
                <stop offset="0%" stopColor="#ff5c7a" />
                <stop offset="50%" stopColor="#ff8e72" />
                <stop offset="100%" stopColor="#f5a65b" />
              </linearGradient>
            </defs>
            <rect x="2" y="2" width="36" height="36" rx="10" fill="url(#lg2)" />
            <path d="M12 28 V12 L20 22 L28 12 V28" stroke="#fff" strokeWidth="2.6" fill="none" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          <div>
            <h1>NeoOrders</h1>
            <p>Sistema de gestión de pedidos</p>
          </div>
        </div>
        <form onSubmit={submit} className="login-form">
          <label>
            Email
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>
          <label>
            Contraseña
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </label>
          {error && <div className="form-error">{error}</div>}
          <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
            {loading ? 'Ingresando…' : 'Iniciar sesión'}
          </button>
        </form>
        <p className="login-hint">
          Cuentas de ejemplo: admin@neoorders.com / Admin123! · empleado@neoorders.com / Empleado123!
        </p>
      </div>
    </div>
  );
}
