import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { AnimatePresence, motion } from 'motion/react';
import { toast } from 'sonner';
import {
  Calendar,
  CalendarClock,
  Copy,
  Link2,
  Mail,
  MapPin,
  Pencil,
  Phone,
  Plus,
  Send,
  Trash2,
  Unlink,
  UserPlus,
  Users,
  X,
} from 'lucide-react';
import { api, ApiError } from '../api/client';
import type {
  AddInviteeEntry,
  ContactRequirement,
  EventAdmin,
  Invitee,
  Rsvp,
  RsvpStatus,
} from '../api/types';
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Field } from '@/components/Field';
import { DateTimePicker } from '@/components/DateTimePicker';
import { ImageDropZone } from '@/components/ImageDropZone';
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
  const [linkingRsvp, setLinkingRsvp] = useState<Rsvp | null>(null);
  const [linkInvitees, setLinkInvitees] = useState<Invitee[] | null>(null);
  const [selectedLinkInviteeId, setSelectedLinkInviteeId] = useState('');
  const [linkBusy, setLinkBusy] = useState(false);
  const [linkLoadError, setLinkLoadError] = useState<string | null>(null);

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
    if (await copyTextToClipboard(inviteUrl)) {
      toast.success('Invite-link kopieret');
    } else {
      toast.error('Kunne ikke kopiere invite-linket');
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

  async function openLinkDialog(rsvp: Rsvp) {
    setLinkingRsvp(rsvp);
    setSelectedLinkInviteeId(rsvp.inviteeId ?? '');
    setLinkLoadError(null);
    setLinkInvitees(null);
    try {
      setLinkInvitees(await api.listInvitees(token));
    } catch (err) {
      setLinkLoadError(err instanceof ApiError ? err.message : 'Kunne ikke hente gæstelisten.');
    }
  }

  async function saveRsvpLink() {
    if (!linkingRsvp) return;
    setLinkBusy(true);
    try {
      await api.linkRsvpInvitee(token, linkingRsvp.id, selectedLinkInviteeId || null);
      setLinkingRsvp(null);
      await load();
      toast.success(selectedLinkInviteeId ? 'Svar knyttet til gæst' : 'Kobling fjernet');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Kunne ikke gemme koblingen.');
    } finally {
      setLinkBusy(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      className="space-y-8"
    >
      {event.imageUrl && (
        <img
          src={event.imageUrl}
          alt=""
          className="aspect-[16/7] w-full rounded-lg object-cover shadow-sm ring-1 ring-border"
        />
      )}

      <div className="flex flex-wrap items-start justify-between gap-4 border-b pb-6">
        <div className="space-y-2">
          <h1
            className="font-serif text-5xl leading-none tracking-tight sm:text-6xl"
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

      <div className="grid gap-3 sm:grid-cols-3">
        <SummaryTile label="Kommer" value={grouped.Yes.length} />
        <SummaryTile label="Måske" value={grouped.Maybe.length} />
        <SummaryTile label="Kan ikke" value={grouped.No.length} />
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
              onImageChanged={load}
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
            Svar{' '}
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
                              {r.inviteeId ? (
                                <div className="text-muted-foreground inline-flex items-center gap-1.5 text-xs">
                                  <Link2 className="size-3.5" />
                                  Knyttet til {r.inviteeName || r.inviteeEmail || 'gæst på listen'}
                                </div>
                              ) : (
                                <div className="text-muted-foreground inline-flex items-center gap-1.5 text-xs">
                                  <Unlink className="size-3.5" />
                                  Ikke knyttet til gæstelisten
                                </div>
                              )}
                            </div>
                            <div className="flex items-center gap-1">
                              <Button
                                variant="ghost"
                                size="icon"
                                className="text-muted-foreground hover:text-foreground size-8"
                                onClick={() => openLinkDialog(r)}
                                aria-label={`${r.inviteeId ? 'Skift' : 'Knyt'} gæst for ${r.guestName}`}
                              >
                                <Link2 className="size-4" />
                              </Button>
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
                            </div>
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

      <InviteeSection
        key={event.rsvps.map((r) => `${r.id}:${r.inviteeId ?? ''}`).join('|')}
        adminToken={token}
        eventTitle={event.title}
        eventInviteToken={event.inviteToken}
      />

      <Dialog
        open={!!linkingRsvp}
        onOpenChange={(open) => {
          if (!open) setLinkingRsvp(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Knyt svar til gæst</DialogTitle>
            <DialogDescription>
              Vælg den gæst på listen, som dette svar hører til.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-3">
            <div className="text-sm font-medium">{linkingRsvp?.guestName}</div>
            {linkLoadError ? (
              <p className="text-destructive text-sm">{linkLoadError}</p>
            ) : (
              <select
                value={selectedLinkInviteeId}
                onChange={(e) => setSelectedLinkInviteeId(e.target.value)}
                disabled={!linkInvitees || linkBusy}
                className="border-input bg-background ring-offset-background focus-visible:ring-ring w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:outline-none"
              >
                <option value="">Ingen kobling</option>
                {(linkInvitees ?? []).map((invitee) => (
                  <option key={invitee.id} value={invitee.id}>
                    {inviteeLabel(invitee)}
                  </option>
                ))}
              </select>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => setLinkingRsvp(null)}>
              Annullér
            </Button>
            <Button type="button" onClick={saveRsvpLink} disabled={linkBusy || !!linkLoadError}>
              {linkBusy ? 'Gemmer...' : 'Gem kobling'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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

function SummaryTile({ label, value }: { label: string; value: number }) {
  return (
    <Card size="sm">
      <CardContent className="space-y-1 pt-3">
        <div className="text-muted-foreground text-xs font-semibold tracking-wide uppercase">
          {label}
        </div>
        <div className="font-serif text-3xl leading-none tracking-tight">{value}</div>
      </CardContent>
    </Card>
  );
}

function inviteeLabel(invitee: Invitee) {
  return invitee.name || invitee.email || 'Gæst uden navn';
}

function personalInviteUrl(inviteToken: string, invitee: Invitee) {
  return `${window.location.origin}/invite/${inviteToken}?g=${invitee.personalInviteToken}`;
}

async function copyTextToClipboard(text: string): Promise<boolean> {
  if (navigator.clipboard && window.isSecureContext) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // Fall back for browsers that expose the API but deny write access.
    }
  }

  const textarea = document.createElement('textarea');
  textarea.value = text;
  textarea.setAttribute('readonly', '');
  textarea.style.position = 'fixed';
  textarea.style.left = '-9999px';
  textarea.style.top = '0';
  document.body.appendChild(textarea);
  textarea.focus();
  textarea.select();

  try {
    return document.execCommand('copy');
  } catch {
    return false;
  } finally {
    document.body.removeChild(textarea);
  }
}

function EditForm({
  event,
  onCancel,
  onSaved,
  onImageChanged,
  adminToken,
  hasExistingMaybe,
}: {
  event: EventAdmin;
  onCancel: () => void;
  onSaved: () => void;
  onImageChanged: () => Promise<void>;
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
  const [imageBusy, setImageBusy] = useState(false);
  const [confirmImageRemoval, setConfirmImageRemoval] = useState(false);

  async function uploadImage(file: File) {
    setImageBusy(true);
    try {
      await api.uploadEventImage(adminToken, file);
      await onImageChanged();
      toast.success('Billede opdateret');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Kunne ikke uploade billedet.');
    } finally {
      setImageBusy(false);
    }
  }

  async function removeImage() {
    setConfirmImageRemoval(false);
    setImageBusy(true);
    try {
      await api.deleteEventImage(adminToken);
      await onImageChanged();
      toast.success('Billede fjernet');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Kunne ikke fjerne billedet.');
    } finally {
      setImageBusy(false);
    }
  }

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
    <>
    <Card>
      <CardContent className="pt-6">
        <form onSubmit={onSubmit} className="space-y-5">
          <Field
            label="Eventbillede (valgfri)"
            hint="Vises øverst på invitationssiden og i invitations-mailen."
          >
            <ImageDropZone
              imageUrl={event.imageUrl}
              onPick={uploadImage}
              onRemove={() => setConfirmImageRemoval(true)}
              busy={imageBusy}
            />
          </Field>

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

    <AlertDialog
      open={confirmImageRemoval}
      onOpenChange={(open) => !open && setConfirmImageRemoval(false)}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Fjern billede?</AlertDialogTitle>
          <AlertDialogDescription>
            Billedet fjernes fra invitationssiden og fremtidige invitations-mails.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Annullér</AlertDialogCancel>
          <AlertDialogAction onClick={removeImage}>Fjern</AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
    </>
  );
}

type InviteeDraft = { rowId: string; name: string; email: string };

function newDraft(seed: Partial<Omit<InviteeDraft, 'rowId'>> = {}): InviteeDraft {
  return {
    rowId:
      typeof crypto !== 'undefined' && 'randomUUID' in crypto
        ? crypto.randomUUID()
        : Math.random().toString(36).slice(2),
    name: seed.name ?? '',
    email: seed.email ?? '',
  };
}

function looksLikeEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function parseBulkPaste(
  text: string,
  field: 'name' | 'email',
): { name: string; email: string }[] {
  return text
    .split(/[\n,;]+/)
    .map((t) => t.trim())
    .filter(Boolean)
    .map((token) => {
      const angleMatch = /^(.+?)\s*<([^>]+)>$/.exec(token);
      if (angleMatch) {
        return { name: angleMatch[1].trim(), email: angleMatch[2].trim() };
      }
      if (looksLikeEmail(token) || (field === 'email' && token.includes('@'))) {
        return { name: '', email: token };
      }
      return { name: token, email: '' };
    });
}

function formatRelative(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('da-DK', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function InviteeSection({
  adminToken,
  eventTitle,
  eventInviteToken,
}: {
  adminToken: string;
  eventTitle: string;
  eventInviteToken: string;
}) {
  const [invitees, setInvitees] = useState<Invitee[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<InviteeDraft[]>(() => [newDraft()]);
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [sending, setSending] = useState(false);
  const [pendingRemoval, setPendingRemoval] = useState<{ id: string; label: string } | null>(null);
  const [editingInvitee, setEditingInvitee] = useState<{
    id: string;
    name: string;
    email: string;
  } | null>(null);
  const [editError, setEditError] = useState<string | null>(null);
  const [savingEdit, setSavingEdit] = useState(false);

  const load = useCallback(async () => {
    setLoadError(null);
    try {
      const list = await api.listInvitees(adminToken);
      setInvitees(list);
    } catch (err) {
      setLoadError(
        err instanceof ApiError ? err.message : 'Kunne ikke hente gæstelisten.',
      );
    }
  }, [adminToken]);

  useEffect(() => {
    load();
  }, [load]);

  const unsentCount = useMemo(
    () => (invitees ?? []).filter((i) => i.email && i.lastSentAt === null).length,
    [invitees],
  );

  function toggleSelected(id: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function updateDraft(rowId: string, field: 'name' | 'email', value: string) {
    setDrafts((prev) => prev.map((d) => (d.rowId === rowId ? { ...d, [field]: value } : d)));
  }

  function addRow() {
    setDrafts((prev) => [...prev, newDraft()]);
  }

  function removeRow(rowId: string) {
    setDrafts((prev) => {
      const next = prev.filter((d) => d.rowId !== rowId);
      return next.length === 0 ? [newDraft()] : next;
    });
  }

  function handlePaste(rowId: string, field: 'name' | 'email', e: React.ClipboardEvent<HTMLInputElement>) {
    const text = e.clipboardData.getData('text');
    if (!/[\n,;]/.test(text)) return;
    e.preventDefault();
    const parsed = parseBulkPaste(text, field);
    if (parsed.length === 0) return;
    setDrafts((prev) => {
      const idx = prev.findIndex((d) => d.rowId === rowId);
      if (idx === -1) return prev;
      const target = prev[idx];
      const targetMerged: InviteeDraft = {
        ...target,
        name: field === 'name' && parsed[0].name ? parsed[0].name : target.name || parsed[0].name,
        email: field === 'email' ? parsed[0].email : target.email,
      };
      if (field === 'name' && !target.email) {
        targetMerged.email = parsed[0].email;
      }
      const rest = parsed.slice(1).map((p) => newDraft({ name: p.name, email: p.email }));
      return [...prev.slice(0, idx), targetMerged, ...rest, ...prev.slice(idx + 1)];
    });
  }

  async function onAdd(e: FormEvent) {
    e.preventDefault();
    setAddError(null);
    const entries: AddInviteeEntry[] = drafts
      .map((d) => ({ name: d.name.trim() || null, email: d.email.trim() || null }))
      .filter((entry) => Boolean(entry.name || entry.email));
    if (entries.length === 0) {
      setAddError('Tilføj mindst ét navn eller én email-adresse.');
      return;
    }
    setAdding(true);
    try {
      const res = await api.addInvitees(adminToken, entries);
      if (res.added.length > 0) {
        setInvitees((prev) => [...(prev ?? []), ...res.added]);
        toast.success(
          res.added.length === 1
            ? '1 gæst tilføjet'
            : `${res.added.length} gæster tilføjet`,
        );
      }
      const skipped = res.skippedDuplicates.length + res.skippedInvalid.length;
      if (skipped > 0) {
        const parts: string[] = [];
        if (res.skippedDuplicates.length > 0)
          parts.push(`${res.skippedDuplicates.length} allerede tilføjet`);
        if (res.skippedInvalid.length > 0)
          parts.push(`${res.skippedInvalid.length} ugyldige`);
        toast.message(`Sprang ${skipped} linjer over`, { description: parts.join(' · ') });
      }
      if (res.added.length > 0 || skipped === entries.length) {
        setDrafts([newDraft()]);
      }
    } catch (err) {
      setAddError(err instanceof ApiError ? err.message : 'Kunne ikke tilføje gæster.');
    } finally {
      setAdding(false);
    }
  }

  async function confirmDelete() {
    if (!pendingRemoval) return;
    const { id } = pendingRemoval;
    setPendingRemoval(null);
    try {
      await api.deleteInvitee(adminToken, id);
      setInvitees((prev) => (prev ?? []).filter((i) => i.id !== id));
      setSelectedIds((prev) => {
        const next = new Set(prev);
        next.delete(id);
        return next;
      });
      toast.success('Gæst fjernet');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Kunne ikke fjerne gæst.');
    }
  }

  function startEdit(invitee: Invitee) {
    setEditError(null);
    setEditingInvitee({
      id: invitee.id,
      name: invitee.name ?? '',
      email: invitee.email ?? '',
    });
  }

  async function saveEdit() {
    if (!editingInvitee) return;
    const name = editingInvitee.name.trim();
    const email = editingInvitee.email.trim();
    if (!name && !email) {
      setEditError('Udfyld mindst navn eller email.');
      return;
    }
    setSavingEdit(true);
    setEditError(null);
    try {
      const updated = await api.updateInvitee(adminToken, editingInvitee.id, {
        name: name || null,
        email: email || null,
      });
      setInvitees((prev) =>
        (prev ?? []).map((i) =>
          i.id === updated.id ? { ...updated, rsvpStatus: i.rsvpStatus } : i,
        ),
      );
      setEditingInvitee(null);
      toast.success('Gæst opdateret');
    } catch (err) {
      setEditError(err instanceof ApiError ? err.message : 'Kunne ikke opdatere gæsten.');
    } finally {
      setSavingEdit(false);
    }
  }

  async function copyPersonalLink(invitee: Invitee) {
    if (await copyTextToClipboard(personalInviteUrl(eventInviteToken, invitee))) {
      toast.success('Personligt invite-link kopieret');
    } else {
      toast.error('Kunne ikke kopiere det personlige invite-link');
    }
  }

  async function onSend(filter: 'unsent' | 'selected') {
    if (sending) return;
    const inviteeIds = filter === 'selected' ? Array.from(selectedIds) : null;
    if (filter === 'selected' && inviteeIds!.length === 0) return;
    setSending(true);
    try {
      const res = await api.sendInvitations(adminToken, {
        inviteeIds,
        onlyUnsent: filter === 'unsent',
      });
      if (res.enqueued === 0) {
        toast.message('Ingen at sende til', {
          description:
            filter === 'unsent' ? 'Alle med email har fået invitation.' : 'Vælg mindst én gæst med email.',
        });
      } else {
        toast.success(
          res.enqueued === 1
            ? '1 invitation sendt'
            : `${res.enqueued} invitationer sendt`,
        );
        if (filter === 'selected') setSelectedIds(new Set());
        load();
      }
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Kunne ikke sende invitationer.');
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <UserPlus className="text-muted-foreground size-4" />
        <h2 className="font-serif text-xl tracking-tight">
          Invitationer{' '}
          <span className="text-muted-foreground text-sm font-normal">
            ({invitees?.length ?? 0})
          </span>
        </h2>
      </div>

      <Card>
        <CardContent className="space-y-4 pt-6">
          <form onSubmit={onAdd} className="space-y-3">
            <div className="space-y-1.5">
              <div className="flex items-baseline justify-between">
                <Label className="text-sm font-medium">Tilføj gæster</Label>
                <span className="text-muted-foreground text-xs">
                  Du kan også indsætte en liste — vi splitter på linjeskift og komma.
                </span>
              </div>
              <div className="hidden grid-cols-[1fr_1.4fr_auto] gap-2 px-1 sm:grid">
                <div className="text-muted-foreground text-xs">Navn (valgfri)</div>
                <div className="text-muted-foreground text-xs">Email (valgfri)</div>
                <div />
              </div>
              <div className="space-y-2">
                <AnimatePresence initial={false}>
                  {drafts.map((draft) => (
                    <motion.div
                      key={draft.rowId}
                      layout
                      initial={{ opacity: 0, y: -4 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={{ opacity: 0, x: -8, transition: { duration: 0.15 } }}
                      transition={{ duration: 0.2, ease: 'easeOut' }}
                      className="grid grid-cols-[1fr_auto] gap-2 sm:grid-cols-[1fr_1.4fr_auto]"
                    >
                      <Input
                        value={draft.name}
                        onChange={(e) => updateDraft(draft.rowId, 'name', e.target.value)}
                        onPaste={(e) => handlePaste(draft.rowId, 'name', e)}
                        placeholder="Navn"
                        maxLength={200}
                        autoComplete="off"
                        className="col-span-2 sm:col-span-1"
                      />
                      <Input
                        type="email"
                        value={draft.email}
                        onChange={(e) => updateDraft(draft.rowId, 'email', e.target.value)}
                        onPaste={(e) => handlePaste(draft.rowId, 'email', e)}
                        placeholder="email@example.dk"
                        maxLength={320}
                        autoComplete="off"
                        data-1p-ignore
                        data-lpignore="true"
                        data-bwignore
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => removeRow(draft.rowId)}
                        disabled={drafts.length === 1 && !draft.name && !draft.email}
                        aria-label="Fjern linje"
                        className="text-muted-foreground hover:text-destructive size-8"
                      >
                        <X className="size-4" />
                      </Button>
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>
              {addError && <p className="text-destructive text-xs">{addError}</p>}
            </div>
            <div className="flex flex-wrap items-center justify-between gap-2">
              <Button type="button" variant="ghost" size="sm" onClick={addRow}>
                <Plus className="size-4" />
                Endnu en linje
              </Button>
              <Button type="submit" disabled={adding} size="sm">
                <UserPlus className="size-4" />
                {adding ? 'Tilføjer…' : 'Tilføj'}
              </Button>
            </div>
          </form>

          {loadError && <p className="text-destructive text-sm">{loadError}</p>}

          {invitees && invitees.length > 0 && (
            <div className="space-y-3 pt-2">
              <div className="flex flex-wrap items-center justify-between gap-2 border-t pt-3">
                <div className="text-muted-foreground text-xs">
                  {selectedIds.size > 0
                    ? `${selectedIds.size} valgt`
                    : unsentCount > 0
                      ? `${unsentCount} har ikke fået invitation endnu`
                      : 'Alle med email har fået invitation'}
                </div>
                <div className="flex flex-wrap gap-2">
                  {selectedIds.size > 0 ? (
                    <Button
                      type="button"
                      size="sm"
                      onClick={() => onSend('selected')}
                      disabled={sending}
                    >
                      <Send className="size-4" />
                      {sending ? 'Sender…' : `Gensend (${selectedIds.size})`}
                    </Button>
                  ) : (
                    <Button
                      type="button"
                      size="sm"
                      onClick={() => onSend('unsent')}
                      disabled={sending || unsentCount === 0}
                    >
                      <Send className="size-4" />
                      {sending ? 'Sender…' : 'Send til ikke-sendte'}
                    </Button>
                  )}
                </div>
              </div>

              <ul className="divide-border divide-y">
                <AnimatePresence initial={false}>
                  {invitees.map((inv) => {
                    const selected = selectedIds.has(inv.id);
                    return (
                      <motion.li
                        key={inv.id}
                        layout
                        initial={{ opacity: 0, y: 6 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, x: -12, transition: { duration: 0.18 } }}
                        transition={{ duration: 0.22, ease: 'easeOut' }}
                        className="flex items-start gap-3 py-3 first:pt-0 last:pb-0"
                      >
                        <input
                          type="checkbox"
                          checked={selected}
                          onChange={() => toggleSelected(inv.id)}
                          disabled={!inv.email}
                          aria-label={`Vælg ${inviteeLabel(inv)}`}
                          className="border-input text-primary focus-visible:ring-ring/50 mt-1.5 size-4 cursor-pointer rounded border bg-transparent accent-current focus-visible:ring-2"
                        />
                        <div className="min-w-0 flex-1 space-y-2">
                          {editingInvitee?.id === inv.id ? (
                            <div className="space-y-2">
                              <div className="grid gap-2 sm:grid-cols-2">
                                <Input
                                  value={editingInvitee.name}
                                  onChange={(e) =>
                                    setEditingInvitee((prev) =>
                                      prev ? { ...prev, name: e.target.value } : prev,
                                    )
                                  }
                                  placeholder="Navn"
                                  maxLength={200}
                                />
                                <Input
                                  type="email"
                                  value={editingInvitee.email}
                                  onChange={(e) =>
                                    setEditingInvitee((prev) =>
                                      prev ? { ...prev, email: e.target.value } : prev,
                                    )
                                  }
                                  placeholder="email@example.dk"
                                  maxLength={320}
                                />
                              </div>
                              {editError && (
                                <p className="text-destructive text-xs">{editError}</p>
                              )}
                              <div className="flex gap-2">
                                <Button
                                  type="button"
                                  size="sm"
                                  onClick={saveEdit}
                                  disabled={savingEdit}
                                >
                                  {savingEdit ? 'Gemmer...' : 'Gem'}
                                </Button>
                                <Button
                                  type="button"
                                  variant="secondary"
                                  size="sm"
                                  onClick={() => setEditingInvitee(null)}
                                >
                                  Annullér
                                </Button>
                              </div>
                            </div>
                          ) : (
                            <>
                              <div className="flex flex-wrap items-baseline gap-x-2">
                                <span className="text-sm font-medium">{inviteeLabel(inv)}</span>
                                {inv.email ? (
                                  <a
                                    href={`mailto:${inv.email}`}
                                    className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1 text-xs"
                                  >
                                    <Mail className="size-3" />
                                    {inv.email}
                                  </a>
                                ) : (
                                  <span className="text-muted-foreground text-xs">Ingen email</span>
                                )}
                              </div>
                              <div className="flex flex-wrap items-center gap-2 text-xs">
                                {inv.email ? (
                                  inv.lastSentAt ? (
                                    <span className="bg-muted text-muted-foreground rounded-full px-2 py-0.5">
                                      Sendt {formatRelative(inv.lastSentAt)}
                                      {inv.sendCount > 1 ? ` · ${inv.sendCount}×` : ''}
                                    </span>
                                  ) : (
                                    <span className="text-muted-foreground">Ikke sendt</span>
                                  )
                                ) : (
                                  <span className="text-muted-foreground">Del via personligt link</span>
                                )}
                                {inv.rsvpStatus && <StatusBadge status={inv.rsvpStatus} />}
                              </div>
                            </>
                          )}
                        </div>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            className="text-muted-foreground hover:text-foreground size-8"
                            onClick={() => copyPersonalLink(inv)}
                            aria-label={`Kopier personligt invite-link for ${inviteeLabel(inv)}`}
                          >
                            <Copy className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="text-muted-foreground hover:text-foreground size-8"
                            onClick={() => startEdit(inv)}
                            aria-label={`Redigér ${inviteeLabel(inv)}`}
                          >
                            <Pencil className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="text-muted-foreground hover:text-destructive size-8"
                            onClick={() => setPendingRemoval({ id: inv.id, label: inviteeLabel(inv) })}
                            aria-label={`Fjern ${inviteeLabel(inv)}`}
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        </div>
                      </motion.li>
                    );
                  })}
                </AnimatePresence>
              </ul>
            </div>
          )}
        </CardContent>
      </Card>

      <AlertDialog
        open={!!pendingRemoval}
        onOpenChange={(open) => !open && setPendingRemoval(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Fjern fra invitationsliste?</AlertDialogTitle>
            <AlertDialogDescription>
              {pendingRemoval
                ? `${pendingRemoval.label} fjernes fra invitationslisten for "${eventTitle}". Eksisterende svar bevares.`
                : ''}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Annullér</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete}>Fjern</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
