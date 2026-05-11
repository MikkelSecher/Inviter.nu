export type RsvpStatus = 'Yes' | 'No' | 'Maybe';

export interface EventPublic {
  id: string;
  title: string;
  description: string;
  startsAt: string;
  inviteToken: string;
}

export interface Rsvp {
  id: string;
  guestName: string;
  status: RsvpStatus;
  comment: string | null;
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
}

export interface CreateEventInput {
  title: string;
  description: string;
  startsAt: string;
}

export interface CreateRsvpInput {
  guestName: string;
  status: RsvpStatus;
  comment: string | null;
}
