import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import { Button, Card, Field, TextArea, TextInput } from '../components/ui';
import { fromDatetimeLocalValue } from '../lib/format';
import { rememberEvent } from '../lib/myEvents';

export function CreateEventPage() {
  const navigate = useNavigate();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [startsAt, setStartsAt] = useState('');
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
    setSubmitting(true);
    try {
      const created = await api.createEvent({
        title: title.trim(),
        description: description.trim(),
        startsAt: fromDatetimeLocalValue(startsAt),
      });
      rememberEvent({
        id: created.id,
        title: created.title,
        adminToken: created.adminToken,
        createdAt: created.createdAt,
      });
      navigate(`/manage/${created.adminToken}`, { replace: true });
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Kunne ikke oprette event.';
      setError(msg);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Opret et event</h1>
        <p className="mt-1 text-sm text-slate-600">
          Udfyld det vigtigste — du får et delbart invite-link og en hemmelig admin-URL bagefter.
        </p>
      </div>

      <Card>
        <form onSubmit={onSubmit} className="space-y-4">
          <Field label="Titel">
            <TextInput
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Sommerfest"
              maxLength={200}
              autoFocus
            />
          </Field>

          <Field label="Beskrivelse" hint="Praktisk info, dresscode, beskeder til gæsterne.">
            <TextArea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Vi mødes i haven, tag gerne en flaske vin med..."
              maxLength={4000}
            />
          </Field>

          <Field label="Hvornår">
            <TextInput
              type="datetime-local"
              value={startsAt}
              onChange={(e) => setStartsAt(e.target.value)}
            />
          </Field>

          {error && <p className="text-sm text-rose-600">{error}</p>}

          <div className="pt-2">
            <Button type="submit" disabled={submitting}>
              {submitting ? 'Opretter…' : 'Opret event'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
