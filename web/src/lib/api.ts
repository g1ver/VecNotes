import { PUBLIC_API_URL } from '$env/static/public';

const base = PUBLIC_API_URL;

export type NoteStatus = 'Uploaded' | 'Transcribed' | 'Confirmed';

export interface Chunk {
	id: number;
	sequencePlace: number;
	noteText: string | null;
}

export interface Note {
	id: number;
	createdAt: string;
	updatedAt: string;
	status?: NoteStatus;
	rawTranscript?: string | null;
	chunks: Chunk[];
}

export interface SearchResult {
	noteId: number;
	chunkId: number;
	sequencePlace: number;
	noteText: string | null;
	score: number;
}

export interface TranscriptResponse {
	id: number;
	status: NoteStatus;
	transcript: string | null;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
	const res = await fetch(`${base}${path}`, init);
	if (!res.ok) throw new Error(await res.text());
	if (res.status === 204) return undefined as T;
	return res.json();
}

export const api = {
	getNotes: () => request<Note[]>('/notes'),

	getNote: (id: number) => request<Note>(`/notes/${id}`),

	getTranscript: (id: number) => request<TranscriptResponse>(`/notes/${id}/transcript`),

	updateTranscript: (id: number, transcript: string) =>
		request<TranscriptResponse>(`/notes/${id}/transcript`, {
			method: 'PUT',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ transcript })
		}),

	transcribe: (id: number) =>
		request<TranscriptResponse>(`/notes/${id}/transcribe`, { method: 'POST' }),

	confirm: (id: number) => request<Note>(`/notes/${id}/confirm`, { method: 'POST' }),

	upload: (blob: Blob, filename = 'recording.webm') => {
		const form = new FormData();
		form.append('audio', blob, filename);
		return request<{ id: number; status: NoteStatus }>('/audio', { method: 'POST', body: form });
	},

	search: (q: string, limit = 8) =>
		request<SearchResult[]>(`/notes/search?q=${encodeURIComponent(q)}&limit=${limit}`),

	deleteNote: (id: number) => request<void>(`/notes/${id}`, { method: 'DELETE' }),

	audioUrl: (id: number) => `${base}/notes/${id}/audio`
};
