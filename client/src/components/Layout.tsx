import { Link, NavLink, Outlet } from 'react-router-dom';
import { Toaster } from '@/components/ui/sonner';
import { ThemeToggle } from './ThemeToggle';

export function Layout() {
  return (
    <div className="bg-background text-foreground relative min-h-full">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[420px] opacity-70"
        style={{
          background:
            'radial-gradient(ellipse 70% 60% at 50% -10%, var(--color-warm-from) 0%, transparent 70%)',
        }}
      />
      <header className="border-border/70 bg-background/60 supports-[backdrop-filter]:bg-background/40 sticky top-0 z-30 border-b backdrop-blur">
        <div className="mx-auto flex max-w-3xl items-center justify-between px-4 py-4">
          <Link
            to="/"
            className="font-serif text-xl tracking-tight"
            style={{ fontVariationSettings: '"opsz" 144' }}
          >
            Invitér nu
          </Link>
          <nav className="flex items-center gap-1 text-sm">
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                `text-muted-foreground hover:text-foreground rounded-md px-3 py-1.5 transition-colors ${
                  isActive ? 'text-foreground' : ''
                }`
              }
            >
              Opret
            </NavLink>
            <NavLink
              to="/mine"
              className={({ isActive }) =>
                `text-muted-foreground hover:text-foreground rounded-md px-3 py-1.5 transition-colors ${
                  isActive ? 'text-foreground' : ''
                }`
              }
            >
              Mine events
            </NavLink>
            <div className="ml-1">
              <ThemeToggle />
            </div>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-3xl px-4 py-10 sm:py-14">
        <Outlet />
      </main>
      <Toaster richColors closeButton position="bottom-right" />
    </div>
  );
}
