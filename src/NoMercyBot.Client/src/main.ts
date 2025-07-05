import './assets/base.css';

import { createApp } from 'vue';
import I18NextVue from 'i18next-vue';
import { VueQueryPlugin } from '@tanstack/vue-query';

import router from './router';
import '@/router/routes';

import i18next from '@/config/i18Next';
import { queryClient } from '@/config/tanstack-query';

import App from './App.vue';

const app = createApp(App);

app.use(I18NextVue, {
	i18next,
	rerenderOn: ['languageChanged', 'loaded'],
});

app.use(VueQueryPlugin, {
	enableDevtoolsV6Plugin: true,
	queryClient,
});

app.use(router);

router.isReady().then(() => {
	app.mount('#app');
});
