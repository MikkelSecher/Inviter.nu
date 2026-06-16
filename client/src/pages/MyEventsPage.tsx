import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { AnimatePresence, motion } from 'motion/react';
import { ArrowRight, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { forgetEvent, listMyEvents, type MyEvent } from '../lib/myEvents';

export function MyEventsPage() {
  const [events, setEvents] = useState<MyEvent[]>([]);
  const [pendingRemoval, setPendingRemoval] = useState<MyEvent | null>(null);

  useEffect(() => {
    setEvents(listMyEvents());
  }, []);

  function confirmRemoval() {
    if (!pendingRemoval) return;
    forgetEvent(pendingRemoval.id);
    setEvents(listMyEvents());
    setPendingRemoval(null);
  }

  if (events.length === 0) {
    return (
      <div className="space-y-8">
        <div className="border-b pb-6">
          <h1 className="font-serif text-5xl leading-none tracking-tight sm:text-6xl">Mine events</h1>
          <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-6">
            Du har ikke oprettet nogen events i denne browser endnu.
          </p>
        </div>
        <Card>
          <CardContent className="flex flex-col items-start gap-4 pt-6">
            <p className="text-sm">Klar til at samle gæster?</p>
            <Button asChild>
              <Link to="/">
                Opret et event <ArrowRight className="ml-1 size-4" />
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="border-b pb-6">
        <h1 className="font-serif text-5xl leading-none tracking-tight sm:text-6xl">Mine events</h1>
        <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-6">
          Gemt lokalt i denne browser. Mister du browseren, mister du listen — ikke selve eventet.
        </p>
      </div>

      <ul className="grid gap-3 lg:grid-cols-2">
        <AnimatePresence initial={false}>
          {events.map((ev) => (
            <motion.li
              key={ev.id}
              layout
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, x: -16, transition: { duration: 0.18 } }}
              transition={{ duration: 0.22, ease: 'easeOut' }}
            >
              <Card className="transition-shadow hover:shadow-md">
                <CardContent className="flex flex-wrap items-center justify-between gap-3 py-4">
                  <div className="min-w-0">
                    <div className="truncate font-medium">{ev.title}</div>
                    <div className="text-muted-foreground text-xs">
                      Oprettet {new Date(ev.createdAt).toLocaleDateString('da-DK')}
                    </div>
                  </div>
                  <div className="flex items-center gap-1">
                    <Button asChild variant="secondary" size="sm">
                      <Link to={`/manage/${ev.adminToken}`}>
                        Åbn <ArrowRight className="ml-1 size-3.5" />
                      </Link>
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => setPendingRemoval(ev)}
                      aria-label="Fjern fra liste"
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>

      <AlertDialog
        open={!!pendingRemoval}
        onOpenChange={(open) => !open && setPendingRemoval(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Fjern fra liste?</AlertDialogTitle>
            <AlertDialogDescription>
              Eventet slettes ikke — du mister bare hurtig adgang fra denne browser. Du kan stadig
              komme tilbage hvis du har admin-linket.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annullér</AlertDialogCancel>
            <AlertDialogAction onClick={confirmRemoval}>Fjern</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
