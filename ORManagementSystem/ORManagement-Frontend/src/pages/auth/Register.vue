<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { register } from '../../services/authService'
import { showToast } from '../../utils/toast'

const router = useRouter()

const username = ref('')
const fullName = ref('')
const email = ref('')
const password = ref('')
const roleName = ref('Surgeon')
const specialty = ref('')
const hospitalId = ref(1)

const error = ref('')
const loading = ref(false)

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

  if (roleName.value === 'Surgeon' && !specialty.value.trim()) {
    error.value = 'Specialty is required for surgeons.'
    return false
  }

  return true
}

const handleRegister = async () => {
  if (!validateRegister()) {
    showToast(error.value, 'error')
    return
  }

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
      specialty: roleName.value === 'Surgeon'
        ? specialty.value.trim()
        : null
    })

    showToast('Registered successfully. Please login.', 'success')

    router.push('/')
  } catch (err) {
    error.value =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Registration failed.'

    showToast(error.value, 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="navbar-custom text-center">
    <h1 class="m-0">OR Management System</h1>
  </div>

  <div class="auth-wrapper">
    <div class="auth-card">
      <h4 class="auth-title">Register</h4>

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

      <div class="input-group mb-3">
        <span class="input-group-text">
          <i class="bi bi-people"></i>
        </span>
        <select v-model="roleName" class="form-select">
          <option value="Surgeon">Surgeon</option>
          <option value="ORScheduler">OR Scheduler</option>
        </select>
      </div>

      <div v-if="roleName === 'Surgeon'" class="input-group mb-3">
        <span class="input-group-text">
          <i class="bi bi-heart-pulse"></i>
        </span>
        <input
          v-model="specialty"
          class="form-control"
          placeholder="Enter Specialty, e.g. Orthopedics"
        />
      </div>

      <div class="input-group mb-3">
        <span class="input-group-text">
          <i class="bi bi-hospital"></i>
        </span>
        <input
          v-model.number="hospitalId"
          type="number"
          class="form-control"
          placeholder="Enter Hospital ID"
        />
      </div>

      <p v-if="error" class="text-danger small">
        {{ error }}
      </p>

      <button
        class="btn btn-success auth-btn w-100"
        :disabled="loading"
        @click="handleRegister"
      >
        <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
        {{ loading ? 'Registering...' : 'Register' }}
      </button>

      <p class="text-center mt-3 mb-0">
        Already have an account?
        <router-link to="/">Login</router-link>
      </p>
    </div>
  </div>
</template>

<style scoped>
.navbar-custom {
  background: #111827;
  color: white;
  padding: 18px;
}

.input-group-text,
.form-control,
.form-select {
  height: 45px;
}

.auth-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 60px;
}

.auth-card {
  width: 420px;
  padding: 25px;
  border-radius: 12px;
  background: white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

.auth-title {
  text-align: center;
  margin-bottom: 20px;
}

.auth-btn {
  height: 45px;
}
</style>