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
  const [location, setLocation] = useState('');
  const [startsAt, setStartsAt] = useState('');
  const [allowMaybe, setAllowMaybe] = useState(false);
  const [rsvpDeadline, setRsvpDeadline] = useState('');
  const [showDeadline, setShowDeadline] = useState(false);
  const [contactRequirement, setContactRequirement] = useState<ContactRequirement>('None');
  const [organizerName, setOrganizerName] = useState('');
  const [organizerEmail, setOrganizerEmail] = useState('');
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
        location: location.trim(),
        startsAt: fromDatetimeLocalValue(startsAt),
        allowMaybe,
        rsvpDeadline: rsvpDeadline ? fromDatetimeLocalValue(rsvpDeadline) : null,
        contactRequirement,
        organizerName: organizerName.trim() || null,
        organizerEmail: organizerEmail.trim() || null,
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

            <Field
              label="Sted (valgfri)"
              htmlFor="location"
              hint="Gade-adresse eller navn på stedet."
            >
              <Input
                id="location"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                placeholder="Vesterbrogade 12, 1620 København"
                maxLength={500}
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
                Vælg om gæsten skal opgive email ved tilmelding.
              </p>
              <RadioGroup
                value={contactRequirement}
                onValueChange={(v) => setContactRequirement(v as ContactRequirement)}
                className="grid grid-cols-2 gap-2 pt-1"
              >
                {(
                  [
                    { v: 'None', label: 'Ingen' },
                    { v: 'Email', label: 'Email' },
                  ] as const
                ).map((opt) => (
                  <div key={opt.v} className="relative">
                    <RadioGroupItem
                      id={`contact-${opt.v}`}
                      value={opt.v}
                      className="peer sr-only"
                    />
                    <Label
                      htmlFor={`contact-${opt.v}`}
                      className="border-border bg-background hover:bg-accent/50 peer-data-[state=checked]:border-primary peer-data-[state=checked]:bg-primary peer-data-[state=checked]:text-primary-foreground flex cursor-pointer items-center justify-center gap-0 rounded-md border px-3 py-2.5 text-sm font-medium transition-colors"
                    >
                      {opt.label}
                    </Label>
                  </div>
                ))}
              </RadioGroup>
            </div>

            <div className="border-border/70 space-y-4 rounded-lg border p-4">
              <div className="space-y-0.5">
                <Label className="text-sm font-medium">Dig som arrangør (valgfrit)</Label>
                <p className="text-muted-foreground text-xs">
                  Hvis du oplyser din email, sender vi dit admin-link dertil — så du altid kan finde
                  tilbage til eventet, også fra en anden enhed.
                </p>
              </div>
              <Field label="Dit navn" htmlFor="organizerName">
                <Input
                  id="organizerName"
                  name="organizerName"
                  value={organizerName}
                  onChange={(e) => setOrganizerName(e.target.value)}
                  placeholder="Anne Andersen"
                  maxLength={200}
                  autoComplete="off"
                />
              </Field>
              <Field label="Din email" htmlFor="organizerEmail">
                <Input
                  id="organizerEmail"
                  name="email"
                  type="email"
                  value={organizerEmail}
                  onChange={(e) => setOrganizerEmail(e.target.value)}
                  placeholder="dig@example.dk"
                  maxLength={320}
                  autoComplete="email"
                />
              </Field>
            </div>

            {error && <p className="text-destructive text-sm">{error}</p>}

            <div className="flex justify-center pt-2">
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
