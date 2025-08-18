# UI/UX Design Specification - Phase 1 Equipment Scheduling

## 1. Design Foundation

### Design Objectives
- **Primary Goal**: Enable equipment scheduling in under 30 minutes
- **Simplicity First**: Maximum 3 clicks for any common task
- **Visual Clarity**: Status visible at a glance without interpretation
- **Progressive Disclosure**: Complex features hidden until needed
- **Mobile Responsive**: Core functions work on all devices

### Target Users & Contexts
| User Type | Frequency | Primary Device | Key Tasks |
|-----------|-----------|----------------|-----------|
| Operations Manager | Daily | Desktop | Monitor status, handle exceptions |
| Industrial Engineer | Weekly | Desktop | Design patterns, assignments |
| System Admin | Monthly | Desktop | Equipment setup, integration |
| OEE System | Continuous | API | Query availability data |

### Platform Requirements
- **Primary**: Desktop web (1024px+) - Full functionality
- **Secondary**: Tablet (768px+) - View and basic edits
- **Tertiary**: Mobile (320px+) - View only, emergency access
- **Browsers**: Chrome 90+, Firefox 88+, Safari 14+, Edge 90+

### Accessibility Standards
- **WCAG 2.1 Level AA** compliance required
- **Color Contrast**: 4.5:1 for normal text, 3:1 for large text
- **Keyboard Navigation**: All functions accessible via keyboard
- **Screen Readers**: Semantic HTML with ARIA labels
- **Focus Indicators**: 2px outline with high contrast

### Visual Design System

#### Color Palette
```
Primary Colors:
├── Primary Blue: #0B5394 (Navigation, primary actions)
├── Success Green: #28A745 (Operating status, confirmations)
├── Warning Amber: #FFC107 (Maintenance, attention needed)
├── Error Red: #DC3545 (Breakdowns, critical issues)
├── Neutral Gray: #6C757D (Disabled, inactive states)

Status Colors:
├── Operating: #28A745 (Green)
├── Maintenance: #FFC107 (Amber)
├── Breakdown: #DC3545 (Red)
├── Holiday: #17A2B8 (Teal)
├── Not Scheduled: #6C757D (Gray)

Background Colors:
├── Page Background: #F8F9FA (Light gray)
├── Card Background: #FFFFFF (White)
├── Hover State: #E9ECEF (Light hover)
├── Selected State: #D1ECF1 (Light blue)
```

#### Typography Scale
```
Font Family: Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif

Headings:
├── H1: 32px, Bold (700), 1.2 line-height
├── H2: 24px, SemiBold (600), 1.3 line-height
├── H3: 20px, SemiBold (600), 1.4 line-height
├── H4: 18px, Medium (500), 1.4 line-height

Body Text:
├── Large: 16px, Regular (400), 1.6 line-height
├── Normal: 14px, Regular (400), 1.5 line-height
├── Small: 12px, Regular (400), 1.4 line-height
├── Caption: 11px, Medium (500), 1.3 line-height

Special Text:
├── Equipment ID: 13px, Monospace, Medium (500)
├── Status Badge: 11px, SemiBold (600), Uppercase
├── Data Values: 14px, Monospace, Regular (400)
```

#### Spacing System
```
Base Unit: 4px

Spacing Scale:
├── xs: 4px
├── sm: 8px
├── md: 16px
├── lg: 24px
├── xl: 32px
├── 2xl: 48px
├── 3xl: 64px

Component Spacing:
├── Card Padding: 24px
├── Button Padding: 12px 16px
├── Input Padding: 8px 12px
├── Table Cell Padding: 12px
├── Section Margin: 32px
```

## 2. Information Architecture

### Application Structure
```
Equipment Scheduling System
├── Dashboard (Landing page, system overview)
├── Equipment
│   ├── Hierarchy View (ISA-95 tree)
│   ├── List View (Tabular display)
│   └── Import (Bulk upload)
├── Patterns
│   ├── Library (5 simple patterns)
│   ├── Assignments (Current assignments)
│   └── Custom (Create custom pattern)
├── Schedules
│   ├── Generator (Create schedules)
│   ├── Calendar View (Visual schedule)
│   └── Exceptions (Maintenance, breakdowns)
├── Integration
│   ├── API Status (Connection health)
│   ├── Export (Download schedules)
│   └── Settings (API configuration)
└── Settings
    ├── Calendars (Holiday management)
    ├── Users (Access control)
    └── System (Configuration)
```

### Navigation Patterns

#### Primary Navigation (Left Sidebar)
- Width: 240px expanded, 64px collapsed
- Fixed position with scroll
- Icon + Label format
- Active state with left border
- Badge indicators for counts/status

#### Secondary Navigation (Page Tabs)
- Horizontal tabs within sections
- Maximum 5 tabs per section
- Active state with bottom border
- Tab overflow handled with dropdown

#### Breadcrumb Navigation
- Shows hierarchical location
- Clickable for navigation
- Format: Home > Equipment > Hierarchy

### Content Organization

#### Dashboard Layout
```
┌─────────────────────────────────────────────────┐
│                  Header Bar                      │
├────────┬─────────────────────────────────────────┤
│        │  ┌──────────────────────────────────┐  │
│  Side  │  │      Summary Cards (4)           │  │
│  bar   │  ├──────────────────────────────────┤  │
│        │  │   Quick Actions    │   Status    │  │
│        │  │                    │   Chart     │  │
│        │  ├──────────────────────────────────┤  │
│        │  │        Recent Activity           │  │
│        │  └──────────────────────────────────┘  │
└────────┴─────────────────────────────────────────┘
```

#### Standard Page Layout
```
┌─────────────────────────────────────────────────┐
│                  Header Bar                      │
├────────┬─────────────────────────────────────────┤
│        │  Page Title            Actions Toolbar  │
│  Side  │  ┌──────────────────────────────────┐  │
│  bar   │  │        Tab Navigation            │  │
│        │  ├──────────────────────────────────┤  │
│        │  │                                  │  │
│        │  │        Main Content Area         │  │
│        │  │                                  │  │
│        │  └──────────────────────────────────┘  │
└────────┴─────────────────────────────────────────┘
```

## 3. Interface Components

### Component Library

#### Layout Components

**LC-001: Header Bar**
- Height: 64px
- Background: White
- Shadow: 0 1px 3px rgba(0,0,0,0.1)
- Contents: Logo, Search, User Menu, Notifications
- Fixed position top

**LC-002: Sidebar Navigation**
- Width: 240px (expanded), 64px (collapsed)
- Background: #F8F9FA
- Border-right: 1px solid #DEE2E6
- Toggle button at bottom
- Scrollable with fixed header/footer

**LC-003: Page Container**
- Max-width: 1400px
- Padding: 24px
- Background: White
- Border-radius: 8px
- Shadow: 0 1px 3px rgba(0,0,0,0.05)

#### Data Display Components

**DD-001: Summary Card**
```
Dimensions: 280px × 120px
Structure:
┌────────────────────────┐
│ Label          Icon    │
│                        │
│ 42             →+12%   │
│ Large Value    Change  │
└────────────────────────┘

Styling:
- Background: White
- Border: 1px solid #DEE2E6
- Border-radius: 8px
- Padding: 20px
- Hover: Shadow elevation
```

**DD-002: Data Table**
```
Row Height: 48px
Header Height: 56px
Features:
- Sortable columns (arrow indicators)
- Checkboxes for selection
- Status badges in cells
- Hover highlight
- Zebra striping optional
- Pagination at bottom
```

**DD-003: Equipment Tree Node**
```
Height: 40px
Structure:
[▼] [Icon] Equipment Name        [Badge] [Actions]
    └─ Indent: 24px per level

States:
- Default: White background
- Hover: #F8F9FA background
- Selected: #D1ECF1 background
- Has Pattern: Blue dot indicator
- Override: Orange dot indicator
```

**DD-004: Pattern Card**
```
Dimensions: 320px × 200px
Structure:
┌─────────────────────────┐
│ Pattern Name        ⋮   │
├─────────────────────────┤
│ Visual Timeline         │
│ ████████░░░░████████    │
├─────────────────────────┤
│ Coverage: 16 hrs/day    │
│ Days: Mon-Fri           │
│ Type: Two-Shift         │
└─────────────────────────┘

Interaction:
- Click to select
- Hover for details
- Drag to assign
```

#### Form Components

**FC-001: Button Primary**
- Height: 40px
- Padding: 12px 24px
- Background: #0B5394
- Color: White
- Border-radius: 6px
- Hover: Darken 10%
- Disabled: Opacity 0.5

**FC-002: Button Secondary**
- Height: 40px
- Padding: 12px 24px
- Background: White
- Color: #0B5394
- Border: 1px solid #0B5394
- Border-radius: 6px
- Hover: Light blue background

**FC-003: Input Field**
- Height: 40px
- Padding: 8px 12px
- Border: 1px solid #CED4DA
- Border-radius: 4px
- Focus: Blue border + shadow
- Error: Red border
- Placeholder: #6C757D

**FC-004: Select Dropdown**
- Height: 40px
- Padding: 8px 12px
- Border: 1px solid #CED4DA
- Border-radius: 4px
- Arrow: Chevron down
- Open: Blue border

**FC-005: Date Picker**
- Height: 40px
- Calendar icon right
- Popup calendar
- Range selection support
- Quick presets (Today, This Week, etc.)

#### Feedback Components

**FB-001: Toast Notification**
```
Position: Top-right
Width: 320px
Auto-dismiss: 5 seconds
Types:
├── Success: Green border/icon
├── Warning: Amber border/icon
├── Error: Red border/icon
├── Info: Blue border/icon
```

**FB-002: Loading Spinner**
- Size: 24px default, 16px small, 32px large
- Color: Primary blue
- Animation: 1.5s rotation
- Center in container

**FB-003: Empty State**
```
Structure:
┌─────────────────────┐
│                     │
│     [Icon/Image]    │
│                     │
│    No Data Found    │
│  Try adjusting...   │
│                     │
│   [Action Button]   │
│                     │
└─────────────────────┘
```

**FB-004: Progress Bar**
- Height: 8px
- Background: #E9ECEF
- Fill: Primary blue
- Animated fill
- Text overlay optional

### Modal Dialogs

**MD-001: Standard Modal**
```
Max-width: 600px
Structure:
┌──────────────────────────────┐
│ Title                    X   │
├──────────────────────────────┤
│                              │
│         Content Area         │
│                              │
├──────────────────────────────┤
│        [Cancel] [Confirm]    │
└──────────────────────────────┘

Overlay: Black 50% opacity
Animation: Fade in 200ms
```

**MD-002: Confirmation Dialog**
```
Max-width: 400px
Structure:
┌──────────────────────────────┐
│ ⚠️ Confirm Action            │
├──────────────────────────────┤
│ Are you sure you want to     │
│ delete this pattern?         │
│                              │
│ This action cannot be undone.│
├──────────────────────────────┤
│        [Cancel] [Delete]     │
└──────────────────────────────┘
```

## 4. User Experience Flows

### Primary User Journeys

#### Journey 1: Initial Setup (System Admin - 30 minutes)

**Step 1: Equipment Import**
```
Screen Flow:
1. Dashboard → Equipment → Import
2. Download template button
3. Fill Excel template offline
4. Drag & drop file to upload zone
5. Preview validation results
6. Fix errors if any
7. Confirm import
8. Success: "847 equipment items imported"

Key UI Elements:
- Drag-drop zone with visual feedback
- Progress bar during processing
- Validation table with error highlights
- Success notification with summary
```

**Step 2: Pattern Assignment**
```
Screen Flow:
1. Equipment → Hierarchy View
2. Select Enterprise node
3. Patterns panel opens on right
4. Drag "Two-Shift" pattern to Enterprise
5. Confirmation: "Apply to 847 items?"
6. Click "Apply"
7. Tree updates with pattern badges
8. Summary: "847 items scheduled"

Key Interactions:
- Drag-and-drop visual feedback
- Inheritance visualization (dotted lines)
- Real-time badge updates
- Undo option available
```

**Step 3: Schedule Generation**
```
Screen Flow:
1. Schedules → Generator
2. Set date range (Next 12 months)
3. Preview affected equipment
4. Click "Generate Schedules"
5. Progress bar with equipment count
6. Success: "Schedules generated"
7. Auto-redirect to Calendar View

Key Features:
- Date range picker with presets
- Live preview of impact
- Batch progress indication
- Automatic validation
```

#### Journey 2: Daily Operations (Operations Manager - 5 minutes)

**Dashboard Check**
```
Visual Priority:
1. Status Cards (immediate visibility)
   - 847 Equipment Scheduled ✓
   - 2 Maintenance Today ⚠️
   - API Connected ✓
   - Last Update: 2 min ago

2. Quick Actions (one-click access)
   - Handle Exception
   - Regenerate Today
   - View Calendar
   - Export Data

3. Activity Feed (recent changes)
   - "Line A: Maintenance started"
   - "Schedule updated for Line B"
   - "Holiday calendar applied"
```

**Exception Handling**
```
Screen Flow:
1. Alert: "Line B breakdown reported"
2. Click notification → Exception form
3. Select: Duration (Today only / Multiple days)
4. Choose: Impact (Complete shutdown / Partial)
5. Add: Notes (optional)
6. Submit → Schedules update
7. API notification sent
8. Confirmation: "OEE systems updated"

Form Design:
- Pre-filled with equipment context
- Smart defaults based on history
- Visual timeline showing impact
- One-click common scenarios
```

#### Journey 3: Pattern Customization (Industrial Engineer - 10 minutes)

**Custom Pattern Creation**
```
Screen Flow:
1. Patterns → Custom
2. Name pattern: "Weekend Maintenance"
3. Visual editor opens
4. Click-drag on timeline grid:
   Mon-Fri: OFF
   Sat: 08:00-16:00
   Sun: OFF
5. Preview weekly hours: 8 hrs/week
6. Save pattern
7. Pattern appears in library

Editor Features:
- 24×7 grid (hourly resolution)
- Click to toggle on/off
- Drag to select ranges
- Copy day to other days
- Visual preview updates live
```

### Error Handling Patterns

#### Validation Errors
```
Display:
- Field-level: Red border + message below
- Form-level: Error summary at top
- Inline help: Tooltip with guidance

Example:
"Pattern must have at least 1 operating hour"
[!] Input field with red border
    Helper text explains requirement
```

#### System Errors
```
Network Error:
┌─────────────────────────────────┐
│ ⚠️ Connection Lost              │
│                                 │
│ Unable to reach server.         │
│ Your changes have been saved    │
│ locally and will sync when      │
│ connection is restored.         │
│                                 │
│         [Retry Now]             │
└─────────────────────────────────┘
```

#### Permission Errors
```
Access Denied:
┌─────────────────────────────────┐
│ 🔒 Permission Required          │
│                                 │
│ You don't have permission to    │
│ modify patterns. Contact your   │
│ administrator for access.       │
│                                 │
│    [Request Access] [Cancel]    │
└─────────────────────────────────┘
```

## 5. Responsive Design

### Breakpoint Strategy
```
Desktop:  1024px and up    - Full functionality
Tablet:   768px to 1023px  - Adapted layout
Mobile:   320px to 767px   - View only
```

### Desktop Layout (1024px+)
```
Features:
- Sidebar always visible
- Multi-column layouts
- Drag-and-drop enabled
- All features available
- Hover states active
```

### Tablet Layout (768px-1023px)
```
Adaptations:
- Sidebar collapses to icons
- Single column forms
- Touch-friendly targets (44px min)
- Simplified tables (horizontal scroll)
- Bottom navigation bar
```

### Mobile Layout (320px-767px)
```
Limitations:
- View-only mode
- Sidebar hidden (hamburger menu)
- Stack all content vertically
- Critical actions only
- No drag-and-drop
- Simplified data display
```

### Component Responsive Behavior

**Navigation Sidebar**
- Desktop: 240px fixed left
- Tablet: 64px icon-only
- Mobile: Hidden, hamburger menu

**Data Tables**
- Desktop: All columns visible
- Tablet: Horizontal scroll
- Mobile: Card layout

**Pattern Cards**
- Desktop: 3-4 columns
- Tablet: 2 columns
- Mobile: Single column

**Forms**
- Desktop: Multi-column
- Tablet: Single column
- Mobile: Single column, stacked

## 6. Interaction Patterns

### Drag and Drop
```
Supported Operations:
1. Pattern → Equipment (assignment)
2. File → Upload zone (import)
3. Timeline segments (pattern creation)

Visual Feedback:
- Drag source: 50% opacity
- Valid target: Green border
- Invalid target: Red border
- Drop preview: Ghost image
```

### Keyboard Navigation
```
Tab Order:
- Logical left-to-right, top-to-bottom
- Skip to main content link
- Focus trap in modals

Shortcuts:
- Ctrl/Cmd + S: Save
- Ctrl/Cmd + N: New item
- Escape: Cancel/close
- Enter: Submit form
- Space: Toggle selection
```

### Loading States
```
Inline Loading:
- Replace content with spinner
- Maintain container dimensions
- Show progress if > 2 seconds

Page Loading:
- Full-page spinner overlay
- Progress bar at top
- Skeleton screens for structure
```

### Validation Timing
```
Strategy:
- On blur: Individual field validation
- On submit: Full form validation
- Real-time: Character limits, format
- Async: Uniqueness checks

Feedback:
- Immediate: Format errors
- Delayed: Server validation
- Success: Green checkmark
```

## 7. Implementation Specifications

### CSS Framework
```
Recommended: Tailwind CSS
Alternatives: Bootstrap 5, Material-UI

Custom Properties:
--color-primary: #0B5394;
--color-success: #28A745;
--color-warning: #FFC107;
--color-error: #DC3545;
--spacing-unit: 4px;
--border-radius: 6px;
--transition-speed: 200ms;
```

### Component States
```
All Interactive Elements:
- Default (idle state)
- Hover (desktop only)
- Focus (keyboard navigation)
- Active (being clicked)
- Disabled (not available)
- Loading (processing)
- Error (validation failed)
- Success (action completed)
```

### Animation Guidelines
```
Transitions:
- Duration: 200-300ms
- Easing: ease-in-out
- Properties: opacity, transform
- Avoid: color, width changes

Page Transitions:
- Fade: 200ms
- Slide: 300ms
- No animation > 500ms
```

### Accessibility Checklist
- [ ] All images have alt text
- [ ] Form labels associated with inputs
- [ ] Error messages linked to fields
- [ ] Keyboard navigation complete
- [ ] Focus indicators visible
- [ ] Color not sole indicator
- [ ] Screen reader tested
- [ ] Contrast ratios met

## 8. Phase 2 Preparation

### Hidden UI Modules
These components are built but hidden in Phase 1:

```
Employee Section:
├── Employee list table
├── Skill matrix grid
├── Team assignment interface
├── Coverage calendar
└── Shift swap requests

Advanced Patterns:
├── Complex pattern designer
├── Team rotation visualizer
├── Coverage analyzer
└── Optimization dashboard
```

### Feature Flag Controls
```javascript
// UI Feature Configuration
const features = {
  employeeModule: false,      // Hidden in Phase 1
  advancedPatterns: false,    // Hidden in Phase 1
  coverageAnalysis: false,    // Hidden in Phase 1
  apiV2Endpoints: false,      // Hidden in Phase 1
};

// Phase 2 Activation
if (features.employeeModule) {
  showMenuItem('Employees');
  enableEmployeeRoutes();
}
```

### Upgrade Path UX
When Phase 2 activates:
1. New menu items appear
2. Pattern library expands
3. Additional tabs show
4. New dashboard widgets
5. No layout restructuring
6. Smooth transition

This specification provides complete implementation guidance for Phase 1 while ensuring the interface can gracefully expand for Phase 2 without redesign.