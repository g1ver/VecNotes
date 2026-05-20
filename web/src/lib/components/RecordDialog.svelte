<script lang="ts">
	import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { api } from '$lib/api';
	import { goto } from '$app/navigation';

	let { open = $bindable(false) }: { open?: boolean } = $props();

	type State = 'idle' | 'recording' | 'preview' | 'uploading' | 'error';
	let state: State = $state('idle');
	let error = $state('');
	let recorder: MediaRecorder | null = null;
	let chunks: BlobPart[] = [];
	let previewUrl = $state('');

	async function start() {
		try {
			const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
			chunks = [];
			recorder = new MediaRecorder(stream);
			recorder.ondataavailable = (e) => chunks.push(e.data);
			recorder.onstop = () => {
				previewUrl = URL.createObjectURL(new Blob(chunks, { type: 'audio/webm' }));
				state = 'preview';
			};
			recorder.start();
			state = 'recording';
		} catch {
			error = 'Microphone access denied.';
			state = 'error';
		}
	}

	function stop() {
		recorder?.stop();
		recorder?.stream.getTracks().forEach((t) => t.stop());
	}

	function rerecord() {
		URL.revokeObjectURL(previewUrl);
		previewUrl = '';
		state = 'idle';
	}

	async function upload() {
		state = 'uploading';
		try {
			const blob = new Blob(chunks, { type: 'audio/webm' });
			const { id } = await api.upload(blob);
			URL.revokeObjectURL(previewUrl);
			previewUrl = '';
			open = false;
			state = 'idle';
			goto(`/notes/${id}`);
		} catch {
			error = 'Upload failed. Is the server running?';
			state = 'error';
		}
	}

	function reset() {
		if (previewUrl) URL.revokeObjectURL(previewUrl);
		previewUrl = '';
		state = 'idle';
		error = '';
	}
</script>

<Dialog bind:open>
	<DialogContent class="sm:max-w-sm">
		<DialogHeader>
			<DialogTitle>New voice note</DialogTitle>
		</DialogHeader>

		<div class="flex flex-col items-center gap-6 py-4">
			{#if state === 'idle'}
				<button
					onclick={start}
					class="w-20 h-20 rounded-full bg-primary flex items-center justify-center transition-transform hover:scale-105 active:scale-95"
					aria-label="Start recording"
				>
					<svg xmlns="http://www.w3.org/2000/svg" class="w-8 h-8 text-primary-foreground" viewBox="0 0 24 24" fill="currentColor">
						<path d="M12 14a3 3 0 0 0 3-3V5a3 3 0 0 0-6 0v6a3 3 0 0 0 3 3zm5-3a5 5 0 0 1-10 0H5a7 7 0 0 0 6 6.92V20H9v2h6v-2h-2v-2.08A7 7 0 0 0 19 11h-2z"/>
					</svg>
				</button>
				<p class="text-sm text-muted-foreground">Tap to start recording</p>

			{:else if state === 'recording'}
				<button
					onclick={stop}
					class="w-20 h-20 rounded-full bg-destructive flex items-center justify-center relative"
					aria-label="Stop recording"
				>
					<span class="absolute inset-0 rounded-full bg-destructive animate-ping opacity-40"></span>
					<span class="w-7 h-7 rounded-sm bg-destructive-foreground"></span>
				</button>
				<p class="text-sm text-muted-foreground">Recording… tap to stop</p>

			{:else if state === 'preview'}
				<audio src={previewUrl} controls class="w-full"></audio>
				<div class="flex gap-3 w-full">
					<Button variant="outline" class="flex-1" onclick={rerecord}>Re-record</Button>
					<Button class="flex-1" onclick={upload}>Upload</Button>
				</div>

			{:else if state === 'uploading'}
				<div class="w-20 h-20 rounded-full border-4 border-primary border-t-transparent animate-spin"></div>
				<p class="text-sm text-muted-foreground">Uploading…</p>

			{:else if state === 'error'}
				<p class="text-sm text-destructive text-center">{error}</p>
				<Button variant="outline" onclick={reset}>Try again</Button>
			{/if}
		</div>
	</DialogContent>
</Dialog>
