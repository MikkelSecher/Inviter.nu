import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion } from 'motion/react';
import { toast } from 'sonner';
import { CalendarClock, Plus, Sparkles, X } from 'lucide-react';
import { api, ApiError } from '../api/client';
import type { AddInviteeEntry, ContactRequirement } from '../api/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { Field } from '@/components/Field';
import { DateTimePicker } from '@/components/DateTimePicker';
import { ImageDropZone } from '@/components/ImageDropZone';
import { fromDatetimeLocalValue } from '../lib/format';
import { rememberEvent } from '../lib/myEvents';

type GuestDraft = { rowId: string; name: string; email: string };

function newGuestDraft(seed: Partial<Omit<GuestDraft, 'rowId'>> = {}): GuestDraft {
  return {
    rowId:
      typeof crypto !== 'undefined' && 'randomUUID' in crypto
        ? crypto.randomUUID()
        : Math.random().toString(36).slice(2),
    name: seed.name ?? '',
    email: seed.email ?? '',
  };
}

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
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [guestDrafts, setGuestDrafts] = useState<GuestDraft[]>(() => [newGuestDraft()]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function pickImage(file: File) {
    if (imagePreview) URL.revokeObjectURL(imagePreview);
    setImageFile(file);
    setImagePreview(URL.createObjectURL(file));
  }

  function clearImage() {
    if (imagePreview) URL.revokeObjectURL(imagePreview);
    setImageFile(null);
    setImagePreview(null);
  }

  function updateGuestDraft(rowId: string, field: 'name' | 'email', value: string) {
    setGuestDrafts((prev) => prev.map((d) => (d.rowId === rowId ? { ...d, [field]: value } : d)));
  }

  function addGuestRow() {
    setGuestDrafts((prev) => [...prev, newGuestDraft()]);
  }

  function removeGuestRow(rowId: string) {
    setGuestDrafts((prev) => {
      const next = prev.filter((d) => d.rowId !== rowId);
      return next.length === 0 ? [newGuestDraft()] : next;
    });
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
      const guests: AddInviteeEntry[] = guestDrafts
        .map((d) => ({ name: d.name.trim() || null, email: d.email.trim() || null }))
        .filter((entry) => Boolean(entry.name || entry.email));
      if (guests.length > 0) {
        try {
          const added = await api.addInvitees(created.adminToken, guests);
          if (added.skippedDuplicates.length > 0 || added.skippedInvalid.length > 0) {
            toast.message('Nogle gæster blev sprunget over', {
              description: 'Du kan rette gæstelisten fra admin-siden.',
            });
          }
        } catch {
          toast.error('Eventet er oprettet, men gæstelisten kunne ikke gemmes.');
        }
      }
      if (imageFile) {
        try {
          await api.uploadEventImage(created.adminToken, imageFile);
        } catch {
          toast.error(
            'Eventet er oprettet, men billedet kunne ikke uploades. Du kan prøve igen fra event-siden.',
          );
        }
      }
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
      className="grid gap-8 lg:grid-cols-[minmax(0,0.85fr)_minmax(460px,1.15fr)] lg:items-start"
    >
      <section className="space-y-6 lg:sticky lg:top-24">
      <div className="space-y-3">
        <div className="text-primary inline-flex items-center gap-1.5 text-xs font-medium tracking-wide uppercase">
          <Sparkles className="size-3.5" />
          Et nyt event
        </div>
        <h1
          className="font-serif text-5xl leading-[0.98] tracking-tight sm:text-6xl"
          style={{ fontVariationSettings: '"opsz" 144' }}
        >
          Saml folk omkring noget rart.
        </h1>
        <p className="text-muted-foreground max-w-prose text-base leading-7">
          Udfyld det vigtigste. Du får et delbart invite-link og en hemmelig admin-URL bagefter — så
          kan du følge med i hvem der kommer.
        </p>
      </div>

      <div className="divide-border divide-y border-y">
        <div className="grid gap-1 py-4">
          <div className="text-sm font-semibold">1. Opret eventet</div>
          <p className="text-muted-foreground text-sm">Skriv dato, sted og det vigtigste for gæsterne.</p>
        </div>
        <div className="grid gap-1 py-4">
          <div className="text-sm font-semibold">2. Del invitationen</div>
          <p className="text-muted-foreground text-sm">Send linket direkte eller brug invitationslisten.</p>
        </div>
        <div className="grid gap-1 py-4">
          <div className="text-sm font-semibold">3. Følg svarene</div>
          <p className="text-muted-foreground text-sm">Admin-siden samler tilmeldinger og kontaktinfo.</p>
        </div>
      </div>
      </section>

      <Card className="shadow-lg shadow-primary/5">
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

            <div className="border-border/70 space-y-4 rounded-lg border p-4">
              <div className="space-y-0.5">
                <Label className="text-sm font-medium">Gæsteliste (valgfri)</Label>
                <p className="text-muted-foreground text-xs">
                  Tilføj gæster nu eller senere. Email er valgfri; alle får et personligt link.
                </p>
              </div>
              <div className="hidden grid-cols-[1fr_1.4fr_auto] gap-2 px-1 sm:grid">
                <div className="text-muted-foreground text-xs">Navn</div>
                <div className="text-muted-foreground text-xs">Email (valgfri)</div>
                <div />
              </div>
              <div className="space-y-2">
                {guestDrafts.map((draft) => (
                  <div
                    key={draft.rowId}
                    className="grid grid-cols-[1fr_auto] gap-2 sm:grid-cols-[1fr_1.4fr_auto]"
                  >
                    <Input
                      value={draft.name}
                      onChange={(e) => updateGuestDraft(draft.rowId, 'name', e.target.value)}
                      placeholder="Anne Andersen"
                      maxLength={200}
                      autoComplete="off"
                      className="col-span-2 sm:col-span-1"
                    />
                    <Input
                      type="email"
                      value={draft.email}
                      onChange={(e) => updateGuestDraft(draft.rowId, 'email', e.target.value)}
                      placeholder="anne@example.dk"
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
                      onClick={() => removeGuestRow(draft.rowId)}
                      disabled={guestDrafts.length === 1 && !draft.name && !draft.email}
                      aria-label="Fjern gæstelinje"
                      className="text-muted-foreground hover:text-destructive size-8"
                    >
                      <X className="size-4" />
                    </Button>
                  </div>
                ))}
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={addGuestRow}>
                <Plus className="size-4" />
                Tilføj gæst
              </Button>
            </div>

            <Field
              label="Eventbillede (valgfri)"
              hint="Vises øverst på invitationssiden og i invitations-mailen."
            >
              <ImageDropZone
                imageUrl={imagePreview}
                onPick={pickImage}
                onRemove={clearImage}
                disabled={submitting}
              />
            </Field>

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
