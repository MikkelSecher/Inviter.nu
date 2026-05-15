export type RsvpStatus = 'Yes' | 'No' | 'Maybe';

export type ContactRequirement = 'None' | 'Email' | 'Phone';

export interface EventPublic {
  id: string;
  title: string;
  description: string;
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
  startsAt: string;
  inviteToken: string;
  adminToken: string;
  createdAt: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
  rsvps: Rsvp[];
}

export interface EventCreated {
  id: string;
  title: string;
  description: string;
  startsAt: string;
  inviteToken: string;
  adminToken: string;
  createdAt: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
}

export interface CreateEventInput {
  title: string;
  description: string;
  startsAt: string;
  allowMaybe: boolean;
  rsvpDeadline: string | null;
  contactRequirement: ContactRequirement;
}

export interface CreateRsvpInput {
  guestName: string;
  status: RsvpStatus;
  comment: string | null;
  email: string | null;
  phone: string | null;
}
