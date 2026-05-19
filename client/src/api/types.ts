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
}

export interface Rsvp {
  id: string;
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
}

export interface Invitee {
  id: string;
  email: string;
  name: string | null;
  addedAt: string;
  lastSentAt: string | null;
  sendCount: number;
  rsvpStatus: RsvpStatus | null;
}

export interface AddInviteeEntry {
  email: string;
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
