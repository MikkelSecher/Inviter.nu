import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import type { RsvpStatus } from '../api/types';

const styles: Record<RsvpStatus, string> = {
  Yes: 'bg-status-yes text-status-yes-foreground border-transparent',
  Maybe: 'bg-status-maybe text-status-maybe-foreground border-transparent',
  No: 'bg-status-no text-status-no-foreground border-transparent',
};

const labels: Record<RsvpStatus, string> = {
  Yes: 'Kommer',
  Maybe: 'Måske',
  No: 'Kommer ikke',
};

export function StatusBadge({ status, className }: { status: RsvpStatus; className?: string }) {
  return (
    <Badge className={cn(styles[status], 'font-medium', className)} variant="secondary">
      {labels[status]}
    </Badge>
  );
}

export const statusLabel = (s: RsvpStatus) => labels[s];
