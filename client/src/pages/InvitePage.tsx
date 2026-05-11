import { useEffect, useState, type FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { EventPublic, RsvpStatus } from '../api/types';
import { Button, Card, Field, TextArea, TextInput } from '../components/ui';
import { formatEventTime } from '../lib/format';

const statusChoices: { value: RsvpStatus; label: string }[] = [
  { value: 'Yes', label: 'Jeg kommer' },
  { value: 'Maybe', label: 'Måske' },
  { value: 'No', label: 'Jeg kommer ikke' },
];

export function InvitePage() {
  const { token = '' } = useParams();
  const [event, setEvent] = useState<EventPublic | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [status, setStatus] = useState<RsvpStatus | null>(null);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoadError(null);
    api.getInvite(token)
      .then((ev) => { if (!cancelled) setEvent(ev); })
      .catch((err) => {
        if (cancelled) return;
        setLoadError(err instanceof ApiError && err.status === 404
          ? 'Vi kunne ikke finde dette event. Tjek at linket er korrekt.'
          : 'Kunne ikke hente event.');
      });
    return () => { cancelled = true; };
  }, [token]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim()) { setError('Skriv venligst dit navn.'); return; }
    if (!status) { setError('Vælg om du kommer.'); return; }
    setSubmitting(true);
    try {
      await api.submitRsvp(token, {
        guestName: name.trim(),
        status,
        comment: comment.trim() ? comment.trim() : null,
      });
      setSubmitted(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kunne ikke sende tilbagemelding.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loadError) {
    return <Card><p className="text-sm text-slate-700">{loadError}</p></Card>;
  }
  if (!event) {
    return <p className="text-sm text-slate-500">Henter event…</p>;
  }

  return (
    <div className="space-y-6">
      <Card>
        <h1 className="text-2xl font-semibold tracking-tight">{event.title}</h1>
        <p className="mt-1 text-sm text-slate-600">{formatEventTime(event.startsAt)}</p>
        {event.description && (
          <p className="mt-4 whitespace-pre-wrap text-sm text-slate-700">{event.description}</p>
        )}
      </Card>

      {submitted ? (
        <Card>
          <h2 className="text-lg font-semibold">Tak for din tilbagemelding!</h2>
          <p className="mt-2 text-sm text-slate-600">
            Arrangøren kan se dit svar nu. Du kan lukke siden.
          </p>
        </Card>
      ) : (
        <Card>
          <h2 className="text-lg font-semibold">Giv besked om du kommer</h2>
          <form onSubmit={onSubmit} className="mt-4 space-y-4">
            <Field label="Dit navn">
              <TextInput
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Anne Andersen"
                maxLength={200}
              />
            </Field>

            <div>
              <div className="mb-2 text-sm font-medium text-slate-700">Kommer du?</div>
              <div className="flex flex-wrap gap-2">
                {statusChoices.map((c) => {
                  const selected = status === c.value;
                  return (
                    <button
                      key={c.value}
                      type="button"
                      onClick={() => setStatus(c.value)}
                      className={`rounded-md px-3 py-2 text-sm font-medium ring-1 ring-inset transition-colors ${
                        selected
                          ? 'bg-slate-900 text-white ring-slate-900'
                          : 'bg-white text-slate-700 ring-slate-300 hover:bg-slate-50'
                      }`}
                    >
                      {c.label}
                    </button>
                  );
                })}
              </div>
            </div>

            <Field label="Kommentar (valgfri)" hint="Allergier, +1, andre noter til arrangøren.">
              <TextArea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                maxLength={2000}
              />
            </Field>

            {error && <p className="text-sm text-rose-600">{error}</p>}

            <div className="pt-2">
              <Button type="submit" disabled={submitting}>
                {submitting ? 'Sender…' : 'Send tilbagemelding'}
              </Button>
            </div>
          </form>
        </Card>
      )}
    </div>
  );
}
