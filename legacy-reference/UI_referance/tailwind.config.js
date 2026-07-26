/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Medical Blue Theme
        medical: {
          primary: '#0F52BA',      // Sapphire Blue
          secondary: '#007BFF',    // Medical Blue
          success: '#28A745',      // Success Green
          warning: '#FFC107',      // Warning Yellow
          danger: '#DC3545',       // Critical Red
          background: '#F8F9FA',   // Light Gray
          text: '#212529',         // Primary Text
          textMuted: '#6C757D',    // Muted Text
        },
        // Extended color palette
        blue: {
          50: '#f0f7ff',
          100: '#e0f2fe',
          200: '#bae6fd',
          300: '#7dd3fc',
          400: '#38bdf8',
          500: '#0ea5e9',
          600: '#0284c7',
          700: '#0369a1',
          800: '#075985',
          900: '#0c3d66',
          950: '#082f49',
        },
        cyan: {
          50: '#f0f9ff',
          100: '#e0f7ff',
          200: '#cff9ff',
          300: '#a5f3ff',
          400: '#67e8f9',
          500: '#06b6d4',
          600: '#0891b2',
          700: '#0e7490',
          800: '#155e75',
          900: '#164e63',
          950: '#082f49',
        },
      },
      fontFamily: {
        sans: ['Inter', 'Roboto', 'Open Sans', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        xs: ['12px', { lineHeight: '16px' }],
        sm: ['14px', { lineHeight: '20px' }],
        base: ['16px', { lineHeight: '24px' }],
        lg: ['18px', { lineHeight: '28px' }],
        xl: ['20px', { lineHeight: '28px' }],
        '2xl': ['24px', { lineHeight: '32px' }],
        '3xl': ['30px', { lineHeight: '36px' }],
      },
      boxShadow: {
        sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        DEFAULT: '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        md: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
        xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
      },
      borderRadius: {
        none: '0',
        sm: '0.125rem',
        DEFAULT: '0.25rem',
        md: '0.375rem',
        lg: '0.5rem',
        xl: '0.75rem',
        '2xl': '1rem',
        '3xl': '1.5rem',
        full: '9999px',
      },
      spacing: {
        0: '0px',
        1: '0.25rem',
        2: '0.5rem',
        3: '0.75rem',
        4: '1rem',
        5: '1.25rem',
        6: '1.5rem',
        8: '2rem',
        10: '2.5rem',
        12: '3rem',
        16: '4rem',
        20: '5rem',
        24: '6rem',
      },
    },
  },
  plugins: [
    // Custom plugin for healthcare-specific utilities
    function ({ addUtilities }) {
      const newUtilities = {
        '.card-hover': {
          '@apply transition-all duration-300 hover:shadow-lg hover:scale-105': {},
        },
        '.btn-primary': {
          '@apply px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium': {},
        },
        '.btn-secondary': {
          '@apply px-4 py-2 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 transition font-medium': {},
        },
        '.badge-success': {
          '@apply px-3 py-1 bg-green-100 text-green-800 rounded-full text-xs font-medium': {},
        },
        '.badge-warning': {
          '@apply px-3 py-1 bg-yellow-100 text-yellow-800 rounded-full text-xs font-medium': {},
        },
        '.badge-danger': {
          '@apply px-3 py-1 bg-red-100 text-red-800 rounded-full text-xs font-medium': {},
        },
        '.table-row-hover': {
          '@apply hover:bg-gray-50 transition': {},
        },
      };
      addUtilities(newUtilities);
    },
  ],
}
