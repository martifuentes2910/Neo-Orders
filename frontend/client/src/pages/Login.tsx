import { Navigate } from 'react-router-dom';
import { motion, useReducedMotion } from 'framer-motion';
import { Eye, EyeOff, LogIn } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import { useLogin } from '@/features/auth';
import Logo from '@/components/Logo';
import LoginIllustration from '@/components/auth/LoginIllustration';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';

/** Página de Login: el formulario y el envío viven en `useLogin`. */
export default function Login() {
  const { palette } = useTheme();
  const reduceMotion = useReducedMotion();
  const {
    user, loading, email, setEmail, password, setPassword,
    showPassword, setShowPassword, error, submitting, submit,
  } = useLogin();

  if (!loading && user) return <Navigate to="/panel" replace />;

  const isClean = palette === 'clean';
  const panelStyle = isClean
    ? undefined
    : {
        background:
          palette === 'menta'
            ? 'linear-gradient(145deg, #0E4D41 0%, #1F7A67 45%, #35C0A3 100%)'
            : 'linear-gradient(145deg, #131418 0%, #ff5c7a 55%, #f5a65b 100%)',
      };

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4 sm:p-6">
      <motion.div
        initial={reduceMotion ? false : { opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: 'easeOut' }}
        className="w-full max-w-5xl"
      >
        <Card className="overflow-hidden border-border/60 shadow-xl">
          <CardContent className="grid p-0 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
            <div className="flex flex-col justify-center p-6 sm:p-10 lg:p-12">
              <div className="mb-8 flex items-center gap-3">
                <Logo size={44} variant="lockup" />
              </div>
              <div className="mb-8 space-y-2">
                <p className="text-sm font-medium text-muted-foreground">Sistema de gestión de pedidos</p>
                <h1 className="text-3xl font-bold tracking-tight">Iniciá sesión</h1>
                <p className="max-w-md text-sm text-muted-foreground">
                  Accedé al panel para gestionar pedidos, clientes, productos y reportes.
                </p>
              </div>

              <form onSubmit={submit} className="space-y-5" noValidate>
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    className="h-11"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="password">Contraseña</Label>
                  <div className="relative">
                    <Input
                      id="password"
                      type={showPassword ? 'text' : 'password'}
                      autoComplete="current-password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      required
                      className="h-11 pr-11"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword((v) => !v)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground transition-colors hover:text-foreground"
                      aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                    >
                      {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                </div>

                {error && (
                  <div
                    role="alert"
                    aria-live="polite"
                    className="rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive"
                  >
                    {error}
                  </div>
                )}

                <Button type="submit" className="h-11 w-full gap-2" disabled={submitting}>
                  <LogIn className="h-4 w-4" />
                  {submitting ? 'Ingresando…' : 'Iniciar sesión'}
                </Button>
              </form>

              <div className="mt-6 rounded-xl border bg-muted/30 p-4 text-xs text-muted-foreground">
                <p className="mb-2 font-medium text-foreground">Cuentas de ejemplo</p>
                <p><span className="font-medium text-foreground">Admin:</span> admin@neoorders.com / Admin123!</p>
                <p><span className="font-medium text-foreground">Empleado:</span> empleado@neoorders.com / Empleado123!</p>
              </div>
            </div>

            <div
              className={`relative hidden min-h-[560px] flex-col justify-between overflow-hidden p-8 md:flex lg:p-10 ${isClean ? 'glass-surface border-l border-border/40' : 'text-white'}`}
              style={panelStyle}
            >
              <div className={`absolute inset-0 ${isClean ? 'bg-[rgba(255,255,255,0.18)] dark:bg-[rgba(255,255,255,0.03)]' : 'bg-[radial-gradient(circle_at_top_right,rgba(255,255,255,0.22),transparent_45%),radial-gradient(circle_at_bottom_left,rgba(0,0,0,0.18),transparent_45%)]'}`} />
              <div className={`relative space-y-4 ${isClean ? 'text-foreground' : ''}`}>
                <p className={`text-sm font-semibold uppercase tracking-[0.18em] ${isClean ? 'text-muted-foreground' : 'text-white/75'}`}>NeoOrders</p>
                <h2 className="max-w-sm text-3xl font-bold leading-tight">
                  Pedidos, clientes y stock en un solo lugar.
                </h2>
                <p className={`max-w-sm text-sm ${isClean ? 'text-muted-foreground' : 'text-white/80'}`}>
                  Una vista clara para seguir el negocio, reducir errores y tomar decisiones con datos reales.
                </p>
                <ul className={`space-y-2 text-sm ${isClean ? 'text-muted-foreground' : 'text-white/85'}`}>
                  <li className="flex items-center gap-2"><span className={`h-1.5 w-1.5 rounded-full ${isClean ? 'bg-[var(--clean-green)]' : 'bg-white'}`} />Seguimiento de pedidos por estado</li>
                  <li className="flex items-center gap-2"><span className={`h-1.5 w-1.5 rounded-full ${isClean ? 'bg-[var(--clean-orange)]' : 'bg-white'}`} />Catálogo con stock e imágenes</li>
                  <li className="flex items-center gap-2"><span className={`h-1.5 w-1.5 rounded-full ${isClean ? 'bg-[var(--clean-violet)]' : 'bg-white'}`} />Reportes para entender el rendimiento</li>
                </ul>
              </div>
              <div className={`relative mt-8 ${isClean ? 'text-foreground' : 'text-white'}`}>
                <LoginIllustration className="mx-auto w-full max-w-[420px]" />
              </div>
            </div>
          </CardContent>
        </Card>
      </motion.div>
    </div>
  );
}
