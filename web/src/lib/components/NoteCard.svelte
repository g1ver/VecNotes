<script lang="ts">
	import { Card } from '$lib/components/ui/card';
	import StatusBadge from './StatusBadge.svelte';
	import type { Note } from '$lib/api';

	let { note, onclick }: { note: Note; onclick?: () => void } = $props();

	const preview = $derived(
		note.chunks[0]?.noteText?.slice(0, 120) ??
		note.rawTranscript?.slice(0, 120) ??
		'No content yet'
	);

	const date = $derived(
		new Date(note.createdAt).toLocaleDateString('en-US', {
			month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
		})
	);
</script>

<button
	class="w-full text-left"
	{onclick}
>
	<div class="rounded-lg border border-border bg-card p-4 transition-colors hover:bg-muted/50 cursor-pointer">
		<div class="flex items-start justify-between gap-2 mb-2">
			<span class="text-xs text-muted-foreground">{date}</span>
			<StatusBadge status={note.status} />
		</div>
		<p class="text-sm text-foreground line-clamp-2 leading-relaxed">
			{preview}{preview.length === 120 ? '…' : ''}
		</p>
	</div>
</button>
