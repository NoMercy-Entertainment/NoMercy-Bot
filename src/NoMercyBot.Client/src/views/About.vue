<script lang="ts" setup>
import { ref } from 'vue';
import { useTranslation } from 'i18next-vue';
import MoooomIcon from '@/components/icons/MoooomIcon.vue';

const { t } = useTranslation();

const appVersion = ref('1.0.0'); // Replace with actual version from your config
const buildDate = ref('2025-07-06'); // Update dynamically or from build info

// Features list
const features = [
	{
		name: 'Multi-platform Integration',
		description: 'Connect with Twitch, YouTube, Discord, Spotify and more services from one dashboard',
		icon: 'syncCircle',
	},
	{
		name: 'Automated Alerts',
		description: 'Customize alerts for followers, subscribers, donations, and other events',
		icon: 'bellAlert',
	},
	{
		name: 'Chat Management',
		description: 'Moderate your chat with automated tools and custom commands',
		icon: 'chatBubble',
	},
	{
		name: 'Analytics Dashboard',
		description: 'Track your growth with detailed analytics and reporting tools',
		icon: 'chartBar',
	},
	{
		name: 'OBS Control',
		description: 'Control your OBS scenes and settings directly from the dashboard',
		icon: 'displaySettings',
	},
	{
		name: 'Stream Scheduling',
		description: 'Plan and announce your streams across all your connected platforms',
		icon: 'calendar',
	},
];

const teamMembers = [
	{ name: 'Your Name', role: 'Lead Developer', avatar: 'https://i.pravatar.cc/150?img=1' },
	// Add more team members as needed
];
</script>

<template>
	<div class="h-inherit flex flex-col mb-auto w-full">
		<!-- Header -->
		<header class="border-b border-white/5 w-full">
			<div class="px-8 pt-8 pb-6">
				<h1 class="text-2xl font-bold text-white">
					{{ t('about.title', 'About NoMercy Bot') }}
				</h1>
				<p class="text-neutral-400 mt-2">
					{{ t('about.subtitle', 'All-in-one streaming companion for content creators') }}
				</p>
			</div>
		</header>

		<!-- Content -->
		<div class="flex-1 px-8 py-6">
			<!-- App Description -->
			<div class="bg-neutral-800 border border-white/10 rounded-lg p-6 shadow-sm">
				<div class="prose prose-invert max-w-none">
					<p class="text-lg text-white">
						{{
							t('about.description', 'NoMercy Bot is a powerful streaming companion that helps content creators manage their streams across multiple platforms. With integrations for popular services like Twitch, YouTube, Discord, and more, NoMercy Bot provides a unified dashboard for all your streaming needs.')
						}}
					</p>
				</div>
			</div>

			<!-- Features -->
			<div class="mt-8">
				<h2 class="text-lg font-medium text-white mb-4">
					{{ t('about.featuresTitle', 'Features') }}
				</h2>
				<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
					<div v-for="feature in features" :key="feature.name"
						class="bg-neutral-800 border border-white/10 rounded-lg p-5 shadow-sm flex"
					>
						<div class="mr-4 flex-shrink-0">
							<MoooomIcon :icon="feature.icon" class="h-6 w-6 text-theme-500" />
						</div>
						<div>
							<h3 class="text-base font-medium text-white">
								{{ feature.name }}
							</h3>
							<p class="mt-1 text-sm text-neutral-400">
								{{ feature.description }}
							</p>
						</div>
					</div>
				</div>
			</div>

			<!-- Team Section -->
			<div v-if="teamMembers.length > 0" class="mt-8">
				<h2 class="text-lg font-medium text-white mb-4">
					{{ t('about.teamTitle', 'Our Team') }}
				</h2>
				<div class="grid grid-cols-2 gap-6 sm:grid-cols-3 lg:grid-cols-4">
					<div v-for="member in teamMembers" :key="member.name"
						class="bg-neutral-800 border border-white/10 rounded-lg p-4 shadow-sm text-center"
					>
						<img :alt="member.name" :src="member.avatar" class="mx-auto h-20 w-20 rounded-full mb-3">
						<h3 class="text-base font-medium text-white">
							{{ member.name }}
						</h3>
						<p class="text-sm text-neutral-400">
							{{ member.role }}
						</p>
					</div>
				</div>
			</div>

			<!-- Version Info -->
			<div class="mt-8 bg-neutral-800 border border-white/10 rounded-lg p-4 shadow-sm">
				<div class="flex justify-between items-center">
					<div>
						<h3 class="text-sm font-medium text-white">
							{{ t('about.versionInfo', 'Version Information') }}
						</h3>
						<p class="text-xs text-neutral-400 mt-1">
							{{ t('about.version', 'Version') }}: {{ appVersion }} |
							{{ t('about.buildDate', 'Build Date') }}: {{ buildDate }}
						</p>
					</div>
					<div>
						<a class="text-theme-500 hover:text-theme-400 text-sm flex items-center" href="https://github.com/yourusername/nomercy-bot" rel="noopener noreferrer"
							target="_blank"
						>
							<MoooomIcon class="h-5 w-5 mr-1" icon="github" />
							{{ t('about.viewOnGitHub', 'View on GitHub') }}
						</a>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<style scoped>
/* Add any specific styles here */
</style>
