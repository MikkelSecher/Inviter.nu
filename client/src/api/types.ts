export type RsvpStatus = 'Yes' | 'No' | 'Maybe';

export type ContactRequirement = 'None' | 'Email' | 'Phone';

export interface EventPublic {
  id: string;
  title: string;
  description: string;
  location: string;
  startsAt: string;
  inviteToken: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
  imageUrl: string | null;
}

export interface Rsvp {
  id: string;
  inviteeId: string | null;
  inviteeName: string | null;
  inviteeEmail: string | null;
  guestName: string;
  status: RsvpStatus;
  comment: string | null;
  email: string | null;
  phone: string | null;
  submittedAt: string;
}

export interface EventAdmin {
  id: string;
  title: string;
  description: string;
  location: string;
  startsAt: string;
  inviteToken: string;
  adminToken: string;
  createdAt: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
  organizerEmail: string | null;
  organizerName: string | null;
  imageUrl: string | null;
  rsvps: Rsvp[];
}

export interface EventCreated {
  id: string;
  title: string;
  description: string;
  location: string;
  startsAt: string;
  inviteToken: string;
  adminToken: string;
  createdAt: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
  organizerEmail: string | null;
  organizerName: string | null;
  imageUrl: string | null;
}

export interface CreateEventInput {
  title: string;
  description: string;
  location: string;
  startsAt: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
  organizerEmail: string | null;
  organizerName: string | null;
}

export interface CreateRsvpInput {
  guestName: string;
  status: RsvpStatus;
  comment: string | null;
  email: string | null;
  phone: string | null;
  inviteeToken: string | null;
}

export interface InviteePrefill {
  name: string | null;
  email: string | null;
}

export interface Invitee {
  id: string;
  personalInviteToken: string;
  email: string | null;
  name: string | null;
  addedAt: string;
  lastSentAt: string | null;
  sendCount: number;
  rsvpStatus: RsvpStatus | null;
}

export interface AddInviteeEntry {
  email: string | null;
  name: string | null;
}

export interface AddInviteesResponse {
  added: Invitee[];
  skippedDuplicates: string[];
  skippedInvalid: string[];
}

export interface SendInvitationsInput {
  inviteeIds: string[] | null;
  onlyUnsent: boolean;
}

export interface SendInvitationsResponse {
  enqueued: number;
}

export interface UpdateInviteeInput {
  email: string | null;
  name: string | null;
}

export interface UploadEventImageResponse {
  imageUrl: string;
}

export type MetricsPeriod = '7d' | '30d' | '90d' | 'all';

export interface MetricsSnapshot {
  events: number;
  rsvps: number;
  invitees: number;
  emails: number;
  period: MetricsPeriod;
  upcomingOnly: boolean;
}
