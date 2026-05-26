import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { CalendarCheck, Mail, UserCheck, Users } from 'lucide-react';
import { api, ApiError } from '../api/client';
import type { MetricsPeriod, MetricsSnapshot } from '../api/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { NotFoundPage } from './NotFoundPage';

const PERIODS: { value: MetricsPeriod; label: string }[] = [
  { value: '7d', label: '7 dage' },
  { value: '30d', label: '30 dage' },
  { value: '90d', label: '90 dage' },
  { value: 'all', label: 'Altid' },
];

export function MetricsPage() {
  const { slug = '' } = useParams();
  const [period, setPeriod] = useState<MetricsPeriod>('30d');
  const [upcomingOnly, setUpcomingOnly] = useState(false);
  const [snapshot, setSnapshot] = useState<MetricsSnapshot | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await api.getMetrics(slug, period, upcomingOnly);
      setSnapshot(data);
      setNotFound(false);
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setNotFound(true);
      } else {
        toast.error('Kunne ikke hente metrics.');
      }
    } finally {
      setLoading(false);
    }
  }, [slug, period, upcomingOnly]);

  useEffect(() => {
    void load();
  }, [load]);

  if (notFound) return <NotFoundPage />;
  if (loading && snapshot === null) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <Skeleton className="h-9 w-32" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1
          className="font-serif text-4xl tracking-tight"
          style={{ fontVariationSettings: '"opsz" 144' }}
        >
          Metrics
        </h1>
        <p className="text-muted-foreground text-sm">
          Snapshot pr. periode. Mails er ikke knyttet til events, så filteret for fremtidige events
          påvirker ikke mailtallet.
        </p>
      </header>

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="flex flex-wrap items-center gap-2">
            {PERIODS.map((p) => (
              <Button
                key={p.value}
                variant={period === p.value ? 'default' : 'outline'}
                size="sm"
                onClick={() => setPeriod(p.value)}
              >
                {p.label}
              </Button>
            ))}
          </div>
          <div className="flex items-center gap-2">
            <input
              id="upcomingOnly"
              type="checkbox"
              className="h-4 w-4 rounded border-input"
              checked={upcomingOnly}
              onChange={(e) => setUpcomingOnly(e.target.checked)}
            />
            <Label htmlFor="upcomingOnly" className="cursor-pointer">
              Kun fremtidige events
            </Label>
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label="Events"
          value={snapshot?.events}
          loading={loading}
          icon={<CalendarCheck className="h-5 w-5" />}
        />
        <KpiCard
          label="Tilmeldinger"
          value={snapshot?.rsvps}
          loading={loading}
          icon={<UserCheck className="h-5 w-5" />}
        />
        <KpiCard
          label="Inviterede"
          value={snapshot?.invitees}
          loading={loading}
          icon={<Users className="h-5 w-5" />}
        />
        <KpiCard
          label="Mails sendt"
          value={snapshot?.emails}
          loading={loading}
          icon={<Mail className="h-5 w-5" />}
        />
      </div>
    </div>
  );
}

function KpiCard({
  label,
  value,
  loading,
  icon,
}: {
  label: string;
  value: number | undefined;
  loading: boolean;
  icon: React.ReactNode;
}) {
  return (
    <Card>
      <CardContent className="space-y-2 pt-6">
        <div className="text-muted-foreground flex items-center gap-2 text-sm">
          {icon}
          <span>{label}</span>
        </div>
        {loading || value === undefined ? (
          <Skeleton className="h-9 w-20" />
        ) : (
          <div className={cn('font-serif text-4xl tracking-tight')}>{value}</div>
        )}
      </CardContent>
    </Card>
  );
}
