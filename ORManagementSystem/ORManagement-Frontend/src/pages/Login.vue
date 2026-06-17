<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { login } from '../services/authService'
import { showToast } from '../utils/toast'

const router = useRouter()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

// validation
const validateLogin = () => {
  error.value = ''

  const usernameValue = username.value.trim()
  const passwordValue = password.value.trim()

  if (!usernameValue || !passwordValue) {
    error.value = 'Username and Password are required.'
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

// login handler
const handleLogin = async () => {
  if (!validateLogin()) return

  loading.value = true

  try {
    error.value = ''

    // call API (cookies are automatically stored)
    await login({
      username: username.value.trim(),
      password: password.value.trim()
    })

    // no localStorage needed (cookies handle auth)
    showToast('Login successful', 'success')

    router.push('/dashboard')
  } catch (err: any) {
    error.value =
      err?.response?.data?.message ||
      'Login failed. Please try again.'

    // interceptor already shows toast, optional here
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

      <h4 class="auth-title">Login</h4>

      <!-- Username -->
      <div class="input-group mb-3">
        <span class="input-group-text">
          <i class="bi bi-person"></i>
        </span>

        <input
          required
          v-model="username"
          class="form-control"
          placeholder="Username"
        />
      </div>

      <!-- Password -->
      <div class="input-group mb-3">
        <span class="input-group-text">
          <i class="bi bi-lock"></i>
        </span>

        <input
          required
          v-model="password"
          type="password"
          class="form-control"
          placeholder="Password"
        />
      </div>

      <!-- Error -->
      <p v-if="error" class="text-danger">{{ error }}</p>

      <!-- Button -->
      <button
        class="btn btn-primary auth-btn w-100"
        :disabled="loading"
        @click="handleLogin"
      >
        {{ loading ? 'Logging in...' : 'Login' }}
      </button>

      <!-- Redirect -->
      <p class="text-center mt-3">
        New user?
        <router-link to="/register">Register</router-link>
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
  width: 350px;
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