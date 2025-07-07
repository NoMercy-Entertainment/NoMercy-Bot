import type {RouteRecordRaw} from 'vue-router';

import type {InferRouteNames, InferRoutePaths} from '@/types/router';
import {routeNameToKey} from '@/types/router';

import {user} from '@/store/user.ts';

import appLayout from '@/layout/AppLayout.vue';
import Home from '@/views/Home.vue';
import Login from '@/views/Auth/Login.vue';
import Unauthenticated from '@/views/Auth/Unauthenticated.vue';
import NotFound from '@/views/NotFound.vue';
import type {MoooomIcons} from '@Icons/icons';
import {useSessionStorage} from '@vueuse/core';

export const mainRoutes: Array<RouteRecordRaw> = [
    {
        path: '/',
        component: appLayout,
        redirect: () => {
            if (user.value?.access_token) {
                return {
                    name: 'Home',
                };
            } else {
                useSessionStorage('redirect', '/home').value = '/home';
                return {
                    name: 'Login',
                    params: {
                        provider: 'twitch',
                    },
                };
            }
        },
        children: [
            {
                path: '/home',
                name: 'Home',
                component: Home,
                meta: {
                    requiresAuth: true,
                    group: 'main',
                    icon: 'home' as keyof typeof MoooomIcons,
                },
            },
            {
                path: '/settings',
                name: 'Settings',
                meta: {
                    requiresAuth: true,
                    group: 'main',
                    icon: 'settings' as keyof typeof MoooomIcons,
                    menuItem: true,
                    collapsible: true,
                },
                children: [
                    {
                        path: '',
                        name: 'Settings Overview',
                        component: () => import('@/views/Settings/Settings.vue'),
                        meta: {
                            requiresAuth: true,
                            group: 'settings',
                            icon: 'gridMasonry' as keyof typeof MoooomIcons,
                            menuItem: true,
                        },
                    },
                    {
                        path: 'providers',
                        name: 'Providers',
                        meta: {
                            requiresAuth: true,
                            group: 'settings',
                            icon: 'shoppingCart' as keyof typeof MoooomIcons,
                            menuItem: true,
                        },
                        children: [
                            {
                                path: '',
                                name: 'Provider List',
                                component: () => import('@/views/Settings/Providers/Providers.vue'),
                                meta: {
                                    requiresAuth: true,
                                },
                            },
                            {
                                path: ':provider',
                                name: 'Provider Settings',
                                component: () => import('@/views/Settings/Providers/ProviderSettings.vue'),
                                meta: {
                                    requiresAuth: true,
                                },
                            },
                        ],
                    },
                ],
            },
            {
                path: '/:catchAll(.*)*',
                component: NotFound,
                props: {
                    message: 'Page not found',
                    status: 404,
                },
            },
            {
                path: '/oauth/:provider',
                children: [
                    {
                        path: '',
                        name: 'Unauthenticated',
                        component: Unauthenticated,
                        meta: {
                            group: 'auth',
                        },
                    },
                    {
                        path: 'login',
                        name: 'Login',
                        component: Login,
                        meta: {
                            group: 'auth',
                        },
                        props: (route) => {
                            return {
                                provider: route.params.provider,
                                query: route.query,
                            };
                        },
                    },
                    {
                        path: 'callback',
                        redirect: (to) => {
                            return {
                                path: `/oauth/${to.params.provider}/login`,
                                query: {...to.query},
                            };
                        },
                        meta: {
                            group: 'auth',
                        },
                    },
                ],
            },
            {
                path: '/oauth/twitch/logout',
                name: 'Sign out',
                component: () => import('@/views/Auth/Logout.vue'),
                meta: {
                    requiresAuth: true,
                    group: 'profileMenu',
                    icon: 'doorOut' as keyof typeof MoooomIcons,
                    menuItem: true,
                },
            },
            {
                path: '/about',
                name: 'About',
                component: () => import('@/views/About.vue'),
                meta: {
                    icon: 'arrowLeftStartOnRectangle' as keyof typeof MoooomIcons,
                    group: 'profileMenu',
                    menuItem: true,
                },
            },
            {
                path: '/terms',
                name: 'Terms of Service',
                component: () => import('@/views/Legal/Terms.vue'),
                meta: {
                    icon: 'bookOpen' as keyof typeof MoooomIcons,
                },
            },
            {
                path: '/privacy',
                name: 'Privacy Policy',
                component: () => import('@/views/Legal/Privacy.vue'),
                meta: {
                    icon: 'shieldCheck' as keyof typeof MoooomIcons,
                },
            },
        ],
    },
];

// Combine all routes
export const routes = mainRoutes.at(0)!.children;

export default routes;

export type RouteName = InferRouteNames<typeof mainRoutes>;
export type RoutePath = InferRoutePaths<typeof mainRoutes>;

export function getRoutePaths(routes: RouteRecordRaw[]) {
    const paths = routes.reduce(
        (acc, route) => {
            if (route.children) {
                for (const child of route.children) {
                    if (!child.name) {
                        continue;
                    }
                    const key = routeNameToKey(child.name as string);
                    acc[key] = child.path;
                }
            }
            return acc;
        },
        {} as Record<string, string>,
    );

    return paths as { [K in (typeof routes)[number]['name'] as string]: (typeof routes)[number]['path'] };
}

export function getRouteNames(routes: RouteRecordRaw[]): Record<string, RouteName> {
    return routes.reduce(
        (acc, route) => {
            if (route.name) {
                acc[route.name.toString().toLowerCase().replace(/\s+/g, '_')] = route.name as RouteName;
            }
            return acc;
        },
        {} as Record<string, RouteName>,
    );
}

export const routeNames = getRouteNames(mainRoutes);
export const routePaths = getRoutePaths(mainRoutes);
