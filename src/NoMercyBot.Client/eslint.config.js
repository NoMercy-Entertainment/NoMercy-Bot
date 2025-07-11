import antfu from '@antfu/eslint-config';

export default antfu({
	vue: {
		a11y: true,
		overrides: {
			'prefer-regex-literals': 'off',
			'regexp/prefer-w': 'off',
			'vue-a11y/no-distracting-elements': 'off',
			'vue/first-attribute-linebreak': 'off',
			'vue/multi-word-component-names': 'off',
			'vue/no-deprecated-slot-attribute': 'off',
			'vue/no-reserved-component-names': 'off',
		},
	},
	typescript: {
		overrides: {
			'@typescript-eslint/ban-ts-comment': 'off',
			'antfu/no-top-level-await': 'off',
			'antfu/top-level-function': 'off',
			'no-async-promise-executor': 'off',
			'no-console': 'off',
			'node/prefer-global/process': 'off',
			'perfectionist/sort-imports': 'off',
			'ts/consistent-type-definitions': 'off',
			'ts/no-unsafe-function-type': 'off',
		},
	},
	js: {
		overrides: {
			'no-console': 'off',
			'no-debugger': 'off',
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
		markdown: true,
		svg: false,
	},
});

// module.exports = {
//   root: true,
//   env: {
//     node: true
//   },
//   'extends': [
//     'plugin:vue/vue3-essential',
//     'eslint:recommended',
//     '@vue/typescript/recommended',
//   ],
//   parserOptions: {
//     ecmaVersion: 2020
//   },
//   rules: {
//     'no-console': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
//     'no-debugger': process.env.NODE_ENV === 'production' ? 'warn' : 'off',
//     'vue/no-deprecated-slot-attribute': 'off',
//     'vue/multi-word-component-names': 'off',
//     'vue/no-reserved-component-names': 'off',
//     '@typescript-eslint/no-explicit-any': 'off',
//     '@typescript-eslint/ban-ts-comment': 'off',
//   }
// }
