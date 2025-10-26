/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    '../**/*.razor',
    '../**/*.cshtml',
    '../**/*.html'
  ],
  theme: {
    extend: {
      // Custom color palette for text editor
      colors: {
        'editor': {
          'bg': '#1e1e1e',
          'fg': '#d4d4d4',
          'selection': '#264f78',
          'line-highlight': '#2a2a2a',
        },
        'tab': {
          'active': '#1e1e1e',
          'inactive': '#2d2d2d',
          'hover': '#383838',
        }
      },
      // Font families for code editing
      fontFamily: {
        'mono': ['Monaco', 'Menlo', 'Consolas', 'Courier New', 'monospace'],
      },
    },
  },
  plugins: [],
}
