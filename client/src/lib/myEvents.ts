const KEY = 'inviter.myEvents';

export interface MyEvent {
  id: string;
  title: string;
  adminToken: string;
  createdAt: string;
}

export function listMyEvents(): MyEvent[] {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export function rememberEvent(ev: MyEvent): void {
  const existing = listMyEvents().filter((e) => e.id !== ev.id);
  existing.unshift(ev);
  localStorage.setItem(KEY, JSON.stringify(existing));
}

export function forgetEvent(id: string): void {
  const next = listMyEvents().filter((e) => e.id !== id);
  localStorage.setItem(KEY, JSON.stringify(next));
}

export function updateRememberedTitle(id: string, title: string): void {
  const list = listMyEvents();
  const idx = list.findIndex((e) => e.id === id);
  if (idx === -1) return;
  list[idx] = { ...list[idx], title };
  localStorage.setItem(KEY, JSON.stringify(list));
}
