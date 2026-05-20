<script lang="ts">
	import { Badge } from '$lib/components/ui/badge';
	import type { NoteStatus } from '$lib/api';

	let { status }: { status: NoteStatus | undefined } = $props();

	const variants: Record<string, string> = {
		Uploaded: 'bg-zinc-500/15 text-zinc-400 border-zinc-500/20',
		Transcribed: 'bg-amber-500/15 text-amber-400 border-amber-500/20',
		Confirmed: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20'
	};
</script>

{#if status}
	<span class="inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium {variants[status] ?? ''}">
		{status}
	</span>
{/if}
