import type { RouteLocationNormalized } from 'vue-router';
import { createRouter, createWebHistory } from 'vue-router';
import { mainRoutes } from '@/router/routes';
import { user } from '@/store/user';

const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes: mainRoutes,
});

router.beforeEach((to: RouteLocationNormalized, from: RouteLocationNormalized) => {
	// Redirect to home if trying to access login while authenticated
	if (to.name === 'Login' && to.params.provider === 'twitch' && user.value?.access_token) {
		return { name: 'Home' };
	}

	// Add authentication check for protected routes if needed
	if (to.meta.requiresAuth && !user.value?.access_token) {
		return { name: 'Login' };
	}

	if (to.name === from.name && to.params?.channelName !== from.params?.channelName) {
		return { ...to, replace: true };
	}
});

export default router;
