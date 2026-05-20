<script lang="ts">
	import type { SearchResult } from '$lib/api';

	let { result, onclick }: { result: SearchResult; onclick?: () => void } = $props();

	const pct = $derived(Math.round(result.score * 100));
</script>

<button class="w-full text-left" {onclick}>
	<div class="rounded-lg border border-border bg-card p-4 transition-colors hover:bg-muted/50 cursor-pointer">
		<p class="text-sm text-foreground leading-relaxed mb-3">{result.noteText}</p>
		<div class="flex items-center gap-2">
			<div class="flex-1 h-1 rounded-full bg-muted overflow-hidden">
				<div class="h-full rounded-full bg-primary transition-all" style="width: {pct}%"></div>
			</div>
			<span class="text-xs text-muted-foreground tabular-nums">{pct}%</span>
			<span class="text-xs text-muted-foreground">note {result.noteId}</span>
		</div>
	</div>
</button>
