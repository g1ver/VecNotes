<script lang="ts">
	import '../app.css';
	import { goto } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import RecordDialog from '$lib/components/RecordDialog.svelte';

	let { children } = $props();

	let query = $state('');
	let dialogOpen = $state(false);

	function search() {
		if (query.trim()) goto(`/?q=${encodeURIComponent(query.trim())}`);
		else goto('/');
	}
</script>

<div class="min-h-screen bg-background flex flex-col">
	<header class="border-b border-border px-4 h-14 flex items-center gap-3 sticky top-0 bg-background/80 backdrop-blur z-10">
		<a href="/" class="text-sm font-semibold tracking-tight text-foreground shrink-0">VecNotes</a>
		<div class="flex-1 max-w-md">
			<form onsubmit={(e) => { e.preventDefault(); search(); }} class="relative">
				<Input
					bind:value={query}
					placeholder="Search notes…"
					class="pr-8 h-8 text-sm"
				/>
			</form>
		</div>
		<Button
			size="sm"
			onclick={() => (dialogOpen = true)}
			class="shrink-0 gap-1.5"
		>
			<svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="currentColor">
				<path d="M12 14a3 3 0 0 0 3-3V5a3 3 0 0 0-6 0v6a3 3 0 0 0 3 3zm5-3a5 5 0 0 1-10 0H5a7 7 0 0 0 6 6.92V20H9v2h6v-2h-2v-2.08A7 7 0 0 0 19 11h-2z"/>
			</svg>
			Record
		</Button>
	</header>

	<main class="flex-1 max-w-2xl w-full mx-auto px-4 py-6">
		{@render children()}
	</main>
</div>

<RecordDialog bind:open={dialogOpen} />
