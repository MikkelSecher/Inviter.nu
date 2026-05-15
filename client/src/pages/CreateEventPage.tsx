import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion } from 'motion/react';
import { CalendarClock, Sparkles, X } from 'lucide-react';
import { api, ApiError } from '../api/client';
import type { ContactRequirement } from '../api/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { Field } from '@/components/Field';
import { DateTimePicker } from '@/components/DateTimePicker';
import { fromDatetimeLocalValue } from '../lib/format';
import { rememberEvent } from '../lib/myEvents';

export function CreateEventPage() {
  const navigate = useNavigate();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [startsAt, setStartsAt] = useState('');
  const [allowMaybe, setAllowMaybe] = useState(false);
  const [rsvpDeadline, setRsvpDeadline] = useState('');
  const [showDeadline, setShowDeadline] = useState(false);
  const [contactRequirement, setContactRequirement] = useState<ContactRequirement>('None');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (!title.trim()) {
      setError('Titel er påkrævet.');
      return;
    }
    if (!startsAt) {
      setError('Dato og tid er påkrævet.');
      return;
    }
    if (rsvpDeadline && new Date(rsvpDeadline) > new Date(startsAt)) {
      setError('SU-deadline kan ikke ligge efter eventet.');
      return;
    }
    setSubmitting(true);
    try {
      const created = await api.createEvent({
        title: title.trim(),
        description: description.trim(),
        startsAt: fromDatetimeLocalValue(startsAt),
        allowMaybe,
        rsvpDeadline: rsvpDeadline ? fromDatetimeLocalValue(rsvpDeadline) : null,
        contactRequirement,
      });
      rememberEvent({
        id: created.id,
        title: created.title,
        adminToken: created.adminToken,
        createdAt: created.createdAt,
      });
      navigate(`/manage/${created.adminToken}`, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kunne ikke oprette event.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      className="space-y-8"
    >
      <div className="space-y-3">
        <div className="text-primary inline-flex items-center gap-1.5 text-xs font-medium tracking-wide uppercase">
          <Sparkles className="size-3.5" />
          Et nyt event
        </div>
        <h1
          className="font-serif text-4xl leading-[1.05] tracking-tight sm:text-5xl"
          style={{ fontVariationSettings: '"opsz" 144' }}
        >
          Saml folk omkring noget rart.
        </h1>
        <p className="text-muted-foreground max-w-prose text-base">
          Udfyld det vigtigste. Du får et delbart invite-link og en hemmelig admin-URL bagefter — så
          kan du følge med i hvem der kommer.
        </p>
      </div>

      <Card>
        <CardContent className="pt-6">
          <form onSubmit={onSubmit} className="space-y-5">
            <Field label="Titel" htmlFor="title">
              <Input
                id="title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Sommerfest"
                maxLength={200}
                autoFocus
              />
            </Field>

            <Field
              label="Beskrivelse"
              htmlFor="description"
              hint="Praktisk info, dresscode, beskeder til gæsterne."
            >
              <Textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Vi mødes i haven, tag gerne en flaske vin med…"
                maxLength={4000}
                rows={5}
              />
            </Field>

            <Field label="Hvornår" htmlFor="startsAt">
              <DateTimePicker
                id="startsAt"
                value={startsAt}
                onChange={setStartsAt}
              />
            </Field>

            {showDeadline ? (
              <Field
                label="Senest tilmelding"
                htmlFor="rsvpDeadline"
                hint="Efter denne dato kan gæster ikke længere svare."
              >
                <div className="flex items-center gap-2">
                  <div className="flex-1">
                    <DateTimePicker
                      id="rsvpDeadline"
                      value={rsvpDeadline}
                      onChange={setRsvpDeadline}
                      maxDateTime={startsAt || undefined}
                      clearable
                    />
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                      setRsvpDeadline('');
                      setShowDeadline(false);
                    }}
                    aria-label="Fjern tilmeldingsfrist"
                  >
                    <X className="size-4" />
                  </Button>
                </div>
              </Field>
            ) : (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setShowDeadline(true)}
                className="text-muted-foreground hover:text-foreground -ml-2 self-start"
              >
                <CalendarClock className="size-4" />
                Sæt tilmeldingsfrist
              </Button>
            )}

            <div className="border-border/70 flex items-center justify-between gap-4 rounded-lg border p-4">
              <div className="space-y-0.5">
                <Label htmlFor="allowMaybe" className="text-sm font-medium">
                  Tillad "måske"-svar
                </Label>
                <p className="text-muted-foreground text-xs">
                  Når slået fra, kan gæster kun vælge "kommer" eller "kommer ikke".
                </p>
              </div>
              <Switch id="allowMaybe" checked={allowMaybe} onCheckedChange={setAllowMaybe} />
            </div>

            <div className="space-y-2">
              <Label className="text-sm font-medium">Kontaktoplysninger fra gæsten</Label>
              <p className="text-muted-foreground text-xs">
                Vælg om gæsten skal opgive email eller telefonnummer ved tilmelding.
              </p>
              <RadioGroup
                value={contactRequirement}
                onValueChange={(v) => setContactRequirement(v as ContactRequirement)}
                className="grid grid-cols-3 gap-2 pt-1"
              >
                {(
                  [
                    { v: 'None', label: 'Ingen' },
                    { v: 'Email', label: 'Email' },
                    { v: 'Phone', label: 'Telefon' },
                  ] as const
                ).map((opt) => (
                  <Label
                    key={opt.v}
                    htmlFor={`contact-${opt.v}`}
                    className="border-border bg-background hover:bg-accent/50 has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary has-[[data-state=checked]]:text-primary-foreground flex cursor-pointer items-center justify-center gap-2 rounded-md border px-3 py-2.5 text-sm font-medium transition-colors"
                  >
                    <RadioGroupItem id={`contact-${opt.v}`} value={opt.v} className="sr-only" />
                    {opt.label}
                  </Label>
                ))}
              </RadioGroup>
            </div>

            {error && <p className="text-destructive text-sm">{error}</p>}

            <div className="pt-2">
              <Button type="submit" disabled={submitting} size="lg">
                {submitting ? 'Opretter…' : 'Opret event'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </motion.div>
  );
}
