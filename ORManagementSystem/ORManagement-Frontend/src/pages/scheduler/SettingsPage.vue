<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import { getSettings, updateSetting } from '../../services/settingsService'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)

const settings = ref([])
const selectedSetting = ref(null)

const settingForm = ref({
  settingValue: ''
})

const loadSettings = async () => {
  loading.value = true

  try {
    const response = await getSettings()
    settings.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load settings.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const openEdit = setting => {
  selectedSetting.value = setting
  settingForm.value = {
    settingValue: setting.settingValue
  }
}

const submitUpdate = async () => {
  if (!selectedSetting.value) return

  if (!settingForm.value.settingValue?.trim()) {
    showToast('Setting value is required.', 'warning')
    return
  }

  saving.value = true

  try {
    await updateSetting(selectedSetting.value.settingKey, {
      settingValue: settingForm.value.settingValue.trim()
    })

    showToast('Setting updated successfully.', 'success')
    selectedSetting.value = null
    await loadSettings()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update setting.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

onMounted(loadSettings)
</script>

<template>
  <div>
    <PageHeader
      title="System Settings"
      subtitle="Manage configurable scheduling policies and thresholds"
      icon="bi-gear"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <div class="page-card">
        <EmptyState
          v-if="settings.length === 0"
          title="No settings"
          message="No system settings were found."
          icon="bi-gear"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>ID</th>
                <th>Hospital</th>
                <th>Key</th>
                <th>Value</th>
                <th class="text-end">Actions</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="setting in settings" :key="setting.settingId">
                <td>#{{ setting.settingId }}</td>
                <td>
                  <span v-if="setting.hospitalId">Hospital #{{ setting.hospitalId }}</span>
                  <span v-else class="badge bg-secondary">Global Default</span>
                </td>
                <td>
                  <strong>{{ setting.settingKey }}</strong>
                </td>
                <td>{{ setting.settingValue }}</td>
                <td class="text-end">
                  <button class="btn btn-sm btn-outline-primary" @click="openEdit(setting)">
                    Edit
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <AppModal
  :show="!!selectedSetting"
  :title="selectedSetting ? `Edit Setting — ${selectedSetting.settingKey}` : 'Edit Setting'"
  size="md"
  @close="selectedSetting = null"
>
  <div class="mb-3">
    <label class="form-label">Setting Value</label>
    <input
      v-model="settingForm.settingValue"
      class="form-control"
    />
  </div>

  <template #footer>
    <button
      class="btn btn-outline-secondary"
      @click="selectedSetting = null"
    >
      Cancel
    </button>

    <button
      class="btn btn-primary"
      :disabled="saving"
      @click="submitUpdate"
    >
      <span
        v-if="saving"
        class="spinner-border spinner-border-sm me-2"
      ></span>
      Save
    </button>
  </template>
</AppModal>
    </div>
  </div>
</template>