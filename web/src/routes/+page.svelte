<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { api, type Note, type SearchResult } from '$lib/api';
	import NoteCard from '$lib/components/NoteCard.svelte';
	import ChunkCard from '$lib/components/ChunkCard.svelte';

	let q = $derived($page.url.searchParams.get('q') ?? '');

	type PageState =
		| { kind: 'loading' }
		| { kind: 'notes'; data: Note[] }
		| { kind: 'results'; data: SearchResult[] }
		| { kind: 'error'; message: string };

	let state: PageState = $state({ kind: 'loading' });

	$effect(() => {
		state = { kind: 'loading' };
		const current = q;
		if (current) {
			api.search(current).then(
				(data) => { if (q === current) state = { kind: 'results', data }; },
				() => { if (q === current) state = { kind: 'error', message: 'Search failed.' }; }
			);
		} else {
			api.getNotes().then(
				(data) => { if (q === current) state = { kind: 'notes', data }; },
				() => { if (q === current) state = { kind: 'error', message: 'Failed to load notes.' }; }
			);
		}
	});
</script>

{#if state.kind === 'loading'}
	<div class="space-y-3">
		{#each { length: 4 } as _}
			<div class="rounded-lg border border-border bg-card p-4 animate-pulse space-y-2">
				<div class="h-3 w-24 rounded bg-muted"></div>
				<div class="h-3 w-full rounded bg-muted"></div>
				<div class="h-3 w-3/4 rounded bg-muted"></div>
			</div>
		{/each}
	</div>

{:else if state.kind === 'error'}
	<p class="text-sm text-destructive">{state.message}</p>

{:else if state.kind === 'results'}
	{#if state.data.length === 0}
		<p class="text-sm text-muted-foreground text-center py-16">No results for "{q}".</p>
	{:else}
		<p class="text-xs text-muted-foreground mb-3">{state.data.length} result{state.data.length === 1 ? '' : 's'} for "{q}"</p>
		<div class="space-y-3">
			{#each state.data as result (result.chunkId)}
				<ChunkCard {result} onclick={() => goto(`/notes/${result.noteId}`)} />
			{/each}
		</div>
	{/if}

{:else if state.kind === 'notes'}
	{#if state.data.length === 0}
		<div class="flex flex-col items-center gap-3 py-24 text-center">
			<p class="text-sm text-muted-foreground">No notes yet.</p>
			<p class="text-xs text-muted-foreground">Click Record to leave your first voice note.</p>
		</div>
	{:else}
		<div class="space-y-3">
			{#each state.data as note (note.id)}
				<NoteCard {note} onclick={() => goto(`/notes/${note.id}`)} />
			{/each}
		</div>
	{/if}
{/if}
