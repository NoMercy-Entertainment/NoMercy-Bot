import type {RouteLocationNormalized} from 'vue-router';
import {createRouter, createWebHistory} from 'vue-router';
import {mainRoutes} from '@/router/routes';
import {user} from '@/store/user';
import {useSessionStorage} from '@vueuse/core';

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: mainRoutes,
});

router.beforeEach((to: RouteLocationNormalized, from: RouteLocationNormalized) => {
    // Redirect to home if trying to access login while authenticated
    if (to.name !== 'Login') {
        if (user.value?.color) {
            document.documentElement.style.setProperty('--theme', user.value?.color);
        }
    }

    if (
        to.name === 'Login'
        && from.name !== 'Provider Settings'
        && to.params.provider === 'twitch'
        && user.value?.access_token
    ) {
        return {path: useSessionStorage('redirect', '/home').value};
    }

    // Add authentication check for protected routes if needed
    if (to.meta.requiresAuth && !user.value?.access_token) {
        return {name: 'Login'};
    }
});

export default router;
