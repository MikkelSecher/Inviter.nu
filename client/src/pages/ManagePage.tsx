import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { AnimatePresence, motion } from 'motion/react';
import { toast } from 'sonner';
import { Calendar, CalendarClock, Copy, Mail, MapPin, Pencil, Phone, Trash2, Users, X } from 'lucide-react';
import { api, ApiError } from '../api/client';
import type { ContactRequirement, EventAdmin, RsvpStatus } from '../api/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
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
import { Field } from '@/components/Field';
import { DateTimePicker } from '@/components/DateTimePicker';
import { StatusBadge, statusLabel } from '@/components/StatusBadge';
import { cn } from '@/lib/utils';
import { formatEventTime, fromDatetimeLocalValue, toDatetimeLocalValue } from '../lib/format';
import { rememberEvent, updateRememberedTitle } from '../lib/myEvents';

export function ManagePage() {
  const { token = '' } = useParams();
  const [event, setEvent] = useState<EventAdmin | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);
  const [pendingRemoval, setPendingRemoval] = useState<{ id: string; name: string } | null>(null);

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
      setLoadError(
        err instanceof ApiError && err.status === 404
          ? 'Vi kunne ikke finde dette event. Admin-linket er måske forkert.'
          : 'Kunne ikke hente event.',
      );
    }
  }, [token]);

  useEffect(() => {
    load();
  }, [load]);

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
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  const inviteUrl = `${window.location.origin}/invite/${event.inviteToken}`;
  const grouped: Record<RsvpStatus, typeof event.rsvps> = {
    Yes: event.rsvps.filter((r) => r.status === 'Yes'),
    Maybe: event.rsvps.filter((r) => r.status === 'Maybe'),
    No: event.rsvps.filter((r) => r.status === 'No'),
  };
  const deadlinePassed = event.rsvpDeadline ? new Date() > new Date(event.rsvpDeadline) : false;

  async function copyInvite() {
    try {
      await navigator.clipboard.writeText(inviteUrl);
      toast.success('Invite-link kopieret');
    } catch {
      toast.error('Kunne ikke kopiere');
    }
  }

  async function confirmRemoveGuest() {
    if (!pendingRemoval) return;
    const id = pendingRemoval.id;
    setPendingRemoval(null);
    try {
      await api.deleteRsvp(token, id);
      await load();
      toast.success('Gæst fjernet');
    } catch {
      toast.error('Kunne ikke fjerne gæsten');
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      className="space-y-8"
    >
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-2">
          <h1
            className="font-serif text-3xl tracking-tight sm:text-4xl"
            style={{ fontVariationSettings: '"opsz" 144' }}
          >
            {event.title}
          </h1>
          <div className="text-muted-foreground flex items-center gap-2 text-sm">
            <Calendar className="size-4" />
            {formatEventTime(event.startsAt)}
          </div>
          {event.location && (
            <div className="text-muted-foreground flex items-center gap-2 text-sm">
              <MapPin className="size-4" />
              <a
                href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(event.location)}`}
                target="_blank"
                rel="noreferrer"
                className="hover:text-foreground hover:underline"
              >
                {event.location}
              </a>
            </div>
          )}
          {event.rsvpDeadline && (
            <div
              className={cn(
                'flex items-center gap-2 text-sm',
                deadlinePassed ? 'text-destructive' : 'text-muted-foreground',
              )}
            >
              <CalendarClock className="size-4" />
              {deadlinePassed ? 'Tilmelding lukkede' : 'Tilmelding inden'}{' '}
              {formatEventTime(event.rsvpDeadline)}
            </div>
          )}
        </div>
        <Button variant="secondary" size="sm" onClick={() => setEditing((v) => !v)}>
          <Pencil className="mr-1 size-4" /> {editing ? 'Annullér' : 'Redigér'}
        </Button>
      </div>

      <AnimatePresence initial={false}>
        {editing && (
          <motion.div
            key="edit"
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.22, ease: 'easeOut' }}
            style={{ overflow: 'hidden' }}
          >
            <EditForm
              event={event}
              adminToken={token}
              hasExistingMaybe={grouped.Maybe.length > 0}
              onCancel={() => setEditing(false)}
              onSaved={async () => {
                setEditing(false);
                await load();
                toast.success('Eventet er opdateret');
              }}
            />
          </motion.div>
        )}
      </AnimatePresence>

      {!editing && event.description && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-sm whitespace-pre-wrap">{event.description}</p>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="flex flex-wrap items-center justify-between gap-3 pt-6">
          <div className="min-w-0">
            <div className="text-muted-foreground text-xs tracking-wide uppercase">
              Invite-link
            </div>
            <div className="mt-1 truncate text-sm" title={inviteUrl}>
              {inviteUrl}
            </div>
          </div>
          <Button variant="secondary" size="sm" onClick={copyInvite}>
            <Copy className="mr-1 size-4" /> Kopier
          </Button>
        </CardContent>
      </Card>

      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <Users className="text-muted-foreground size-4" />
          <h2 className="font-serif text-xl tracking-tight">
            Gæsteliste{' '}
            <span className="text-muted-foreground text-sm font-normal">
              ({event.rsvps.length})
            </span>
          </h2>
        </div>

        {event.rsvps.length === 0 ? (
          <Card>
            <CardContent className="text-muted-foreground pt-6 text-sm">
              Ingen har svaret endnu. Del invite-linket for at få tilbagemeldinger.
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            {(['Yes', 'Maybe', 'No'] as RsvpStatus[]).map((status) => {
              const list = grouped[status];
              if (list.length === 0) return null;
              return (
                <Card key={status}>
                  <CardContent className="space-y-3 pt-6">
                    <div className="flex items-center gap-2">
                      <StatusBadge status={status} />
                      <span className="text-muted-foreground text-sm">
                        {list.length} {statusLabel(status).toLowerCase()}
                      </span>
                    </div>
                    <ul className="divide-border divide-y">
                      <AnimatePresence initial={false}>
                        {list.map((r) => (
                          <motion.li
                            key={r.id}
                            layout
                            initial={{ opacity: 0, y: 6 }}
                            animate={{ opacity: 1, y: 0 }}
                            exit={{ opacity: 0, x: -12, transition: { duration: 0.18 } }}
                            transition={{ duration: 0.22, ease: 'easeOut' }}
                            className="flex items-start justify-between gap-3 py-3 first:pt-0 last:pb-0"
                          >
                            <div className="min-w-0 space-y-1">
                              <div className="text-sm font-medium">{r.guestName}</div>
                              {r.comment && (
                                <div className="text-muted-foreground text-sm">{r.comment}</div>
                              )}
                              <ContactLine
                                requirement={event.contactRequirement}
                                email={r.email}
                                phone={r.phone}
                              />
                            </div>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="text-muted-foreground hover:text-destructive size-8"
                              onClick={() =>
                                setPendingRemoval({ id: r.id, name: r.guestName })
                              }
                              aria-label={`Fjern ${r.guestName}`}
                            >
                              <Trash2 className="size-4" />
                            </Button>
                          </motion.li>
                        ))}
                      </AnimatePresence>
                    </ul>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      <AlertDialog
        open={!!pendingRemoval}
        onOpenChange={(open) => !open && setPendingRemoval(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Fjern gæst?</AlertDialogTitle>
            <AlertDialogDescription>
              {pendingRemoval
                ? `${pendingRemoval.name} fjernes fra gæstelisten. De kan stadig svare igen via invite-linket.`
                : ''}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annullér</AlertDialogCancel>
            <AlertDialogAction onClick={confirmRemoveGuest}>Fjern</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </motion.div>
  );
}

function ContactLine({
  requirement,
  email,
  phone,
}: {
  requirement: ContactRequirement;
  email: string | null;
  phone: string | null;
}) {
  if (requirement === 'Email' && email) {
    return (
      <a
        href={`mailto:${email}`}
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1.5 text-xs"
      >
        <Mail className="size-3.5" />
        {email}
      </a>
    );
  }
  if (requirement === 'Phone' && phone) {
    return (
      <a
        href={`tel:${phone}`}
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1.5 text-xs"
      >
        <Phone className="size-3.5" />
        {phone}
      </a>
    );
  }
  return null;
}

function EditForm({
  event,
  onCancel,
  onSaved,
  adminToken,
  hasExistingMaybe,
}: {
  event: EventAdmin;
  onCancel: () => void;
  onSaved: () => void;
  adminToken: string;
  hasExistingMaybe: boolean;
}) {
  const [title, setTitle] = useState(event.title);
  const [description, setDescription] = useState(event.description);
  const [location, setLocation] = useState(event.location);
  const [startsAt, setStartsAt] = useState(toDatetimeLocalValue(event.startsAt));
  const [allowMaybe, setAllowMaybe] = useState(event.allowMaybe);
  const [rsvpDeadline, setRsvpDeadline] = useState(
    event.rsvpDeadline ? toDatetimeLocalValue(event.rsvpDeadline) : '',
  );
  const [showDeadline, setShowDeadline] = useState(Boolean(event.rsvpDeadline));
  const [contactRequirement, setContactRequirement] = useState<ContactRequirement>(
    event.contactRequirement,
  );
  const [organizerName, setOrganizerName] = useState(event.organizerName ?? '');
  const [organizerEmail, setOrganizerEmail] = useState(event.organizerEmail ?? '');
  const [saving, setSaving] = useState(false);
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
    setSaving(true);
    try {
      await api.updateEvent(adminToken, {
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
      <CardContent className="pt-6">
        <form onSubmit={onSubmit} className="space-y-5">
          <Field label="Titel" htmlFor="edit-title">
            <Input
              id="edit-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
            />
          </Field>
          <Field label="Beskrivelse" htmlFor="edit-description">
            <Textarea
              id="edit-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={4000}
              rows={4}
            />
          </Field>
          <Field label="Sted (valgfri)" htmlFor="edit-location">
            <Input
              id="edit-location"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="Vesterbrogade 12, 1620 København"
              maxLength={500}
            />
          </Field>
          <Field label="Hvornår" htmlFor="edit-startsAt">
            <DateTimePicker
              id="edit-startsAt"
              value={startsAt}
              onChange={setStartsAt}
            />
          </Field>
          {showDeadline ? (
            <Field label="Senest tilmelding" htmlFor="edit-rsvpDeadline">
              <div className="flex items-center gap-2">
                <div className="flex-1">
                  <DateTimePicker
                    id="edit-rsvpDeadline"
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
              <Label htmlFor="edit-allowMaybe" className="text-sm font-medium">
                Tillad "måske"-svar
              </Label>
              <p className="text-muted-foreground text-xs">
                {!allowMaybe && hasExistingMaybe
                  ? 'Eksisterende "måske"-svar bevares — kun nye svar blokeres.'
                  : 'Når slået fra, kan gæster kun vælge "kommer" eller "kommer ikke".'}
              </p>
            </div>
            <Switch
              id="edit-allowMaybe"
              checked={allowMaybe}
              onCheckedChange={setAllowMaybe}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-medium">Kontaktoplysninger fra gæsten</Label>
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
                <div key={opt.v} className="relative">
                  <RadioGroupItem
                    id={`edit-contact-${opt.v}`}
                    value={opt.v}
                    className="peer sr-only"
                  />
                  <Label
                    htmlFor={`edit-contact-${opt.v}`}
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
                Hvis du oplyser din email, sender vi notifikationer om svar — og du kan altid bruge
                den til at få admin-linket sendt igen.
              </p>
            </div>
            <Field label="Dit navn" htmlFor="edit-organizerName">
              <Input
                id="edit-organizerName"
                value={organizerName}
                onChange={(e) => setOrganizerName(e.target.value)}
                placeholder="Anne Andersen"
                maxLength={200}
              />
            </Field>
            <Field label="Din email" htmlFor="edit-organizerEmail">
              <Input
                id="edit-organizerEmail"
                type="email"
                value={organizerEmail}
                onChange={(e) => setOrganizerEmail(e.target.value)}
                placeholder="dig@example.dk"
                maxLength={320}
              />
            </Field>
          </div>

          {error && <p className="text-destructive text-sm">{error}</p>}
          <div className="flex justify-center gap-2 pt-1">
            <Button type="submit" disabled={saving}>
              {saving ? 'Gemmer…' : 'Gem'}
            </Button>
            <Button type="button" variant="secondary" onClick={onCancel}>
              Annullér
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
