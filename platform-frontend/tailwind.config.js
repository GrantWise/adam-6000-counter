/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ["class"],
  content: [
    "./pages/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./app/**/*.{ts,tsx}",
    "./src/**/*.{ts,tsx}",
  ],
  prefix: "",
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      // Industrial color palette optimized for factory environments
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        
        // Core brand colors
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
          50: "#f0f9ff",
          100: "#e0f2fe",
          200: "#bae6fd",
          300: "#7dd3fc",
          400: "#38bdf8",
          500: "#0ea5e9",
          600: "#0284c7",
          700: "#0369a1",
          800: "#075985",
          900: "#0c4a6e",
          950: "#082f49"
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },

        // Industrial status colors (high contrast)
        status: {
          success: {
            DEFAULT: "#16a34a", // Green - running/healthy
            hover: "#15803d",
            light: "#dcfce7"
          },
          warning: {
            DEFAULT: "#ea580c", // Orange - attention needed
            hover: "#c2410c", 
            light: "#fed7aa"
          },
          error: {
            DEFAULT: "#dc2626", // Red - fault/emergency
            hover: "#b91c1c",
            light: "#fecaca"
          },
          info: {
            DEFAULT: "#2563eb", // Blue - information
            hover: "#1d4ed8",
            light: "#dbeafe"
          },
          neutral: {
            DEFAULT: "#6b7280", // Gray - offline/unknown
            hover: "#4b5563",
            light: "#f3f4f6"
          }
        }
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" },
        },
        "fade-in": {
          from: { opacity: "0" },
          to: { opacity: "1" },
        },
        "slide-in-from-top": {
          from: { transform: "translateY(-100%)" },
          to: { transform: "translateY(0)" },
        },
        "slide-in-from-bottom": {
          from: { transform: "translateY(100%)" },
          to: { transform: "translateY(0)" },
        },
        "slide-in-from-left": {
          from: { transform: "translateX(-100%)" },
          to: { transform: "translateX(0)" },
        },
        "slide-in-from-right": {
          from: { transform: "translateX(100%)" },
          to: { transform: "translateX(0)" },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
        "fade-in": "fade-in 0.3s ease-out",
        "slide-in-from-top": "slide-in-from-top 0.3s ease-out",
        "slide-in-from-bottom": "slide-in-from-bottom 0.3s ease-out",
        "slide-in-from-left": "slide-in-from-left 0.3s ease-out",
        "slide-in-from-right": "slide-in-from-right 0.3s ease-out",
      },
      // Industrial typography scale (readable at distance)
      fontSize: {
        xs: ['0.75rem', { lineHeight: '1rem' }],     // 12px - labels, captions
        sm: ['0.875rem', { lineHeight: '1.25rem' }], // 14px - body text
        base: ['1rem', { lineHeight: '1.5rem' }],    // 16px - default body
        lg: ['1.125rem', { lineHeight: '1.75rem' }], // 18px - emphasized text
        xl: ['1.25rem', { lineHeight: '1.75rem' }],  // 20px - section headers
        '2xl': ['1.5rem', { lineHeight: '2rem' }],   // 24px - page headers
        '3xl': ['1.875rem', { lineHeight: '2.25rem' }], // 30px - dashboard titles
        '4xl': ['2.25rem', { lineHeight: '2.5rem' }]  // 36px - main headers
      },
      // Touch-friendly sizing for industrial use
      spacing: {
        '18': '4.5rem',   // 72px - large touch targets
        '22': '5.5rem',   // 88px - extra large touch targets
      }
    },
  },
  plugins: [require("tailwindcss-animate")],
}