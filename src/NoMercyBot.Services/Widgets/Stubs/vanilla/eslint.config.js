import antfu from '@antfu/eslint-config';

export default antfu({
	typescript: false,
	js: {
		overrides: {
			'prefer-regex-literals': 'off',
			'regexp/prefer-w': 'off',
		},
	},
	stylistic: {
		indent: 'tab',
		quotes: 'single',
		semi: true,
	},
	formatters: {
		css: true,
		html: true,
	},
});
