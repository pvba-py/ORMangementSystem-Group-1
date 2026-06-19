<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/authStore'
import { showToast } from '../../utils/toast'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const validateLogin = () => {
  error.value = ''

  const usernameValue = username.value.trim()
  const passwordValue = password.value.trim()

  if (!usernameValue || !passwordValue) {
    error.value = 'Username and password are required.'
    return false
  }

  if (usernameValue.length < 3) {
    error.value = 'Username must be at least 3 characters.'
    return false
  }

  if (passwordValue.length < 6) {
    error.value = 'Password must be at least 6 characters.'
    return false
  }

  return true
}

const handleLogin = async () => {
  if (!validateLogin()) {
    showToast(error.value, 'error')
    return
  }

  loading.value = true

  try {
    error.value = ''

    await authStore.login({
      username: username.value.trim(),
      password: password.value.trim()
    })

    if (authStore.user?.roleName === 'Surgeon') {
      showToast('Login successful', 'success')
      router.push('/app/surgeon/dashboard')
      return
    }

    if (authStore.user?.roleName === 'ORScheduler') {
      showToast('Login successful', 'success')
      router.push('/app/scheduler/dashboard')
      return
    }

    error.value = 'Login succeeded, but user role could not be loaded. Please contact administrator.'
    showToast(error.value, 'error')
  } catch (err) {
    error.value =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Login failed. Please try again.'

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
      <h4 class="auth-title">Login</h4>

      <div class="input-group mb-3">
        <span class="input-group-text">
          <i class="bi bi-person"></i>
        </span>

        <input
          v-model="username"
          class="form-control"
          placeholder="Username"
          autocomplete="username"
          @keyup.enter="handleLogin"
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
          placeholder="Password"
          autocomplete="current-password"
          @keyup.enter="handleLogin"
        />
      </div>

      <p v-if="error" class="text-danger small">
        {{ error }}
      </p>

      <button
        class="btn btn-primary auth-btn w-100"
        :disabled="loading"
        @click="handleLogin"
      >
        <span
          v-if="loading"
          class="spinner-border spinner-border-sm me-2"
        ></span>
        {{ loading ? 'Logging in...' : 'Login' }}
      </button>

      <p class="text-center mt-3 mb-0">
        New user?
        <router-link to="/register">Register</router-link>
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
.form-control {
  height: 45px;
}

.auth-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 60px;
}

.auth-card {
  width: 380px;
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