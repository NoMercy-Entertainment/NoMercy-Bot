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
        markdown: true,
        svg: false,
    },
});
