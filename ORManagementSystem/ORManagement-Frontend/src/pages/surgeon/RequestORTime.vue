<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import { getPatients } from '../../services/masterDataService'
import { getCurrentCycle } from '../../services/cycleService'
import { createRequest } from '../../services/requestService'
import { showToast } from '../../utils/toast'

const router = useRouter()

const loading = ref(false)
const submitting = ref(false)

const patients = ref([])
const currentCycle = ref(null)

const form = ref({
  patientId: '',
  surgeryType: '',
  estimatedDurationMin: '',
  priority: 'Elective',
  patientReadiness: 'Ready',
  remarks: ''
})

const loadData = async () => {
  loading.value = true

  try {
    const [patientsResponse, cycleResponse] = await Promise.all([
      getPatients(),
      getCurrentCycle()
    ])

    patients.value = patientsResponse.data || []
    currentCycle.value = cycleResponse.data || null
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load request form data.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const validate = () => {
  if (!form.value.patientId) {
    showToast('Please select a patient.', 'warning')
    return false
  }

  if (!form.value.surgeryType.trim()) {
    showToast('Surgery type is required.', 'warning')
    return false
  }

  if (
    !form.value.estimatedDurationMin ||
    Number(form.value.estimatedDurationMin) <= 0
  ) {
    showToast('Estimated duration must be greater than zero.', 'warning')
    return false
  }

  if (!form.value.priority) {
    showToast('Priority is required.', 'warning')
    return false
  }

  if (!form.value.patientReadiness) {
    showToast('Patient readiness is required.', 'warning')
    return false
  }

  if (!currentCycle.value && form.value.priority !== 'Emergency') {
    showToast('No current scheduling cycle was found.', 'warning')
    return false
  }

  return true
}

const submitRequest = async () => {
  if (submitting.value) {
    return
  }

  if (!validate()) {
    return
  }

  submitting.value = true

  try {
    await createRequest({
      patientId: Number(form.value.patientId),
      estimatedDurationMin: Number(form.value.estimatedDurationMin),
      surgeryType: form.value.surgeryType.trim(),
      priority: form.value.priority,
      patientReadiness: form.value.patientReadiness,
      remarks: form.value.remarks?.trim() || null
    })

    showToast('OR request submitted successfully.', 'success')
    router.push('/app/surgeon/requests')
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to submit OR request.'

    showToast(message, 'error')
  } finally {
    submitting.value = false
  }
}

onMounted(loadData)
</script>

<template>
  <div>
    <PageHeader
      title="Request OR Time"
      subtitle="Submit a surgery time request for scheduler review"
      icon="bi-plus-circle"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else class="page-card">
      <div v-if="currentCycle" class="alert alert-info">
        <i class="bi bi-calendar-week me-2"></i>
        Current planning cycle:
        <strong>#{{ currentCycle.cycleId }}</strong>
        · {{ currentCycle.weekStartDate?.substring(0, 10) }}
        to {{ currentCycle.weekEndDate?.substring(0, 10) }}
        <div class="small mt-1">
          Exact OR date and time will be assigned by the scheduler based on available block capacity.
        </div>
      </div>

      <div v-else class="alert alert-warning">
        <i class="bi bi-exclamation-triangle me-2"></i>
        No current scheduling cycle was found.
        <div class="small mt-1">
          Emergency requests can still be submitted for immediate scheduler review.
        </div>
      </div>

      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label">Patient</label>
          <select v-model="form.patientId" class="form-select">
            <option value="">Select patient</option>
            <option
              v-for="patient in patients"
              :key="patient.patientId"
              :value="patient.patientId"
            >
              {{ patient.fullName }} - {{ patient.mrn }}
            </option>
          </select>
        </div>

        <div class="col-md-6">
          <label class="form-label">Surgery Type</label>
          <input
            v-model="form.surgeryType"
            class="form-control"
            placeholder="e.g. Knee Replacement"
          />
        </div>

        <div class="col-md-3">
          <label class="form-label">Estimated Duration</label>
          <div class="input-group">
            <input
              v-model.number="form.estimatedDurationMin"
              type="number"
              class="form-control"
              min="1"
              placeholder="e.g. 120"
            />
            <span class="input-group-text">min</span>
          </div>
          <small class="text-muted">
            Scheduler will allocate block time using this duration.
          </small>
        </div>

        <div class="col-md-3">
          <label class="form-label">Priority</label>
          <select v-model="form.priority" class="form-select">
            <option value="Elective">Elective</option>
            <option value="Urgent">Urgent</option>
            <option value="Emergency">Emergency</option>
          </select>
        </div>

        <div class="col-md-3">
          <label class="form-label">Patient Readiness</label>
          <select v-model="form.patientReadiness" class="form-select">
            <option value="Ready">Ready</option>
            <option value="PendingClearance">Pending Clearance</option>
            <option value="NotReady">Not Ready</option>
          </select>
        </div>

        <div class="col-md-3">
          <label class="form-label">Scheduling Mode</label>
          <input
            class="form-control"
            :value="form.priority === 'Emergency' ? 'Emergency bypass' : 'Cycle based review'"
            disabled
          />
        </div>

        <div class="col-md-12">
          <div class="alert alert-light border mb-0">
            <div class="fw-semibold mb-1">
              Scheduling details are now assigned by the scheduler.
            </div>
            <div class="small text-muted">
              Preferred date, available days, and preferred quarter are no longer selected by the surgeon.
              The system defaults requests to flexible weekday scheduling for the active planning cycle.
            </div>
          </div>
        </div>

        <div class="col-md-12">
          <label class="form-label">Remarks</label>
          <textarea
            v-model="form.remarks"
            class="form-control"
            rows="3"
            placeholder="Optional notes for scheduler"
          ></textarea>
        </div>
      </div>

      <div class="text-end mt-4">
        <button
          class="btn btn-primary"
          :disabled="submitting"
          @click="submitRequest"
        >
          <span
            v-if="submitting"
            class="spinner-border spinner-border-sm me-2"
          ></span>
          Submit Request
        </button>
      </div>
    </div>
  </div>
</template>