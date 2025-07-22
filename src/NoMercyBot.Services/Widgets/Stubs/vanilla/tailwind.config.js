/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts}",
    "./*.{js,ts}",
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
    },
  },
  plugins: [],
}

