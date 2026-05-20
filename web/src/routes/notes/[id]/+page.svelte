<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { api, type Note } from '$lib/api';
	import { Button } from '$lib/components/ui/button';
	import StatusBadge from '$lib/components/StatusBadge.svelte';

	const id = $derived(Number($page.params.id));

	type PageState =
		| { kind: 'loading' }
		| { kind: 'note'; data: Note }
		| { kind: 'error'; message: string };

	let state: PageState = $state({ kind: 'loading' });
	let transcript = $state('');
	let busy = $state(false);

	$effect(() => {
		const noteId = id;
		state = { kind: 'loading' };
		api.getNote(noteId).then(
			(note) => {
				state = { kind: 'note', data: note };
				transcript = note.rawTranscript ?? '';
			},
			() => { state = { kind: 'error', message: 'Note not found.' }; }
		);
	});

	async function transcribe() {
		if (state.kind !== 'note') return;
		busy = true;
		try {
			const res = await api.transcribe(id);
			const note = await api.getNote(id);
			state = { kind: 'note', data: note };
			transcript = note.rawTranscript ?? '';
		} catch {
			alert('Transcription failed.');
		} finally {
			busy = false;
		}
	}

	async function confirmNote() {
		if (state.kind !== 'note') return;
		busy = true;
		try {
			if (transcript !== (state.data.rawTranscript ?? '')) {
				await api.updateTranscript(id, transcript);
			}
			const note = await api.confirm(id);
			state = { kind: 'note', data: note };
		} catch {
			alert('Confirm failed.');
		} finally {
			busy = false;
		}
	}

	async function deleteNote() {
		if (!confirm('Delete this note?')) return;
		await api.deleteNote(id);
		goto('/');
	}

	const date = $derived(
		state.kind === 'note'
			? new Date(state.data.createdAt).toLocaleDateString('en-US', {
					weekday: 'short', month: 'short', day: 'numeric',
					hour: '2-digit', minute: '2-digit'
				})
			: ''
	);
</script>

{#if state.kind === 'loading'}
	<div class="space-y-4 animate-pulse">
		<div class="h-4 w-32 rounded bg-muted"></div>
		<div class="h-40 rounded bg-muted"></div>
	</div>

{:else if state.kind === 'error'}
	<p class="text-sm text-destructive">{state.message}</p>

{:else}
	{@const note = state.data}
	<div class="space-y-6">
		<div class="flex items-center justify-between gap-4">
			<div class="flex items-center gap-3">
				<span class="text-xs text-muted-foreground">{date}</span>
				<StatusBadge status={note.status} />
			</div>
			<button
				onclick={deleteNote}
				class="text-xs text-muted-foreground hover:text-destructive transition-colors"
				aria-label="Delete note"
			>
				Delete
			</button>
		</div>

		<audio src={api.audioUrl(id)} controls crossorigin="anonymous" class="w-full"></audio>

		{#if note.status === 'Uploaded'}
			<div class="flex flex-col items-center gap-4 py-12 text-center">
				<p class="text-sm text-muted-foreground">Audio uploaded. Ready to transcribe.</p>
				<Button onclick={transcribe} disabled={busy}>
					{busy ? 'Transcribing…' : 'Transcribe'}
				</Button>
			</div>

		{:else if note.status === 'Transcribed'}
			<div class="space-y-4">
				<p class="text-xs text-muted-foreground">Review and edit the transcript, then confirm.</p>
				<textarea
					bind:value={transcript}
					rows={12}
					class="w-full rounded-md border border-border bg-card px-3 py-2 text-sm text-foreground leading-relaxed resize-none focus:outline-none focus:ring-1 focus:ring-ring"
				></textarea>
				<div class="flex justify-end">
					<Button onclick={confirmNote} disabled={busy}>
						{busy ? 'Confirming…' : 'Confirm'}
					</Button>
				</div>
			</div>

		{:else if note.status === 'Confirmed'}
			<div class="space-y-3">
				{#each note.chunks as chunk (chunk.id)}
					<div class="rounded-lg border border-border bg-card px-4 py-3">
						<span class="text-xs text-muted-foreground tabular-nums mr-2">#{chunk.sequencePlace}</span>
						<span class="text-sm text-foreground leading-relaxed">{chunk.noteText}</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>
{/if}
