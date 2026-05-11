import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { EventAdmin, RsvpStatus } from '../api/types';
import { Button, Card, Field, TextArea, TextInput } from '../components/ui';
import { StatusBadge, statusLabel } from '../components/StatusBadge';
import { formatEventTime, fromDatetimeLocalValue, toDatetimeLocalValue } from '../lib/format';
import { rememberEvent, updateRememberedTitle } from '../lib/myEvents';

export function ManagePage() {
  const { token = '' } = useParams();
  const [event, setEvent] = useState<EventAdmin | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);
  const [copied, setCopied] = useState(false);

  const load = useCallback(async () => {
    setLoadError(null);
    try {
      const ev = await api.getManage(token);
      setEvent(ev);
      rememberEvent({
        id: ev.id,
        title: ev.title,
        adminToken: ev.adminToken,
        createdAt: ev.createdAt,
      });
    } catch (err) {
      setLoadError(err instanceof ApiError && err.status === 404
        ? 'Vi kunne ikke finde dette event. Admin-linket er måske forkert.'
        : 'Kunne ikke hente event.');
    }
  }, [token]);

  useEffect(() => { load(); }, [load]);

  if (loadError) return <Card><p className="text-sm text-slate-700">{loadError}</p></Card>;
  if (!event) return <p className="text-sm text-slate-500">Henter event…</p>;

  const inviteUrl = `${window.location.origin}/invite/${event.inviteToken}`;
  const grouped: Record<RsvpStatus, typeof event.rsvps> = {
    Yes: event.rsvps.filter((r) => r.status === 'Yes'),
    Maybe: event.rsvps.filter((r) => r.status === 'Maybe'),
    No: event.rsvps.filter((r) => r.status === 'No'),
  };

  async function copyInvite() {
    await navigator.clipboard.writeText(inviteUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  async function removeGuest(rsvpId: string) {
    if (!event) return;
    if (!confirm('Fjern denne gæst?')) return;
    try {
      await api.deleteRsvp(token, rsvpId);
      await load();
    } catch {
      alert('Kunne ikke fjerne gæsten.');
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{event.title}</h1>
          <p className="mt-1 text-sm text-slate-600">{formatEventTime(event.startsAt)}</p>
        </div>
        <Button variant="secondary" onClick={() => setEditing((v) => !v)}>
          {editing ? 'Annullér' : 'Redigér event'}
        </Button>
      </div>

      {editing ? (
        <EditForm
          event={event}
          onCancel={() => setEditing(false)}
          onSaved={async () => { setEditing(false); await load(); }}
          adminToken={token}
        />
      ) : (
        event.description && (
          <Card>
            <p className="whitespace-pre-wrap text-sm text-slate-700">{event.description}</p>
          </Card>
        )
      )}

      <Card>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-semibold text-slate-700">Invite-link</h2>
            <p className="mt-1 break-all text-sm text-slate-600">{inviteUrl}</p>
          </div>
          <Button variant="secondary" onClick={copyInvite}>
            {copied ? 'Kopieret!' : 'Kopier link'}
          </Button>
        </div>
      </Card>

      <div>
        <h2 className="mb-3 text-lg font-semibold">
          Gæsteliste <span className="text-sm font-normal text-slate-500">({event.rsvps.length})</span>
        </h2>

        {event.rsvps.length === 0 ? (
          <Card>
            <p className="text-sm text-slate-600">Ingen har svaret endnu. Del invite-linket for at få tilbagemeldinger.</p>
          </Card>
        ) : (
          <div className="space-y-4">
            {(['Yes', 'Maybe', 'No'] as RsvpStatus[]).map((status) => {
              const list = grouped[status];
              if (list.length === 0) return null;
              return (
                <Card key={status}>
                  <div className="mb-3 flex items-center gap-2">
                    <StatusBadge status={status} />
                    <span className="text-sm text-slate-500">{list.length} {statusLabel(status).toLowerCase()}</span>
                  </div>
                  <ul className="divide-y divide-slate-100">
                    {list.map((r) => (
                      <li key={r.id} className="flex items-start justify-between gap-3 py-3 first:pt-0 last:pb-0">
                        <div>
                          <div className="text-sm font-medium text-slate-900">{r.guestName}</div>
                          {r.comment && (
                            <div className="mt-1 text-sm text-slate-600">{r.comment}</div>
                          )}
                        </div>
                        <button
                          onClick={() => removeGuest(r.id)}
                          className="text-xs text-slate-400 hover:text-rose-600"
                          title="Fjern"
                        >
                          Fjern
                        </button>
                      </li>
                    ))}
                  </ul>
                </Card>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

function EditForm({
  event,
  onCancel,
  onSaved,
  adminToken,
}: {
  event: EventAdmin;
  onCancel: () => void;
  onSaved: () => void;
  adminToken: string;
}) {
  const [title, setTitle] = useState(event.title);
  const [description, setDescription] = useState(event.description);
  const [startsAt, setStartsAt] = useState(toDatetimeLocalValue(event.startsAt));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (!title.trim()) { setError('Titel er påkrævet.'); return; }
    if (!startsAt) { setError('Dato og tid er påkrævet.'); return; }
    setSaving(true);
    try {
      await api.updateEvent(adminToken, {
        title: title.trim(),
        description: description.trim(),
        startsAt: fromDatetimeLocalValue(startsAt),
      });
      updateRememberedTitle(event.id, title.trim());
      onSaved();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Kunne ikke gemme.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card>
      <form onSubmit={onSubmit} className="space-y-4">
        <Field label="Titel">
          <TextInput value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} />
        </Field>
        <Field label="Beskrivelse">
          <TextArea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={4000} />
        </Field>
        <Field label="Hvornår">
          <TextInput type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} />
        </Field>
        {error && <p className="text-sm text-rose-600">{error}</p>}
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Gemmer…' : 'Gem'}</Button>
          <Button type="button" variant="secondary" onClick={onCancel}>Annullér</Button>
        </div>
      </form>
    </Card>
  );
}
