import antfu from '@antfu/eslint-config';

export default antfu({
    react: {
        overrides: {
            'prefer-regex-literals': 'off',
            'regexp/prefer-w': 'off',
        },
    },
    typescript: {
        overrides: {},
    },
    js: {
        overrides: {},
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
