import React, { useState } from 'react';
import {
  Home,
  Users,
  Calendar,
  FileText,
  Pill,
  DollarSign,
  MessageSquare,
  Settings,
  Bell,
  Search,
  Menu,
  X,
  ChevronDown,
  TrendingUp,
  Heart,
  Activity,
  BarChart3,
} from 'lucide-react';

// Color Palette Constants
const COLORS = {
  primary: '#0F52BA',      // Sapphire Blue
  secondary: '#007BFF',    // Medical Blue
  success: '#28A745',      // Success Green
  warning: '#FFC107',      // Warning Yellow
  danger: '#DC3545',       // Critical Red
  background: '#F8F9FA',   // Light Gray
  white: '#FFFFFF',
  text: '#212529',         // Primary Text
  textMuted: '#6C757D',    // Muted Text
};

export default function HealthcareDashboard() {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeMenu, setActiveMenu] = useState('dashboard');
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);

  // Sidebar Menu Items
  const menuItems = [
    { id: 'dashboard', label: 'Dashboard', icon: Home },
    { id: 'patients', label: 'Patients', icon: Users },
    { id: 'appointments', label: 'Appointments', icon: Calendar },
    { id: 'reports', label: 'Reports', icon: FileText },
    { id: 'medications', label: 'Medications', icon: Pill },
    { id: 'billing', label: 'Billing', icon: DollarSign },
    { id: 'messages', label: 'Messages', icon: MessageSquare },
    { id: 'settings', label: 'Settings', icon: Settings },
  ];

  // Mock Data for KPI Cards
  const kpiData = [
    { title: 'Total Patients', value: '2,543', change: '+12.5%', icon: Users, color: 'bg-blue-100' },
    { title: 'Appointments', value: '352', change: '+8.3%', icon: Calendar, color: 'bg-cyan-100' },
    { title: 'Consultations', value: '1,287', change: '+10.1%', icon: Heart, color: 'bg-red-100' },
    { title: 'Revenue', value: '$45,231', change: '+14.7%', icon: DollarSign, color: 'bg-green-100' },
  ];

  // Mock Patient Data
  const patientData = [
    { id: 'PAT-0001', name: 'Sarah Johnson', age: 34, gender: 'Female', phone: '(555) 123-4567', lastVisit: 'May 24, 2025', condition: 'Hypertension', status: 'Follow Up' },
    { id: 'PAT-0002', name: 'Michael Brown', age: 45, gender: 'Male', phone: '(555) 234-5678', lastVisit: 'May 23, 2025', condition: 'Diabetes Type 2', status: 'In Treatment' },
    { id: 'PAT-0003', name: 'Emily Davis', age: 29, gender: 'Female', phone: '(555) 345-6789', lastVisit: 'May 22, 2025', condition: 'Asthma', status: 'Follow Up' },
    { id: 'PAT-0004', name: 'David Wilson', age: 62, gender: 'Male', phone: '(555) 456-7890', lastVisit: 'May 21, 2025', condition: 'Arthritis', status: 'In Treatment' },
    { id: 'PAT-0005', name: 'Jessica Taylor', age: 31, gender: 'Female', phone: '(555) 567-8901', lastVisit: 'May 20, 2025', condition: 'Migraine', status: 'New Patient' },
  ];

  // Render Dashboard Content
  const renderContent = () => {
    switch (activeMenu) {
      case 'dashboard':
        return <DashboardView kpiData={kpiData} patientData={patientData} />;
      case 'patients':
        return <PatientsView patientData={patientData} />;
      case 'appointments':
        return <AppointmentsView />;
      case 'reports':
        return <ReportsView />;
      default:
        return <DashboardView kpiData={kpiData} patientData={patientData} />;
    }
  };

  return (
    <div className="flex h-screen bg-gray-100" style={{ backgroundColor: COLORS.background }}>
      {/* Sidebar */}
      <aside
        className={`${
          sidebarOpen ? 'w-64' : 'w-20'
        } transition-all duration-300 ease-in-out fixed h-screen left-0 top-0 z-40`}
        style={{ backgroundColor: COLORS.primary }}
      >
        {/* Logo */}
        <div className="flex items-center justify-between p-4 border-b border-blue-600">
          {sidebarOpen && (
            <div className="flex items-center gap-2">
              <Heart className="w-8 h-8 text-white" />
              <span className="text-white font-bold text-lg">MediCare</span>
            </div>
          )}
          <button
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="text-white hover:bg-blue-700 p-1 rounded"
          >
            {sidebarOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>

        {/* Menu Items */}
        <nav className="flex-1 overflow-y-auto py-4">
          {menuItems.map((item) => {
            const Icon = item.icon;
            return (
              <button
                key={item.id}
                onClick={() => setActiveMenu(item.id)}
                className={`w-full flex items-center gap-4 px-4 py-3 transition-colors ${
                  activeMenu === item.id
                    ? 'bg-blue-600 text-white border-l-4 border-white'
                    : 'text-blue-100 hover:bg-blue-700'
                }`}
              >
                <Icon size={20} />
                {sidebarOpen && <span className="text-sm font-medium">{item.label}</span>}
              </button>
            );
          })}
        </nav>

        {/* Help Section */}
        {sidebarOpen && (
          <div className="p-4 border-t border-blue-600">
            <div className="bg-blue-600 rounded-lg p-3 text-center">
              <MessageSquare className="w-6 h-6 text-white mx-auto mb-2" />
              <p className="text-white text-xs font-medium mb-2">Need Help?</p>
              <p className="text-blue-100 text-xs mb-3">Contact our support team</p>
              <button className="w-full bg-white text-blue-600 py-2 rounded font-medium text-xs hover:bg-gray-100 transition">
                Contact Support
              </button>
            </div>
          </div>
        )}
      </aside>

      {/* Main Content */}
      <main className={`${sidebarOpen ? 'ml-64' : 'ml-20'} flex-1 flex flex-col transition-all duration-300`}>
        {/* Top Header */}
        <header className="bg-white border-b border-gray-200 px-8 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4 flex-1">
            <h1 className="text-2xl font-bold" style={{ color: COLORS.text }}>
              {menuItems.find((m) => m.id === activeMenu)?.label || 'Dashboard'}
            </h1>
          </div>

          {/* Search Bar */}
          <div className="flex-1 max-w-md mx-4">
            <div className="relative">
              <Search className="absolute left-3 top-3 text-gray-400" size={18} />
              <input
                type="text"
                placeholder="Search patients, appointments..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {/* Right Actions */}
          <div className="flex items-center gap-4">
            {/* Notifications */}
            <button className="relative p-2 hover:bg-gray-100 rounded-lg transition">
              <Bell size={20} style={{ color: COLORS.textMuted }} />
              <span className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
            </button>

            {/* Messages */}
            <button className="relative p-2 hover:bg-gray-100 rounded-lg transition">
              <MessageSquare size={20} style={{ color: COLORS.textMuted }} />
              <span className="absolute top-1 right-1 w-2 h-2 bg-blue-500 rounded-full"></span>
            </button>

            {/* User Profile Dropdown */}
            <div className="relative">
              <button
                onClick={() => setUserDropdownOpen(!userDropdownOpen)}
                className="flex items-center gap-2 p-2 hover:bg-gray-100 rounded-lg transition"
              >
                <div className="w-8 h-8 bg-gradient-to-br from-blue-400 to-blue-600 rounded-full flex items-center justify-center text-white text-sm font-bold">
                  JS
                </div>
                <div className="text-left">
                  <p className="text-sm font-medium" style={{ color: COLORS.text }}>
                    Dr. James Smith
                  </p>
                  <p className="text-xs" style={{ color: COLORS.textMuted }}>
                    Admin
                  </p>
                </div>
                <ChevronDown size={16} style={{ color: COLORS.textMuted }} />
              </button>

              {/* Dropdown Menu */}
              {userDropdownOpen && (
                <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 z-50">
                  <button className="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm" style={{ color: COLORS.text }}>
                    Profile
                  </button>
                  <button className="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm" style={{ color: COLORS.text }}>
                    Settings
                  </button>
                  <hr className="my-2" />
                  <button className="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm text-red-600">
                    Logout
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        {/* Page Content */}
        <div className="flex-1 overflow-auto p-8">
          {renderContent()}
        </div>
      </main>
    </div>
  );
}

// Dashboard View Component
function DashboardView({ kpiData, patientData }) {
  return (
    <div className="space-y-6">
      {/* Welcome Section */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold" style={{ color: '#212529' }}>
            Welcome back, Dr. James Smith
          </h2>
          <p className="text-gray-600 mt-1">Here's what's happening in your clinic today.</p>
        </div>
        <select className="px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option>May 19 - May 25, 2025</option>
          <option>May 12 - May 18, 2025</option>
        </select>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {kpiData.map((kpi, index) => {
          const Icon = kpi.icon;
          return (
            <div key={index} className="bg-white rounded-lg shadow p-6 hover:shadow-lg transition">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-gray-600 text-sm font-medium">{kpi.title}</p>
                  <p className="text-3xl font-bold mt-2" style={{ color: '#0F52BA' }}>
                    {kpi.value}
                  </p>
                  <p className="text-green-600 text-sm mt-2 flex items-center gap-1">
                    <TrendingUp size={14} /> {kpi.change} from last week
                  </p>
                </div>
                <div className={`${kpi.color} p-3 rounded-lg`}>
                  <Icon size={24} style={{ color: '#0F52BA' }} />
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Appointments Overview Chart */}
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-bold mb-4" style={{ color: '#212529' }}>
            Appointments Overview
          </h3>
          <div className="flex items-end justify-between h-64 gap-2">
            {[20, 45, 60, 50, 70, 40, 25].map((height, i) => (
              <div key={i} className="flex-1 flex flex-col items-center">
                <div
                  className="w-full bg-gradient-to-t from-blue-500 to-blue-400 rounded-t"
                  style={{ height: `${height * 2}px` }}
                ></div>
                <p className="text-xs text-gray-600 mt-2">
                  {['Mon 19', 'Tue 20', 'Wed 21', 'Thu 22', 'Fri 23', 'Sat 24', 'Sun 25'][i]}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* Patient Gender Distribution */}
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-bold mb-4" style={{ color: '#212529' }}>
            Patient Gender Distribution
          </h3>
          <div className="flex items-center justify-center">
            <div className="relative w-48 h-48">
              <svg viewBox="0 0 100 100" className="w-full h-full">
                <circle cx="50" cy="50" r="45" fill="none" stroke="#007BFF" strokeWidth="20" strokeDasharray="75 100" />
                <circle cx="50" cy="50" r="45" fill="none" stroke="#28A745" strokeWidth="20" strokeDasharray="25 100" strokeDashoffset="-75" />
              </svg>
              <div className="absolute inset-0 flex items-center justify-center">
                <div className="text-center">
                  <p className="text-2xl font-bold" style={{ color: '#0F52BA' }}>
                    2,543
                  </p>
                  <p className="text-xs text-gray-600">Total</p>
                </div>
              </div>
            </div>
            <div className="ml-6 space-y-3">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full" style={{ backgroundColor: '#007BFF' }}></div>
                <span className="text-sm">Male: 1,356 (53.3%)</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full" style={{ backgroundColor: '#28A745' }}></div>
                <span className="text-sm">Female: 1,187 (46.7%)</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Recent Patients Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h3 className="text-lg font-bold" style={{ color: '#212529' }}>
            Recent Patients
          </h3>
          <a href="#" className="text-blue-600 text-sm font-medium hover:underline">
            View All Patients →
          </a>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Patient ID</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Name</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Age</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Gender</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Last Visit</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Condition</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {patientData.map((patient) => (
                <tr key={patient.id} className="hover:bg-gray-50 transition">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{patient.id}</td>
                  <td className="px-6 py-4 text-sm text-gray-900">{patient.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.age}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.gender}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.lastVisit}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.condition}</td>
                  <td className="px-6 py-4 text-sm">
                    <span
                      className={`px-3 py-1 rounded-full text-xs font-medium ${
                        patient.status === 'In Treatment'
                          ? 'bg-green-100 text-green-800'
                          : patient.status === 'Follow Up'
                          ? 'bg-blue-100 text-blue-800'
                          : 'bg-yellow-100 text-yellow-800'
                      }`}
                    >
                      {patient.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm">
                    <button className="text-blue-600 hover:underline font-medium">View</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// Patients View Component
function PatientsView({ patientData }) {
  const [searchTerm, setSearchTerm] = useState('');

  const filteredPatients = patientData.filter((patient) =>
    patient.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    patient.id.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold" style={{ color: '#212529' }}>
          Patient Management
        </h2>
        <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium">
          + Add New Patient
        </button>
      </div>

      {/* Search & Filter */}
      <div className="bg-white rounded-lg shadow p-4">
        <input
          type="text"
          placeholder="Search by name or patient ID..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Patients Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Patient ID</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Name</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Age</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Phone</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Last Visit</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {filteredPatients.map((patient) => (
                <tr key={patient.id} className="hover:bg-gray-50 transition">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{patient.id}</td>
                  <td className="px-6 py-4 text-sm text-gray-900 font-medium">{patient.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.age}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.phone}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{patient.lastVisit}</td>
                  <td className="px-6 py-4 text-sm space-x-2">
                    <button className="text-blue-600 hover:underline">View</button>
                    <button className="text-gray-600 hover:underline">Edit</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// Appointments View Component
function AppointmentsView() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold" style={{ color: '#212529' }}>
          Appointment Scheduling
        </h2>
        <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium">
          + Book Appointment
        </button>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <p className="text-gray-600">Appointment scheduling interface coming soon...</p>
      </div>
    </div>
  );
}

// Reports View Component
function ReportsView() {
  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold" style={{ color: '#212529' }}>
        Medical Reports
      </h2>

      <div className="bg-white rounded-lg shadow p-6">
        <p className="text-gray-600">Medical reports and analytics coming soon...</p>
      </div>
    </div>
  );
}
