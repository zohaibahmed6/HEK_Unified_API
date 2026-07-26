/**
 * Healthcare API Service
 * Handles all API calls to the healthcare backend
 * Includes error handling, loading states, and data transformation
 */

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:3000/api';

// Helper function to handle API errors
const handleError = (error) => {
  console.error('API Error:', error);
  if (error.response) {
    // Server responded with error status
    throw new Error(error.response.data?.message || 'An error occurred');
  } else if (error.request) {
    // Request made but no response
    throw new Error('No response from server');
  } else {
    throw error;
  }
};

// Helper function to make API calls
const apiCall = async (endpoint, options = {}) => {
  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
        ...options.headers,
      },
      ...options,
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    handleError(error);
  }
};

// ============================================
// PATIENT ENDPOINTS
// ============================================

export const patientService = {
  /**
   * Get all patients with optional filters
   * @param {Object} filters - Filter parameters (search, status, etc.)
   * @returns {Promise<Array>} List of patients
   */
  getAllPatients: async (filters = {}) => {
    const queryParams = new URLSearchParams(filters).toString();
    return apiCall(`/patients?${queryParams}`);
  },

  /**
   * Get a single patient by ID
   * @param {string} patientId - Patient ID
   * @returns {Promise<Object>} Patient details
   */
  getPatientById: async (patientId) => {
    return apiCall(`/patients/${patientId}`);
  },

  /**
   * Create a new patient
   * @param {Object} patientData - Patient information
   * @returns {Promise<Object>} Created patient
   */
  createPatient: async (patientData) => {
    return apiCall('/patients', {
      method: 'POST',
      body: JSON.stringify(patientData),
    });
  },

  /**
   * Update patient information
   * @param {string} patientId - Patient ID
   * @param {Object} patientData - Updated patient data
   * @returns {Promise<Object>} Updated patient
   */
  updatePatient: async (patientId, patientData) => {
    return apiCall(`/patients/${patientId}`, {
      method: 'PUT',
      body: JSON.stringify(patientData),
    });
  },

  /**
   * Delete a patient
   * @param {string} patientId - Patient ID
   * @returns {Promise<void>}
   */
  deletePatient: async (patientId) => {
    return apiCall(`/patients/${patientId}`, {
      method: 'DELETE',
    });
  },

  /**
   * Search patients by name or ID
   * @param {string} query - Search query
   * @returns {Promise<Array>} Search results
   */
  searchPatients: async (query) => {
    return apiCall(`/patients/search?q=${encodeURIComponent(query)}`);
  },
};

// ============================================
// APPOINTMENT ENDPOINTS
// ============================================

export const appointmentService = {
  /**
   * Get all appointments
   * @param {Object} filters - Filter parameters (date, doctor, status, etc.)
   * @returns {Promise<Array>} List of appointments
   */
  getAllAppointments: async (filters = {}) => {
    const queryParams = new URLSearchParams(filters).toString();
    return apiCall(`/appointments?${queryParams}`);
  },

  /**
   * Get appointments for a specific patient
   * @param {string} patientId - Patient ID
   * @returns {Promise<Array>} Patient's appointments
   */
  getPatientAppointments: async (patientId) => {
    return apiCall(`/patients/${patientId}/appointments`);
  },

  /**
   * Book a new appointment
   * @param {Object} appointmentData - Appointment details
   * @returns {Promise<Object>} Created appointment
   */
  bookAppointment: async (appointmentData) => {
    return apiCall('/appointments', {
      method: 'POST',
      body: JSON.stringify(appointmentData),
    });
  },

  /**
   * Reschedule an appointment
   * @param {string} appointmentId - Appointment ID
   * @param {Object} newData - New appointment details
   * @returns {Promise<Object>} Updated appointment
   */
  rescheduleAppointment: async (appointmentId, newData) => {
    return apiCall(`/appointments/${appointmentId}`, {
      method: 'PUT',
      body: JSON.stringify(newData),
    });
  },

  /**
   * Cancel an appointment
   * @param {string} appointmentId - Appointment ID
   * @returns {Promise<void>}
   */
  cancelAppointment: async (appointmentId) => {
    return apiCall(`/appointments/${appointmentId}`, {
      method: 'DELETE',
    });
  },

  /**
   * Get available time slots for a doctor
   * @param {string} doctorId - Doctor ID
   * @param {string} date - Date (YYYY-MM-DD)
   * @returns {Promise<Array>} Available slots
   */
  getAvailableSlots: async (doctorId, date) => {
    return apiCall(`/doctors/${doctorId}/available-slots?date=${date}`);
  },
};

// ============================================
// MEDICAL RECORDS ENDPOINTS
// ============================================

export const medicalRecordService = {
  /**
   * Get all medical records for a patient
   * @param {string} patientId - Patient ID
   * @returns {Promise<Array>} Medical records
   */
  getPatientRecords: async (patientId) => {
    return apiCall(`/patients/${patientId}/medical-records`);
  },

  /**
   * Get a specific medical record
   * @param {string} recordId - Record ID
   * @returns {Promise<Object>} Medical record details
   */
  getRecordById: async (recordId) => {
    return apiCall(`/medical-records/${recordId}`);
  },

  /**
   * Create a new medical record
   * @param {string} patientId - Patient ID
   * @param {Object} recordData - Record information
   * @returns {Promise<Object>} Created record
   */
  createRecord: async (patientId, recordData) => {
    return apiCall(`/patients/${patientId}/medical-records`, {
      method: 'POST',
      body: JSON.stringify(recordData),
    });
  },

  /**
   * Upload medical document/image
   * @param {string} patientId - Patient ID
   * @param {File} file - File to upload
   * @returns {Promise<Object>} Upload result
   */
  uploadDocument: async (patientId, file) => {
    const formData = new FormData();
    formData.append('file', file);

    return fetch(`${API_BASE_URL}/patients/${patientId}/documents`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
      },
      body: formData,
    }).then((res) => res.json());
  },
};

// ============================================
// DASHBOARD ENDPOINTS
// ============================================

export const dashboardService = {
  /**
   * Get dashboard KPI data
   * @returns {Promise<Object>} KPI statistics
   */
  getKPIData: async () => {
    return apiCall('/dashboard/kpi');
  },

  /**
   * Get patient trends data
   * @param {string} period - Time period (week, month, year)
   * @returns {Promise<Array>} Trend data
   */
  getPatientTrends: async (period = 'week') => {
    return apiCall(`/dashboard/trends?period=${period}`);
  },

  /**
   * Get appointment statistics
   * @returns {Promise<Object>} Appointment stats
   */
  getAppointmentStats: async () => {
    return apiCall('/dashboard/appointment-stats');
  },

  /**
   * Get revenue data
   * @param {string} period - Time period
   * @returns {Promise<Object>} Revenue information
   */
  getRevenueData: async (period = 'month') => {
    return apiCall(`/dashboard/revenue?period=${period}`);
  },
};

// ============================================
// DOCTOR/STAFF ENDPOINTS
// ============================================

export const doctorService = {
  /**
   * Get all doctors
   * @returns {Promise<Array>} List of doctors
   */
  getAllDoctors: async () => {
    return apiCall('/doctors');
  },

  /**
   * Get doctor profile
   * @param {string} doctorId - Doctor ID
   * @returns {Promise<Object>} Doctor details
   */
  getDoctorProfile: async (doctorId) => {
    return apiCall(`/doctors/${doctorId}`);
  },

  /**
   * Get doctor's schedule
   * @param {string} doctorId - Doctor ID
   * @param {string} date - Date (YYYY-MM-DD)
   * @returns {Promise<Array>} Doctor's appointments
   */
  getDoctorSchedule: async (doctorId, date) => {
    return apiCall(`/doctors/${doctorId}/schedule?date=${date}`);
  },
};

// ============================================
// AUTHENTICATION ENDPOINTS
// ============================================

export const authService = {
  /**
   * Login user
   * @param {string} email - User email
   * @param {string} password - User password
   * @returns {Promise<Object>} Auth token and user data
   */
  login: async (email, password) => {
    const response = await apiCall('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
    if (response.token) {
      localStorage.setItem('authToken', response.token);
    }
    return response;
  },

  /**
   * Logout user
   */
  logout: () => {
    localStorage.removeItem('authToken');
  },

  /**
   * Get current user profile
   * @returns {Promise<Object>} User information
   */
  getCurrentUser: async () => {
    return apiCall('/auth/me');
  },

  /**
   * Refresh authentication token
   * @returns {Promise<Object>} New token
   */
  refreshToken: async () => {
    const response = await apiCall('/auth/refresh', {
      method: 'POST',
    });
    if (response.token) {
      localStorage.setItem('authToken', response.token);
    }
    return response;
  },
};

// ============================================
// UTILITY FUNCTIONS
// ============================================

/**
 * Format date for API calls
 * @param {Date} date - JavaScript Date object
 * @returns {string} Formatted date (YYYY-MM-DD)
 */
export const formatDateForAPI = (date) => {
  return date.toISOString().split('T')[0];
};

/**
 * Parse API date to JavaScript Date
 * @param {string} dateString - Date string from API
 * @returns {Date} JavaScript Date object
 */
export const parseAPIDate = (dateString) => {
  return new Date(dateString);
};

/**
 * Format phone number
 * @param {string} phone - Phone number
 * @returns {string} Formatted phone
 */
export const formatPhoneNumber = (phone) => {
  const cleaned = phone.replace(/\D/g, '');
  return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
};

export default {
  patientService,
  appointmentService,
  medicalRecordService,
  dashboardService,
  doctorService,
  authService,
};
