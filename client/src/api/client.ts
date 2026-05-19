import type {
  AddInviteeEntry,
  AddInviteesResponse,
  CreateEventInput,
  CreateRsvpInput,
  EventAdmin,
  EventCreated,
  EventPublic,
  Invitee,
  Rsvp,
  SendInvitationsInput,
  SendInvitationsResponse,
} from './types';

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  });
  if (!res.ok) {
    let message = res.statusText;
    try {
      const body = await res.json();
      message = body?.title || body?.detail || message;
    } catch {
      // ignore
    }
    throw new ApiError(res.status, message);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const api = {
  createEvent: (input: CreateEventInput) =>
    request<EventCreated>('/api/events', {
      method: 'POST',
      body: JSON.stringify(input),
    }),

  getInvite: (inviteToken: string) =>
    request<EventPublic>(`/api/invite/${encodeURIComponent(inviteToken)}`),

  submitRsvp: (inviteToken: string, input: CreateRsvpInput) =>
    request<Rsvp>(`/api/invite/${encodeURIComponent(inviteToken)}/rsvp`, {
      method: 'POST',
      body: JSON.stringify(input),
    }),

  getManage: (adminToken: string) =>
    request<EventAdmin>(`/api/manage/${encodeURIComponent(adminToken)}`),

  updateEvent: (adminToken: string, input: CreateEventInput) =>
    request<void>(`/api/manage/${encodeURIComponent(adminToken)}`, {
      method: 'PUT',
      body: JSON.stringify(input),
    }),

  deleteRsvp: (adminToken: string, rsvpId: string) =>
    request<void>(
      `/api/manage/${encodeURIComponent(adminToken)}/rsvp/${encodeURIComponent(rsvpId)}`,
      { method: 'DELETE' },
    ),

  listInvitees: (adminToken: string) =>
    request<Invitee[]>(`/api/manage/${encodeURIComponent(adminToken)}/invitees`),

  addInvitees: (adminToken: string, entries: AddInviteeEntry[]) =>
    request<AddInviteesResponse>(
      `/api/manage/${encodeURIComponent(adminToken)}/invitees`,
      { method: 'POST', body: JSON.stringify({ entries }) },
    ),

  deleteInvitee: (adminToken: string, inviteeId: string) =>
    request<void>(
      `/api/manage/${encodeURIComponent(adminToken)}/invitees/${encodeURIComponent(inviteeId)}`,
      { method: 'DELETE' },
    ),

  sendInvitations: (adminToken: string, input: SendInvitationsInput) =>
    request<SendInvitationsResponse>(
      `/api/manage/${encodeURIComponent(adminToken)}/invitees/send`,
      { method: 'POST', body: JSON.stringify(input) },
    ),
};
