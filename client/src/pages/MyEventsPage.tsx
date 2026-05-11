import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Card } from '../components/ui';
import { forgetEvent, listMyEvents, type MyEvent } from '../lib/myEvents';

export function MyEventsPage() {
  const [events, setEvents] = useState<MyEvent[]>([]);

  useEffect(() => { setEvents(listMyEvents()); }, []);

  function remove(id: string) {
    if (!confirm('Fjern dette event fra din lokale liste? (Eventet slettes ikke — du mister bare hurtig adgang fra denne browser.)')) return;
    forgetEvent(id);
    setEvents(listMyEvents());
  }

  if (events.length === 0) {
    return (
      <Card>
        <h1 className="text-lg font-semibold">Mine events</h1>
        <p className="mt-2 text-sm text-slate-600">
          Du har ikke oprettet nogen events i denne browser endnu.
        </p>
        <div className="mt-4">
          <Link to="/">
            <Button>Opret et event</Button>
          </Link>
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight">Mine events</h1>
      <p className="text-sm text-slate-600">
        Gemt lokalt i din browser. Tæller ikke som login — mister du adgang til denne browser, mister du listen.
      </p>

      <div className="space-y-3">
        {events.map((ev) => (
          <Card key={ev.id}>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <div className="font-medium text-slate-900">{ev.title}</div>
                <div className="text-xs text-slate-500">
                  Oprettet {new Date(ev.createdAt).toLocaleDateString('da-DK')}
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Link to={`/manage/${ev.adminToken}`}>
                  <Button variant="secondary">Åbn</Button>
                </Link>
                <button
                  onClick={() => remove(ev.id)}
                  className="text-xs text-slate-400 hover:text-rose-600"
                >
                  Fjern
                </button>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
