# Healthcare Dashboard UI - Complete Setup & Usage Guide

## Overview
This is a production-ready Healthcare Dashboard built with **React** and **Tailwind CSS**. It features a responsive layout with a collapsible sidebar, dynamic content switching, and full API integration capabilities.

---

## 📦 Project Structure

```
healthcare-dashboard/
├── src/
│   ├── components/
│   │   ├── HealthcareDashboard.jsx      # Main dashboard component
│   │   ├── DashboardView.jsx            # Dashboard overview page
│   │   ├── PatientsView.jsx             # Patients management page
│   │   ├── AppointmentsView.jsx         # Appointments page
│   │   └── ReportsView.jsx              # Reports page
│   ├── services/
│   │   └── apiService.js                # API integration service
│   ├── styles/
│   │   └── globals.css                  # Global styles
│   ├── App.jsx                          # Root component
│   └── index.jsx                        # Entry point
├── public/
│   └── index.html
├── tailwind.config.js                   # Tailwind configuration
├── postcss.config.js                    # PostCSS configuration
├── package.json                         # Dependencies
└── .env.example                         # Environment variables template
```

---

## 🚀 Installation & Setup

### Step 1: Create a New React Project
```bash
npm create vite@latest healthcare-dashboard -- --template react
cd healthcare-dashboard
npm install
```

### Step 2: Install Required Dependencies
```bash
npm install tailwindcss postcss autoprefixer lucide-react axios
npx tailwindcss init -p
```

### Step 3: Copy Project Files
1. Copy `HealthcareDashboard.jsx` to `src/components/`
2. Copy `apiService.js` to `src/services/`
3. Copy `tailwind.config.js` to the project root
4. Copy `SETUP_GUIDE.md` to the project root

### Step 4: Configure Environment Variables
Create a `.env` file in the project root:
```env
REACT_APP_API_URL=http://localhost:3000/api
REACT_APP_ENV=development
```

### Step 5: Update App.jsx
```jsx
import HealthcareDashboard from './components/HealthcareDashboard';
import './styles/globals.css';

function App() {
  return <HealthcareDashboard />;
}

export default App;
```

### Step 6: Create Global Styles
Create `src/styles/globals.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: 'Inter', 'Roboto', 'Open Sans', sans-serif;
  background-color: #f8f9fa;
  color: #212529;
}

html, body, #root {
  height: 100%;
  width: 100%;
}
```

### Step 7: Run the Development Server
```bash
npm run dev
```

The dashboard will be available at `http://localhost:5173`

---

## 🎨 Medical Blue Theme - Color Palette

### Primary Colors
| Color | Hex Code | Usage |
| :--- | :--- | :--- |
| **Sapphire Blue** | `#0F52BA` | Primary buttons, sidebar, main accents |
| **Medical Blue** | `#007BFF` | Secondary elements, links, charts |
| **Success Green** | `#28A745` | Positive indicators, success states |
| **Warning Yellow** | `#FFC107` | Warnings, alerts |
| **Critical Red** | `#DC3545` | Errors, critical alerts |

### Neutral Colors
| Color | Hex Code | Usage |
| :--- | :--- | :--- |
| **White** | `#FFFFFF` | Cards, backgrounds |
| **Light Gray** | `#F8F9FA` | Page background |
| **Primary Text** | `#212529` | Main text content |
| **Muted Text** | `#6C757D` | Secondary text, labels |

### Tailwind CSS Color Classes
```jsx
// Primary
className="bg-blue-600"        // #0F52BA
className="text-blue-600"
className="border-blue-600"

// Secondary
className="bg-cyan-500"        // #007BFF
className="text-cyan-500"

// Success
className="bg-green-600"       // #28A745
className="text-green-600"

// Warning
className="bg-yellow-400"      // #FFC107
className="text-yellow-400"

// Danger
className="bg-red-600"         // #DC3545
className="text-red-600"
```

---

## 🔌 API Integration

### Using the API Service
The `apiService.js` file provides pre-built methods for all common healthcare operations:

#### Get All Patients
```jsx
import { patientService } from './services/apiService';

const fetchPatients = async () => {
  try {
    const patients = await patientService.getAllPatients();
    console.log(patients);
  } catch (error) {
    console.error('Error fetching patients:', error);
  }
};
```

#### Create a New Patient
```jsx
const newPatient = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com',
  phone: '(555) 123-4567',
  dateOfBirth: '1990-01-15',
  gender: 'Male',
};

const patient = await patientService.createPatient(newPatient);
```

#### Book an Appointment
```jsx
import { appointmentService } from './services/apiService';

const appointmentData = {
  patientId: 'PAT-0001',
  doctorId: 'DOC-0001',
  date: '2025-06-15',
  time: '10:00 AM',
  reason: 'Regular checkup',
};

const appointment = await appointmentService.bookAppointment(appointmentData);
```

#### Search Patients
```jsx
const results = await patientService.searchPatients('John Doe');
```

### Available API Methods

#### Patient Service
- `getAllPatients(filters)` - Get all patients
- `getPatientById(patientId)` - Get single patient
- `createPatient(patientData)` - Create new patient
- `updatePatient(patientId, patientData)` - Update patient
- `deletePatient(patientId)` - Delete patient
- `searchPatients(query)` - Search patients

#### Appointment Service
- `getAllAppointments(filters)` - Get all appointments
- `getPatientAppointments(patientId)` - Get patient's appointments
- `bookAppointment(appointmentData)` - Book new appointment
- `rescheduleAppointment(appointmentId, newData)` - Reschedule
- `cancelAppointment(appointmentId)` - Cancel appointment
- `getAvailableSlots(doctorId, date)` - Get available time slots

#### Medical Record Service
- `getPatientRecords(patientId)` - Get patient records
- `getRecordById(recordId)` - Get single record
- `createRecord(patientId, recordData)` - Create record
- `uploadDocument(patientId, file)` - Upload medical document

#### Dashboard Service
- `getKPIData()` - Get KPI statistics
- `getPatientTrends(period)` - Get trend data
- `getAppointmentStats()` - Get appointment stats
- `getRevenueData(period)` - Get revenue data

---

## 🎯 Key Features

### 1. Responsive Sidebar Navigation
- Collapsible sidebar with smooth animations
- Icons from Lucide React
- Active menu highlighting
- Support for mobile hamburger menu

### 2. Dynamic Content Switching
- Seamless switching between different modules
- Dashboard, Patients, Appointments, Reports
- No page reloads

### 3. KPI Dashboard Cards
- Real-time statistics display
- Trend indicators
- Color-coded status badges

### 4. Data Visualization
- Appointment trends chart
- Patient distribution pie chart
- Responsive charts that adapt to screen size

### 5. Patient Management Table
- Searchable patient list
- Sortable columns
- Action buttons for view/edit
- Status badges with color coding

### 6. Top Header
- Global search functionality
- Notifications bell
- Messages indicator
- User profile dropdown

---

## 🔐 Authentication

### Setup Authentication
Update your `apiService.js` with your authentication logic:

```jsx
// In apiService.js
const apiCall = async (endpoint, options = {}) => {
  try {
    const token = localStorage.getItem('authToken');
    
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        ...options.headers,
      },
      ...options,
    });

    if (response.status === 401) {
      // Token expired, refresh or redirect to login
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }

    return await response.json();
  } catch (error) {
    handleError(error);
  }
};
```

### Login Flow
```jsx
import { authService } from './services/apiService';

const handleLogin = async (email, password) => {
  try {
    const response = await authService.login(email, password);
    console.log('Login successful:', response);
    // Redirect to dashboard
  } catch (error) {
    console.error('Login failed:', error);
  }
};
```

---

## 🛠️ Customization

### Changing Colors
Edit `tailwind.config.js`:
```js
theme: {
  extend: {
    colors: {
      medical: {
        primary: '#0F52BA',      // Change primary color
        secondary: '#007BFF',    // Change secondary color
        // ... other colors
      }
    }
  }
}
```

### Adding New Menu Items
In `HealthcareDashboard.jsx`:
```jsx
const menuItems = [
  { id: 'dashboard', label: 'Dashboard', icon: Home },
  { id: 'patients', label: 'Patients', icon: Users },
  // Add new item here
  { id: 'newFeature', label: 'New Feature', icon: NewIcon },
];
```

### Creating New Views
1. Create a new component file (e.g., `NewFeatureView.jsx`)
2. Add it to the `renderContent()` switch statement
3. Add corresponding menu item

---

## 📱 Responsive Design

The dashboard is fully responsive:
- **Desktop**: Full sidebar + content area
- **Tablet**: Sidebar collapses to icons
- **Mobile**: Hamburger menu with full-screen overlay

### Breakpoints
```css
sm: 640px
md: 768px
lg: 1024px
xl: 1280px
2xl: 1536px
```

---

## 🚨 Error Handling

The API service includes built-in error handling:

```jsx
try {
  const data = await patientService.getAllPatients();
} catch (error) {
  console.error('Error:', error.message);
  // Show error toast/notification to user
}
```

---

## 📊 Performance Optimization

### Code Splitting
```jsx
import { lazy, Suspense } from 'react';

const DashboardView = lazy(() => import('./DashboardView'));

<Suspense fallback={<Loading />}>
  <DashboardView />
</Suspense>
```

### Memoization
```jsx
import { memo } from 'react';

const PatientCard = memo(({ patient }) => {
  return <div>{patient.name}</div>;
});
```

---

## 🧪 Testing

### Unit Testing Example
```jsx
import { render, screen } from '@testing-library/react';
import HealthcareDashboard from './components/HealthcareDashboard';

test('renders dashboard title', () => {
  render(<HealthcareDashboard />);
  expect(screen.getByText('Dashboard')).toBeInTheDocument();
});
```

---

## 📚 Additional Resources

- [React Documentation](https://react.dev)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Lucide React Icons](https://lucide.dev)
- [Vite Documentation](https://vitejs.dev)

---

## 🤝 Support

For issues or questions:
1. Check the documentation above
2. Review the code comments
3. Check the API service methods
4. Test with mock data first

---

## 📄 License

This Healthcare Dashboard UI is provided as-is for educational and commercial use.

---

**Happy coding! 🏥💙**
