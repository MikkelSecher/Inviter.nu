import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { AnimatePresence, motion } from 'motion/react';
import { Calendar, CalendarClock, CheckCircle2, Lock, MapPin } from 'lucide-react';
import { api, ApiError } from '../api/client';
import type { EventPublic, RsvpStatus } from '../api/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import { Field } from '@/components/Field';
import { cn } from '@/lib/utils';
import { formatEventTime } from '../lib/format';

export function InvitePage() {
  const { token = '' } = useParams();
  const [event, setEvent] = useState<EventPublic | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [status, setStatus] = useState<RsvpStatus | null>(null);
  const [comment, setComment] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoadError(null);
    api
      .getInvite(token)
      .then((ev) => {
        if (!cancelled) setEvent(ev);
      })
      .catch((err) => {
        if (cancelled) return;
        setLoadError(
          err instanceof ApiError && err.status === 404
            ? 'Vi kunne ikke finde dette event. Tjek at linket er korrekt.'
            : 'Kunne ikke hente event.',
        );
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  const closed = useMemo(
    () => (event?.rsvpDeadline ? new Date() > new Date(event.rsvpDeadline) : false),
    [event],
  );

  const statusChoices = useMemo(() => {
    const all: { value: RsvpStatus; label: string }[] = [
      { value: 'Yes', label: 'Jeg kommer' },
      { value: 'Maybe', label: 'Måske' },
      { value: 'No', label: 'Kan ikke' },
    ];
    return event?.allowMaybe ? all : all.filter((c) => c.value !== 'Maybe');
  }, [event]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!event) return;
    setError(null);
    if (!name.trim()) {
      setError('Skriv venligst dit navn.');
      return;
    }
    if (!status) {
      setError('Vælg om du kommer.');
      return;
    }
    if (event.contactRequirement === 'Email' && !email.trim()) {
      setError('Email er påkrævet.');
      return;
    }
    if (event.contactRequirement === 'Phone' && phone.trim().length < 5) {
      setError('Telefonnummer er påkrævet.');
      return;
    }
    setSubmitting(true);
    try {
      await api.submitRsvp(token, {
        guestName: name.trim(),
        status,
        comment: comment.trim() ? comment.trim() : null,
        email: event.contactRequirement === 'Email' ? email.trim() : null,
        phone: event.contactRequirement === 'Phone' ? phone.trim() : null,
      });
      setSubmitted(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kunne ikke sende tilbagemelding.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loadError) {
    return (
      <Card>
        <CardContent className="pt-6 text-sm">{loadError}</CardContent>
      </Card>
    );
  }
  if (!event) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      className="space-y-8"
    >
      <Card
        className="border-transparent shadow-lg"
        style={{
          background:
            'linear-gradient(135deg, var(--color-warm-from) 0%, var(--color-warm-to) 100%)',
        }}
      >
        <CardContent className="space-y-4 px-8 py-10 sm:px-10 sm:py-14">
          <div className="text-foreground/70 inline-flex items-center gap-1.5 text-xs font-medium tracking-wide uppercase">
            Du er inviteret
          </div>
          <h1
            className="font-serif text-4xl leading-[1.05] tracking-tight sm:text-5xl"
            style={{ fontVariationSettings: '"opsz" 144' }}
          >
            {event.title}
          </h1>
          <div className="text-foreground/80 flex items-center gap-2 text-base">
            <Calendar className="size-4" />
            {formatEventTime(event.startsAt)}
          </div>
          {event.location && (
            <div className="text-foreground/80 flex items-center gap-2 text-base">
              <MapPin className="size-4" />
              <a
                href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(event.location)}`}
                target="_blank"
                rel="noreferrer"
                className="hover:underline"
              >
                {event.location}
              </a>
            </div>
          )}
          {event.rsvpDeadline && !closed && (
            <div className="text-foreground/75 flex items-center gap-2 text-sm">
              <CalendarClock className="size-4" />
              Svar inden {formatEventTime(event.rsvpDeadline)}
            </div>
          )}
          {event.description && (
            <p className="text-foreground/85 max-w-prose pt-2 text-base whitespace-pre-wrap">
              {event.description}
            </p>
          )}
        </CardContent>
      </Card>

      <AnimatePresence mode="wait">
        {closed ? (
          <motion.div
            key="closed"
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
          >
            <Card>
              <CardContent className="space-y-3 pt-6 text-center">
                <Lock className="text-muted-foreground mx-auto size-8" />
                <h2 className="font-serif text-2xl tracking-tight">Tilmelding lukket</h2>
                <p className="text-muted-foreground text-sm">
                  Fristen for at svare var{' '}
                  {event.rsvpDeadline ? formatEventTime(event.rsvpDeadline) : ''}. Kontakt
                  arrangøren direkte hvis du stadig vil med.
                </p>
              </CardContent>
            </Card>
          </motion.div>
        ) : submitted ? (
          <motion.div
            key="thanks"
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.3, ease: 'easeOut' }}
          >
            <Card>
              <CardContent className="space-y-3 pt-6 text-center">
                <CheckCircle2 className="text-primary mx-auto size-10" />
                <h2 className="font-serif text-2xl tracking-tight">Tak for din tilbagemelding!</h2>
                <p className="text-muted-foreground text-sm">
                  Arrangøren kan se dit svar nu. Du kan trygt lukke siden.
                </p>
              </CardContent>
            </Card>
          </motion.div>
        ) : (
          <motion.div
            key="form"
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
          >
            <Card>
              <CardContent className="space-y-5 pt-6">
                <h2 className="font-serif text-xl tracking-tight">Giv besked om du kommer</h2>

                <form onSubmit={onSubmit} className="space-y-5">
                  <Field label="Dit navn" htmlFor="name">
                    <Input
                      id="name"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      placeholder="Anne Andersen"
                      maxLength={200}
                    />
                  </Field>

                  <div className="space-y-2">
                    <div className="text-sm font-medium">Kommer du?</div>
                    <div
                      className={cn(
                        'grid gap-2',
                        event.allowMaybe ? 'grid-cols-3' : 'grid-cols-2',
                      )}
                    >
                      {statusChoices.map((c) => {
                        const selected = status === c.value;
                        return (
                          <button
                            key={c.value}
                            type="button"
                            data-state={selected ? 'on' : 'off'}
                            onClick={() => setStatus(c.value)}
                            className={cn(
                              'border-input bg-background relative rounded-md border px-3 py-3 text-sm font-medium transition-all',
                              'hover:bg-accent/50',
                              selected &&
                                'bg-primary text-primary-foreground border-primary hover:bg-primary',
                            )}
                          >
                            {c.label}
                          </button>
                        );
                      })}
                    </div>
                  </div>

                  {event.contactRequirement === 'Email' && (
                    <Field
                      label="Email"
                      htmlFor="email"
                      hint="Arrangøren bruger den hvis der opstår ændringer."
                    >
                      <Input
                        id="email"
                        name="email"
                        type="email"
                        required
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        placeholder="dig@example.dk"
                        maxLength={200}
                        autoComplete="email"
                      />
                    </Field>
                  )}

                  {event.contactRequirement === 'Phone' && (
                    <Field
                      label="Telefonnummer"
                      htmlFor="phone"
                      hint="Arrangøren bruger det hvis der opstår ændringer."
                    >
                      <Input
                        id="phone"
                        type="tel"
                        required
                        value={phone}
                        onChange={(e) => setPhone(e.target.value)}
                        placeholder="+45 12 34 56 78"
                        maxLength={50}
                      />
                    </Field>
                  )}

                  <Field
                    label="Kommentar (valgfri)"
                    htmlFor="comment"
                    hint="Allergier, +1, andre noter til arrangøren."
                  >
                    <Textarea
                      id="comment"
                      value={comment}
                      onChange={(e) => setComment(e.target.value)}
                      maxLength={2000}
                      rows={3}
                    />
                  </Field>

                  {error && <p className="text-destructive text-sm">{error}</p>}

                  <div className="flex justify-center pt-1">
                    <Button type="submit" disabled={submitting} size="lg">
                      {submitting ? 'Sender…' : 'Send tilbagemelding'}
                    </Button>
                  </div>
                </form>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
}
