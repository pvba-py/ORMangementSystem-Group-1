<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { register } from '../services/authService'
import { showToast } from '../utils/toast'

const router = useRouter()

// form fields
const username = ref('')
const fullName = ref('')
const email = ref('')
const password = ref('')
const roleName = ref('Surgeon')
const specialty = ref('')
const hospitalId = ref<number | null>(1)

const error = ref('')
const loading = ref(false)

// validation
const validateRegister = () => {
  error.value = ''

  const usernameValue = username.value.trim()
  const fullNameValue = fullName.value.trim()
  const emailValue = email.value.trim()
  const passwordValue = password.value.trim()

  if (!usernameValue || !fullNameValue || !emailValue || !passwordValue) {
    error.value = 'All fields are required.'
    return false
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  if (!emailRegex.test(emailValue)) {
    error.value = 'Please enter a valid email address.'
    return false
  }

  if (passwordValue.length < 6) {
    error.value = 'Password must be at least 6 characters.'
    return false
  }

  if (!hospitalId.value) {
    error.value = 'Hospital ID is required.'
    return false
  }

  // only for surgeon
  if (roleName.value === 'Surgeon' && !specialty.value.trim()) {
    error.value = 'Specialty is required for surgeons.'
    return false
  }

  return true
}

// register handler
const handleRegister = async () => {
  if (!validateRegister()) return

  loading.value = true

  try {
    error.value = ''

    await register({
      username: username.value.trim(),
      fullName: fullName.value.trim(),
      email: email.value.trim(),
      password: password.value.trim(),
      roleName: roleName.value,
      hospitalId: hospitalId.value,
      specialty:
        roleName.value === 'Surgeon'
          ? specialty.value.trim()
          : null
    })

    showToast('Registered successfully', 'success')

    router.push('/login')
  } catch (err: any) {
    error.value =
      err?.response?.data?.message ||
      'Registration failed.'

    showToast(error.value, 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="navbar-custom text-center">
    <h1 style="margin: 0;">OR Management System</h1>
  </div>

  <div class="auth-wrapper">
    <div class="auth-card">

      <h4 class="auth-title">Register</h4>

      <!-- Username -->
    <div class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-person"></i>
    </span>
    <input
        v-model="username"
        class="form-control"
        placeholder="Enter Username"
    />
    </div>

    <!-- Full Name -->
    <div class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-person-badge"></i>
    </span>
    <input
        v-model="fullName"
        class="form-control"
        placeholder="Enter Full Name"
    />
    </div>

    <!-- Email -->
    <div class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-envelope"></i>
    </span>
    <input
        v-model="email"
        type="email"
        class="form-control"
        placeholder="Enter Email"
    />
    </div>

    <!-- Password -->
    <div class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-lock"></i>
    </span>
    <input
        v-model="password"
        type="password"
        class="form-control"
        placeholder="Enter Password"
    />
    </div>

    <!-- Role -->
    <div class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-people"></i>
    </span>
    <select v-model="roleName" class="form-control">
        <option value="Surgeon">Surgeon</option>
        <option value="ORScheduler">OR Scheduler</option>
    </select>
    </div>

    <!-- Specialty (only surgeon) -->
    <div v-if="roleName === 'Surgeon'" class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-heart-pulse"></i>
    </span>
    <input
        v-model="specialty"
        class="form-control"
        placeholder="Enter Specialty (e.g., Orthopedics)"
    />
    </div>

    <!-- Hospital ID -->
    <div class="input-group mb-3">
    <span class="input-group-text">
        <i class="bi bi-hospital"></i>
    </span>
    <input
        v-model.number="hospitalId"
        type="number"
        class="form-control"
        placeholder="Enter Hospital ID (HID)"
    />
    </div>

      <!-- Error -->
      <p v-if="error" class="text-danger">{{ error }}</p>

      <!-- Register Button -->
      <button
        class="btn btn-success auth-btn w-100"
        :disabled="loading"
        @click="handleRegister"
      >
        {{ loading ? 'Registering...' : 'Register' }}
      </button>

      <!-- Redirect -->
      <p class="text-center mt-3">
        Already have an account?
        <router-link to="/login">Login</router-link>
      </p>

    </div>
  </div>
</template>

<style scoped>
.input-group-text,
.form-control {
  height: 45px;
}

.auth-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 60px;
}

.auth-card {
  width: 400px;
  padding: 25px;
  border-radius: 8px;
  background: white;
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.auth-title {
  text-align: center;
  margin-bottom: 20px;
}
</style>