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

String.prototype.toTitleCase = function (): string {
	let i: number;
	let j: number;
	let str: string;

	str = this.replace(/([^\W_][^\s-]*) */gu, (txt: string) => {
		return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();
	});

	// Certain minor words should be left lowercase unless
	// they are the first or last words in the string

	// ['a', 'for', 'so', 'an', 'in', 'the', 'and', 'nor', 'to', 'at', 'of', 'up', 'but', 'on', 'yet', 'by', 'or'];
	const lowers = [
		'A',
		'An',
		'The',
		'And',
		'But',
		'Or',
		'For',
		'Nor',
		'As',
		'At',
		'By',
		'For',
		'From',
		'In',
		'Into',
		'Near',
		'Of',
		'On',
		'Onto',
		'To',
		'With',
	];
	for (i = 0, j = lowers.length; i < j; i++) {
		str = str.replace(new RegExp(`\\s${lowers[i]}\\s`, 'gu'), (txt) => {
			return txt.toLowerCase();
		});
	}

	// Certain words such as initialisms or acronyms should be left uppercase
	const uppers = ['Id', 'Tv'];
	for (i = 0, j = uppers.length; i < j; i++) {
		str = str.replace(
			new RegExp(`\\b${uppers[i]}\\b`, 'gu'),
			uppers[i].toUpperCase(),
		);
	}

	return str;
};

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
