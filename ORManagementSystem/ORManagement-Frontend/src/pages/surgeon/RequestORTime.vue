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
  cycleId: null,
  preferredDate: '',
  preferredQuarter: 'Flexible',
  estimatedDurationMin: 60,
  surgeryType: '',
  priority: 'Elective',
  patientReadiness: 'Ready',
  remarks: '',
  availableDaysMask: 31
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

    if (currentCycle.value?.cycleId) {
      form.value.cycleId = currentCycle.value.cycleId
    }
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

  if (!form.value.preferredDate) {
    showToast('Preferred date is required.', 'warning')
    return false
  }

  if (!form.value.surgeryType.trim()) {
    showToast('Surgery type is required.', 'warning')
    return false
  }

  if (!form.value.estimatedDurationMin || form.value.estimatedDurationMin <= 0) {
    showToast('Estimated duration must be greater than zero.', 'warning')
    return false
  }

  return true
}

const submitRequest = async () => {
  if (!validate()) return

  submitting.value = true

  try {
    await createRequest({
      patientId: Number(form.value.patientId),
      cycleId: form.value.priority === 'Emergency'
        ? null
        : form.value.cycleId,
      preferredDate: form.value.preferredDate,
      preferredQuarter: form.value.preferredQuarter,
      estimatedDurationMin: Number(form.value.estimatedDurationMin),
      surgeryType: form.value.surgeryType.trim(),
      priority: form.value.priority,
      patientReadiness: form.value.patientReadiness,
      remarks: form.value.remarks,
      availableDaysMask: Number(form.value.availableDaysMask)
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
      subtitle="Submit a new OR time request for scheduler review"
      icon="bi-plus-circle"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else class="page-card">
      <div v-if="currentCycle" class="alert alert-info">
        <i class="bi bi-calendar-week me-2"></i>
        Current cycle:
        <strong>#{{ currentCycle.cycleId }}</strong>
        · {{ currentCycle.weekStartDate?.substring(0, 10) }}
        to {{ currentCycle.weekEndDate?.substring(0, 10) }}
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

        <div class="col-md-3">
          <label class="form-label">Preferred Date</label>
          <input v-model="form.preferredDate" type="date" class="form-control" />
        </div>

        <div class="col-md-3">
          <label class="form-label">Preferred Quarter</label>
          <select v-model="form.preferredQuarter" class="form-select">
            <option value="Q1">Q1</option>
            <option value="Q2">Q2</option>
            <option value="Flexible">Flexible</option>
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
          <input
            v-model.number="form.estimatedDurationMin"
            type="number"
            class="form-control"
            min="1"
          />
        </div>

        <div class="col-md-3">
          <label class="form-label">Priority</label>
          <select v-model="form.priority" class="form-select">
            <option value="Elective">Elective</option>
            <option value="Urgent">Urgent</option>
            <option value="Emergency">Emergency</option>
          </select>
        </div>

        <div class="col-md-4">
          <label class="form-label">Patient Readiness</label>
          <select v-model="form.patientReadiness" class="form-select">
            <option value="Ready">Ready</option>
            <option value="PendingClearance">Pending Clearance</option>
            <option value="NotReady">Not Ready</option>
          </select>
        </div>

        <div class="col-md-4">
          <label class="form-label">Available Days</label>
          <select v-model.number="form.availableDaysMask" class="form-select">
            <option :value="31">Mon-Fri</option>
            <option :value="3">Mon-Tue</option>
            <option :value="1">Monday Only</option>
            <option :value="4">Wednesday Only</option>
            <option :value="28">Wed-Fri</option>
          </select>
        </div>

        <div class="col-md-4">
          <label class="form-label">Cycle</label>
          <input
            class="form-control"
            :value="form.priority === 'Emergency' ? 'Emergency bypass' : form.cycleId"
            disabled
          />
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
          <span v-if="submitting" class="spinner-border spinner-border-sm me-2"></span>
          Submit Request
        </button>
      </div>
    </div>
  </div>
</template>