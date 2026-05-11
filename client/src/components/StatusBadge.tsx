import type { RsvpStatus } from '../api/types';

const styles: Record<RsvpStatus, string> = {
  Yes: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  Maybe: 'bg-amber-100 text-amber-800 ring-amber-200',
  No: 'bg-rose-100 text-rose-800 ring-rose-200',
};

const labels: Record<RsvpStatus, string> = {
  Yes: 'Kommer',
  Maybe: 'Måske',
  No: 'Kommer ikke',
};

export function StatusBadge({ status }: { status: RsvpStatus }) {
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${styles[status]}`}>
      {labels[status]}
    </span>
  );
}

export const statusLabel = (s: RsvpStatus) => labels[s];
