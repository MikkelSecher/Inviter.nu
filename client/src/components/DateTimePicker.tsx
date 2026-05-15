import { useMemo } from 'react';
import { da } from 'date-fns/locale';
import { Clock, X } from 'lucide-react';
import { Calendar } from '@/components/ui/calendar';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';

interface DateTimePickerProps {
  id: string;
  value: string; // "YYYY-MM-DDTHH:mm" or empty
  onChange: (v: string) => void;
  maxDateTime?: string; // same format; dates after this are disabled
  clearable?: boolean;
  defaultTime?: string; // default time when picking first date, e.g. "18:00"
}

const DEFAULT_TIME = '18:00';

function parseValue(value: string): { date: Date | undefined; time: string } {
  if (!value) return { date: undefined, time: '' };
  const [datePart, timePart] = value.split('T');
  if (!datePart) return { date: undefined, time: '' };
  // Use noon to dodge DST edge cases when comparing day boundaries
  const date = new Date(`${datePart}T12:00:00`);
  return {
    date: isNaN(date.getTime()) ? undefined : date,
    time: timePart?.slice(0, 5) ?? '',
  };
}

function combine(date: Date | undefined, time: string): string {
  if (!date) return '';
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}T${time || DEFAULT_TIME}`;
}

export function DateTimePicker({
  id,
  value,
  onChange,
  maxDateTime,
  clearable = false,
  defaultTime = DEFAULT_TIME,
}: DateTimePickerProps) {
  const { date, time } = useMemo(() => parseValue(value), [value]);
  const maxDate = useMemo(() => {
    if (!maxDateTime) return undefined;
    const parsed = parseValue(maxDateTime);
    return parsed.date;
  }, [maxDateTime]);

  function handleDate(d: Date | undefined) {
    onChange(combine(d, time || defaultTime));
  }

  function handleTime(t: string) {
    if (!date) return;
    onChange(combine(date, t));
  }

  const disabled = maxDate
    ? (d: Date) => {
        const dayStart = new Date(d.getFullYear(), d.getMonth(), d.getDate());
        const maxDayStart = new Date(
          maxDate.getFullYear(),
          maxDate.getMonth(),
          maxDate.getDate(),
        );
        return dayStart > maxDayStart;
      }
    : undefined;

  return (
    <div className="border-border bg-card inline-flex w-fit flex-col overflow-hidden rounded-lg border">
      <Calendar
        mode="single"
        selected={date}
        onSelect={handleDate}
        disabled={disabled}
        locale={da}
        captionLayout="dropdown"
        showOutsideDays={false}
        className="bg-transparent p-3"
      />
      <div className="border-border bg-muted/40 flex items-center gap-2 border-t px-3 py-2">
        <Clock className="text-muted-foreground size-4 shrink-0" />
        <Label htmlFor={`${id}-time`} className="text-muted-foreground text-xs">
          Klokken
        </Label>
        <Input
          id={`${id}-time`}
          type="time"
          value={time}
          onChange={(e) => handleTime(e.target.value)}
          disabled={!date}
          className="h-8 w-28"
        />
        {clearable && value && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => onChange('')}
            className="text-muted-foreground hover:text-foreground ml-auto h-8"
          >
            <X className="mr-1 size-3.5" /> Ryd
          </Button>
        )}
      </div>
    </div>
  );
}
