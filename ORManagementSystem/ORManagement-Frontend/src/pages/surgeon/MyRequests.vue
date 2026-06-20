<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getMyRequests,
  cancelRequest,
  updateRequest
} from '../../services/requestService'
import { formatDate, formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)
const requests = ref([])

const selectedRequest = ref(null)

const editForm = ref({
  preferredDate: '',
  preferredQuarter: 'Flexible',
  estimatedDurationMin: 60,
  surgeryType: '',
  priority: 'Elective',
  patientReadiness: 'Ready',
  remarks: '',
  availableDaysMask: 31
})

const loadRequests = async () => {
  loading.value = true

  try {
    const response = await getMyRequests()
    requests.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load requests.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const openEdit = (request) => {
  selectedRequest.value = request

  editForm.value = {
    preferredDate: request.preferredDate?.substring(0, 10) || '',
    preferredQuarter: request.preferredQuarter || 'Flexible',
    estimatedDurationMin: request.estimatedDurationMin || 60,
    surgeryType: request.surgeryType || '',
    priority: request.priority || 'Elective',
    patientReadiness: request.patientReadiness || 'Ready',
    remarks: request.remarks || '',
    availableDaysMask: request.availableDaysMask || 31
  }
}

const canEdit = (request) => {
  return request.requestStatus === 'PendingReview' || request.requestStatus === 'Modified'
}

const canCancel = (request) => {
  return ['PendingReview', 'Modified', 'Waitlisted'].includes(request.requestStatus)
}

const submitUpdate = async () => {
  if (!selectedRequest.value) return

if (!editForm.value.preferredDate) {
  showToast('Preferred date is required.', 'warning')
  return
}

if (!editForm.value.surgeryType.trim()) {
  showToast('Surgery type is required.', 'warning')
  return
}

if (!editForm.value.estimatedDurationMin || editForm.value.estimatedDurationMin <= 0) {
  showToast('Duration must be greater than zero.', 'warning')
  return
}

  saving.value = true

  try {
    await updateRequest(selectedRequest.value.requestId, {
      preferredDate: editForm.value.preferredDate,
      preferredQuarter: editForm.value.preferredQuarter,
      estimatedDurationMin: Number(editForm.value.estimatedDurationMin),
      surgeryType: editForm.value.surgeryType,
      priority: editForm.value.priority,
      patientReadiness: editForm.value.patientReadiness,
      remarks: editForm.value.remarks,
      availableDaysMask: Number(editForm.value.availableDaysMask)
    })

    showToast('Request updated successfully.', 'success')
    selectedRequest.value = null
    await loadRequests()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update request.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const handleCancel = async (request) => {
  if (!confirm(`Cancel request #${request.requestId}?`)) {
    return
  }

  try {
    await cancelRequest(request.requestId)
    showToast('Request cancelled successfully.', 'success')
    await loadRequests()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to cancel request.'

    showToast(message, 'error')
  }
}

onMounted(loadRequests)
</script>

<template>
  <div>
    <PageHeader
      title="My Requests"
      subtitle="Track your submitted OR requests and scheduler decisions"
      icon="bi-list-check"
    >
      <template #actions>
        <router-link to="/app/surgeon/request-or-time" class="btn btn-primary">
          <i class="bi bi-plus-circle me-1"></i>
          New Request
        </router-link>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else class="page-card">
      <EmptyState
        v-if="requests.length === 0"
        title="No requests"
        message="You have not submitted any OR time requests yet."
        icon="bi-inbox"
      />

      <div v-else class="table-responsive">
        <table class="table table-hover align-middle">
          <thead>
            <tr>
              <th>ID</th>
              <th>Patient</th>
              <th>Surgery</th>
              <th>Preferred</th>
              <th>Duration</th>
              <th>Priority</th>
              <th>Availability</th>
              <th>Status</th>
              <th>Remarks</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="request in requests" :key="request.requestId">
              <td>#{{ request.requestId }}</td>
              <td>
                <div>{{ request.patientName }}</div>
                <small class="text-muted">{{ request.patientMrn }}</small>
              </td>
              <td>{{ request.surgeryType }}</td>
              <td>
                <div>{{ formatDate(request.preferredDate) }}</div>
                <small class="text-muted">{{ request.preferredQuarter }}</small>
              </td>
              <td>{{ request.estimatedDurationMin }} min</td>
              <td>{{ request.priority }}</td>
              <td>{{ request.availableDaysDisplay }}</td>
              <td>
                <StatusBadge :status="request.requestStatus" />
              </td>
              <td>
                <small>{{ request.schedulerRemarks || request.remarks || '-' }}</small>
              </td>
              <td class="text-end">
                <button
                  v-if="canEdit(request)"
                  class="btn btn-sm btn-outline-primary me-2"
                  @click="openEdit(request)"
                >
                  Edit
                </button>

                <button
                  v-if="canCancel(request)"
                  class="btn btn-sm btn-outline-danger"
                  @click="handleCancel(request)"
                >
                  Cancel
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Edit Modal-like card -->
    <AppModal
  :show="!!selectedRequest"
  :title="selectedRequest ? `Edit Request #${selectedRequest.requestId}` : 'Edit Request'"
  size="lg"
  @close="selectedRequest = null"
>
  <div class="row g-3">
    <div class="col-md-4">
      <label class="form-label">Preferred Date</label>
      <input
        v-model="editForm.preferredDate"
        type="date"
        class="form-control"
      />
    </div>

    <div class="col-md-4">
      <label class="form-label">Preferred Quarter</label>
      <select v-model="editForm.preferredQuarter" class="form-select">
        <option value="Q1">Q1</option>
        <option value="Q2">Q2</option>
        <option value="Flexible">Flexible</option>
      </select>
    </div>

    <div class="col-md-4">
      <label class="form-label">Duration Minutes</label>
      <input
        v-model.number="editForm.estimatedDurationMin"
        type="number"
        class="form-control"
        min="1"
      />
    </div>

    <div class="col-md-6">
      <label class="form-label">Surgery Type</label>
      <input
        v-model="editForm.surgeryType"
        class="form-control"
        placeholder="e.g. Knee Replacement"
      />
    </div>

    <div class="col-md-3">
      <label class="form-label">Priority</label>
      <select v-model="editForm.priority" class="form-select">
        <option value="Elective">Elective</option>
        <option value="Urgent">Urgent</option>
        <option value="Emergency">Emergency</option>
      </select>
    </div>

    <div class="col-md-3">
      <label class="form-label">Patient Readiness</label>
      <select v-model="editForm.patientReadiness" class="form-select">
        <option value="Ready">Ready</option>
        <option value="PendingClearance">Pending Clearance</option>
        <option value="NotReady">Not Ready</option>
      </select>
    </div>

    <div class="col-md-4">
      <label class="form-label">Availability</label>
      <select v-model.number="editForm.availableDaysMask" class="form-select">
        <option :value="31">Mon-Fri</option>
        <option :value="3">Mon-Tue</option>
        <option :value="1">Monday Only</option>
        <option :value="4">Wednesday Only</option>
        <option :value="28">Wed-Fri</option>
      </select>
    </div>

    <div class="col-md-8">
      <label class="form-label">Remarks</label>
      <input
        v-model="editForm.remarks"
        class="form-control"
        placeholder="Optional notes"
      />
    </div>
  </div>

  <template #footer>
    <button
      class="btn btn-outline-secondary"
      @click="selectedRequest = null"
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
      Save Changes
    </button>
  </template>
</AppModal>
  </div>
</template>