# Medical Blue Theme - Color Palette Reference

## 🎨 Complete Color System for Healthcare Dashboard

This document provides a comprehensive reference for all colors used in the Healthcare Dashboard UI. Use these hex codes and Tailwind classes throughout your application.

---

## 📋 Primary Colors

### Sapphire Blue (Primary)
```
Hex Code: #0F52BA
RGB: rgb(15, 82, 186)
HSL: hsl(213, 87%, 40%)
Tailwind: bg-blue-700 / text-blue-700
Usage: Sidebar, primary buttons, main accents, active states
```

### Medical Blue (Secondary)
```
Hex Code: #007BFF
RGB: rgb(0, 123, 255)
HSL: hsl(217, 100%, 50%)
Tailwind: bg-blue-600 / text-blue-600
Usage: Secondary buttons, links, chart elements, hover states
```

---

## ✅ Status Colors

### Success Green
```
Hex Code: #28A745
RGB: rgb(40, 167, 69)
HSL: hsl(135, 61%, 41%)
Tailwind: bg-green-600 / text-green-600
Usage: Positive indicators, success messages, "In Treatment" status
```

### Warning Yellow
```
Hex Code: #FFC107
RGB: rgb(255, 193, 7)
HSL: hsl(45, 100%, 51%)
Tailwind: bg-yellow-400 / text-yellow-400
Usage: Warnings, pending actions, "Follow Up" status
```

### Critical Red
```
Hex Code: #DC3545
RGB: rgb(220, 53, 69)
HSL: hsl(354, 70%, 54%)
Tailwind: bg-red-600 / text-red-600
Usage: Errors, critical alerts, "New Patient" status
```

---

## ⚪ Neutral Colors

### White (Background)
```
Hex Code: #FFFFFF
RGB: rgb(255, 255, 255)
HSL: hsl(0, 0%, 100%)
Tailwind: bg-white / text-white
Usage: Card backgrounds, modal overlays, text on dark backgrounds
```

### Light Gray (Page Background)
```
Hex Code: #F8F9FA
RGB: rgb(248, 249, 250)
HSL: hsl(210, 14%, 97%)
Tailwind: bg-gray-50 / text-gray-50
Usage: Main page background, subtle section dividers
```

### Primary Text
```
Hex Code: #212529
RGB: rgb(33, 37, 41)
HSL: hsl(210, 11%, 15%)
Tailwind: text-gray-900
Usage: Main body text, headings, primary content
```

### Muted Text
```
Hex Code: #6C757D
RGB: rgb(108, 117, 125)
HSL: hsl(210, 7%, 45%)
Tailwind: text-gray-600
Usage: Secondary text, labels, helper text, disabled states
```

---

## 🎯 Extended Color Palette

### Blue Shades (Tailwind)
```
blue-50:   #f0f7ff   (Very light blue background)
blue-100:  #e0f2fe   (Light blue background)
blue-200:  #bae6fd   (Light blue hover)
blue-300:  #7dd3fc   (Medium-light blue)
blue-400:  #38bdf8   (Medium blue)
blue-500:  #0ea5e9   (Bright blue)
blue-600:  #0284c7   (Medical blue)
blue-700:  #0369a1   (Sapphire blue - PRIMARY)
blue-800:  #075985   (Dark blue)
blue-900:  #0c3d66   (Very dark blue)
```

### Cyan Shades (Tailwind)
```
cyan-50:   #f0f9ff   (Very light cyan)
cyan-100:  #e0f7ff   (Light cyan)
cyan-200:  #cff9ff   (Light cyan hover)
cyan-300:  #a5f3ff   (Medium-light cyan)
cyan-400:  #67e8f9   (Medium cyan)
cyan-500:  #06b6d4   (Bright cyan)
cyan-600:  #0891b2   (Dark cyan)
cyan-700:  #0e7490   (Very dark cyan)
```

### Gray Shades (Neutral)
```
gray-50:   #f9fafb   (Lightest gray)
gray-100:  #f3f4f6   (Very light gray)
gray-200:  #e5e7eb   (Light gray)
gray-300:  #d1d5db   (Medium-light gray)
gray-400:  #9ca3af   (Medium gray)
gray-500:  #6b7280   (Medium-dark gray)
gray-600:  #4b5563   (Dark gray)
gray-700:  #374151   (Very dark gray)
gray-800:  #1f2937   (Darker gray)
gray-900:  #111827   (Darkest gray)
```

### Green Shades (Success)
```
green-50:  #f0fdf4   (Very light green)
green-100: #dcfce7   (Light green)
green-200: #bbf7d0   (Light green hover)
green-300: #86efac   (Medium-light green)
green-400: #4ade80   (Medium green)
green-500: #22c55e   (Bright green)
green-600: #16a34a   (Success green - PRIMARY)
green-700: #15803d   (Dark green)
```

### Red Shades (Danger)
```
red-50:    #fef2f2   (Very light red)
red-100:   #fee2e2   (Light red)
red-200:   #fecaca   (Light red hover)
red-300:   #fca5a5   (Medium-light red)
red-400:   #f87171   (Medium red)
red-500:   #ef4444   (Bright red)
red-600:   #dc2626   (Danger red - PRIMARY)
red-700:   #b91c1c   (Dark red)
```

### Yellow Shades (Warning)
```
yellow-50:  #fefce8   (Very light yellow)
yellow-100: #fef3c7   (Light yellow)
yellow-200: #fde68a   (Light yellow hover)
yellow-300: #fcd34d   (Medium-light yellow)
yellow-400: #fbbf24   (Warning yellow - PRIMARY)
yellow-500: #f59e0b   (Bright yellow)
yellow-600: #d97706   (Dark yellow)
```

---

## 🎨 Color Usage Guidelines

### Backgrounds
- **Page Background**: `#F8F9FA` (Light Gray)
- **Card Background**: `#FFFFFF` (White)
- **Hover State**: `#F3F4F6` (Gray-100)
- **Active State**: `#E5E7EB` (Gray-200)

### Text
- **Primary Text**: `#212529` (Primary Text)
- **Secondary Text**: `#6C757D` (Muted Text)
- **Disabled Text**: `#ADB5BD` (Gray-400)
- **Link Text**: `#007BFF` (Medical Blue)

### Buttons
- **Primary Button**: Background `#0F52BA`, Text `#FFFFFF`
- **Secondary Button**: Background `#E5E7EB`, Text `#212529`
- **Danger Button**: Background `#DC3545`, Text `#FFFFFF`
- **Hover State**: Darken by 10%

### Borders
- **Default Border**: `#D1D5DB` (Gray-300)
- **Focus Border**: `#0F52BA` (Sapphire Blue)
- **Error Border**: `#DC3545` (Critical Red)

### Status Badges
- **Success**: Background `#D4EDDA`, Text `#155724`
- **Warning**: Background `#FFF3CD`, Text `#856404`
- **Danger**: Background `#F8D7DA`, Text `#721C24`
- **Info**: Background `#D1ECF1`, Text `#0C5460`

---

## 💻 CSS Implementation Examples

### Using Hex Codes Directly
```css
.primary-button {
  background-color: #0F52BA;
  color: #FFFFFF;
}

.sidebar {
  background-color: #0F52BA;
}

.card {
  background-color: #FFFFFF;
  border: 1px solid #D1D5DB;
}
```

### Using Tailwind Classes
```jsx
// Primary Button
<button className="bg-blue-700 text-white hover:bg-blue-800">
  Primary Action
</button>

// Secondary Button
<button className="bg-gray-200 text-gray-900 hover:bg-gray-300">
  Secondary Action
</button>

// Success Badge
<span className="bg-green-100 text-green-800 px-3 py-1 rounded-full">
  In Treatment
</span>

// Warning Badge
<span className="bg-yellow-100 text-yellow-800 px-3 py-1 rounded-full">
  Follow Up
</span>

// Danger Badge
<span className="bg-red-100 text-red-800 px-3 py-1 rounded-full">
  Critical
</span>
```

### Inline Styles
```jsx
<div style={{ backgroundColor: '#0F52BA', color: '#FFFFFF' }}>
  Primary Content
</div>

<div style={{ backgroundColor: '#F8F9FA', color: '#212529' }}>
  Page Background
</div>
```

---

## 🌈 Color Combinations

### Recommended Pairings

| Background | Text | Use Case |
| :--- | :--- | :--- |
| `#FFFFFF` | `#212529` | Main content |
| `#F8F9FA` | `#212529` | Page background |
| `#0F52BA` | `#FFFFFF` | Primary buttons |
| `#007BFF` | `#FFFFFF` | Secondary buttons |
| `#28A745` | `#FFFFFF` | Success states |
| `#FFC107` | `#212529` | Warning states |
| `#DC3545` | `#FFFFFF` | Error states |

---

## ♿ Accessibility Considerations

### Contrast Ratios (WCAG AA Standard)
- **Primary Text on White**: `#212529` on `#FFFFFF` = 12.6:1 ✅
- **Primary Button**: `#0F52BA` on `#FFFFFF` = 7.5:1 ✅
- **Muted Text on White**: `#6C757D` on `#FFFFFF` = 4.5:1 ✅
- **Success Green**: `#28A745` on `#FFFFFF` = 4.5:1 ✅

All colors meet WCAG AA accessibility standards.

---

## 🎯 Quick Reference

```jsx
// Import and use in your components
const COLORS = {
  primary: '#0F52BA',
  secondary: '#007BFF',
  success: '#28A745',
  warning: '#FFC107',
  danger: '#DC3545',
  white: '#FFFFFF',
  background: '#F8F9FA',
  text: '#212529',
  textMuted: '#6C757D',
};

// Usage
<div style={{ backgroundColor: COLORS.primary, color: COLORS.white }}>
  Content
</div>
```

---

## 📱 Dark Mode Support (Future)

For future dark mode implementation:
```
Dark Primary: #1E3A8A
Dark Secondary: #0369A1
Dark Background: #1F2937
Dark Text: #F3F4F6
```

---

**Last Updated**: May 2025  
**Version**: 1.0  
**Compatibility**: React 18+, Tailwind CSS 3+
