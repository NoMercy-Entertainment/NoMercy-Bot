export default {
  plugins: {
    tailwindcss: {},
    autoprefixer: {},
  },
}
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      spacing: {
        'available': 'calc(100% - theme(spacing.6))',
      },
      colors: {
        'theme': {
          300: 'var(--color-300, #a1a1aa)',
          500: 'var(--color-500, #71717a)',
          700: 'var(--color-700, #3f3f46)',
        },
      },
      animation: {
        'shine': 'shine 12s forwards',
      },
      keyframes: {
        shine: {
          '1%': { transform: 'translateX(100%)' },
          '15%, 100%': { transform: 'translateX(-100%)' },
        }
      }
    },
  },
  plugins: [],
}

