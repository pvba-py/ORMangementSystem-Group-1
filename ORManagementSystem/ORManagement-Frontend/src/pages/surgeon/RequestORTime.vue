<script setup>
import { onMounted, ref, computed } from 'vue'
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

const selectedDates = ref([])
const draftSelectedDates = ref([])

const calendarOpen = ref(false)
const calendarYear = ref(new Date().getFullYear())
const calendarMonth = ref(new Date().getMonth())

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
  availableDaysMask: 0
})

const today = computed(() => {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
})

const cycleStartDate = computed(() => {
  return currentCycle.value?.weekStartDate?.substring(0, 10) || ''
})

const cycleEndDate = computed(() => {
  return currentCycle.value?.weekEndDate?.substring(0, 10) || ''
})

const monthTitle = computed(() => {
  return new Date(calendarYear.value, calendarMonth.value, 1).toLocaleDateString(
    'en-US',
    {
      month: 'long',
      year: 'numeric'
    }
  )
})

const selectedDatesDisplay = computed(() => {
  if (!selectedDates.value.length) {
    return 'Select available dates'
  }

  if (selectedDates.value.length === 1) {
    return formatShortSelectedDate(selectedDates.value[0])
  }

  return selectedDates.value
    .map(date => formatShortSelectedDate(date))
    .join(', ')
})

const parseDateOnly = dateString => {
  const [year, month, day] = dateString.split('-').map(Number)
  return new Date(year, month - 1, day)
}

const formatDateOnly = date => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const formatShortSelectedDate = dateString => {
  const date = parseDateOnly(dateString)

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric'
  })
}

const isOutsideCurrentCycle = dateString => {
  if (!cycleStartDate.value || !cycleEndDate.value) {
    return true
  }

  return dateString < cycleStartDate.value || dateString > cycleEndDate.value
}

const calendarDays = computed(() => {
  const days = []

  const firstDayOfMonth = new Date(calendarYear.value, calendarMonth.value, 1)
  const lastDayOfMonth = new Date(calendarYear.value, calendarMonth.value + 1, 0)

  const leadingEmptyDays = firstDayOfMonth.getDay()

  for (let i = 0; i < leadingEmptyDays; i++) {
    days.push(null)
  }

  for (let day = 1; day <= lastDayOfMonth.getDate(); day++) {
    const date = new Date(calendarYear.value, calendarMonth.value, day)
    const dateString = formatDateOnly(date)
    const dayOfWeek = date.getDay()

    days.push({
      date,
      dateString,
      dayNumber: day,
      isWeekend: dayOfWeek === 0 || dayOfWeek === 6,
      isPast: dateString < today.value,
      isOutsideCycle: isOutsideCurrentCycle(dateString),
      isToday: dateString === today.value,
      isSelected: draftSelectedDates.value.includes(dateString)
    })
  }

  return days
})

const getDayMaskFromDateString = dateString => {
  const date = parseDateOnly(dateString)

  switch (date.getDay()) {
    case 1:
      return 1
    case 2:
      return 2
    case 3:
      return 4
    case 4:
      return 8
    case 5:
      return 16
    default:
      return 0
  }
}

const updateAvailabilityFromSelectedDates = () => {
  let mask = 0

  selectedDates.value.forEach(dateString => {
    mask |= getDayMaskFromDateString(dateString)
  })

  form.value.availableDaysMask = mask
  form.value.preferredDate = selectedDates.value.length
    ? selectedDates.value[0]
    : ''
}

const selectedDayNames = computed(() => {
  const mask = Number(form.value.availableDaysMask)
  const days = []

  if ((mask & 1) === 1) days.push('Mon')
  if ((mask & 2) === 2) days.push('Tue')
  if ((mask & 4) === 4) days.push('Wed')
  if ((mask & 8) === 8) days.push('Thu')
  if ((mask & 16) === 16) days.push('Fri')

  return days
})

const availableDaysDisplay = computed(() => {
  if (!selectedDayNames.value.length) {
    return 'No dates selected'
  }

  return selectedDayNames.value.join(', ')
})

const selectedDatesFullDisplay = computed(() => {
  if (!selectedDates.value.length) {
    return 'No dates selected'
  }

  return selectedDates.value
    .map(date => {
      const parsedDate = parseDateOnly(date)

      return parsedDate.toLocaleDateString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric'
      })
    })
    .join(', ')
})

const openCalendar = () => {
  if (!currentCycle.value) {
    showToast('Current scheduling cycle is not available.', 'warning')
    return
  }

  draftSelectedDates.value = [...selectedDates.value]

  if (cycleStartDate.value) {
    const cycleStart = parseDateOnly(cycleStartDate.value)
    calendarYear.value = cycleStart.getFullYear()
    calendarMonth.value = cycleStart.getMonth()
  }

  calendarOpen.value = true
}

const closeCalendar = () => {
  draftSelectedDates.value = [...selectedDates.value]
  calendarOpen.value = false
}

const toggleDraftDate = day => {
  if (!day || day.isWeekend || day.isPast || day.isOutsideCycle) {
    return
  }

  const exists = draftSelectedDates.value.includes(day.dateString)

  if (exists) {
    draftSelectedDates.value = draftSelectedDates.value.filter(
      date => date !== day.dateString
    )
  } else {
    draftSelectedDates.value.push(day.dateString)
    draftSelectedDates.value.sort()
  }
}

const confirmDates = () => {
  selectedDates.value = [...draftSelectedDates.value].sort()
  updateAvailabilityFromSelectedDates()
  calendarOpen.value = false
}

const clearDraftDates = () => {
  draftSelectedDates.value = []
}

const removePreferredDate = dateString => {
  selectedDates.value = selectedDates.value.filter(date => date !== dateString)
  updateAvailabilityFromSelectedDates()
}

const canGoToPreviousMonth = computed(() => {
  if (!cycleStartDate.value) {
    return false
  }

  const cycleStart = parseDateOnly(cycleStartDate.value)
  const currentMonthStart = new Date(calendarYear.value, calendarMonth.value, 1)

  return currentMonthStart > new Date(cycleStart.getFullYear(), cycleStart.getMonth(), 1)
})

const canGoToNextMonth = computed(() => {
  if (!cycleEndDate.value) {
    return false
  }

  const cycleEnd = parseDateOnly(cycleEndDate.value)
  const currentMonthStart = new Date(calendarYear.value, calendarMonth.value, 1)

  return currentMonthStart < new Date(cycleEnd.getFullYear(), cycleEnd.getMonth(), 1)
})

const goToPreviousMonth = () => {
  if (!canGoToPreviousMonth.value) {
    return
  }

  if (calendarMonth.value === 0) {
    calendarMonth.value = 11
    calendarYear.value -= 1
  } else {
    calendarMonth.value -= 1
  }
}

const goToNextMonth = () => {
  if (!canGoToNextMonth.value) {
    return
  }

  if (calendarMonth.value === 11) {
    calendarMonth.value = 0
    calendarYear.value += 1
  } else {
    calendarMonth.value += 1
  }
}

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

      const cycleStart = parseDateOnly(currentCycle.value.weekStartDate.substring(0, 10))
      calendarYear.value = cycleStart.getFullYear()
      calendarMonth.value = cycleStart.getMonth()
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

  if (!selectedDates.value.length) {
    showToast('Please select at least one available date.', 'warning')
    return false
  }

  if (!form.value.preferredDate) {
    showToast('Preferred date is required.', 'warning')
    return false
  }

  if (!form.value.availableDaysMask || form.value.availableDaysMask <= 0) {
    showToast('Available days could not be calculated.', 'warning')
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

      <div v-else class="alert alert-warning">
        <i class="bi bi-exclamation-triangle me-2"></i>
        No current scheduling cycle was found.
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

        <div class="col-md-6 position-relative">
          <label class="form-label">Available Dates in Cycle</label>

          <button
            type="button"
            class="form-control date-picker-trigger d-flex justify-content-between align-items-center"
            @click="openCalendar"
          >
            <span>{{ selectedDatesDisplay }}</span>
            <i class="bi bi-calendar3"></i>
          </button>

          <div
            v-if="calendarOpen"
            class="calendar-popup border bg-white shadow-lg"
          >
            <div class="calendar-header">
              <button
                type="button"
                class="calendar-nav-btn"
                :disabled="!canGoToPreviousMonth"
                @click="goToPreviousMonth"
              >
                <i class="bi bi-chevron-left"></i>
              </button>

              <div class="text-center">
                <div class="calendar-title">{{ monthTitle }}</div>
                <div class="calendar-subtitle">
                  {{ draftSelectedDates.length }} selected
                </div>
              </div>

              <button
                type="button"
                class="calendar-nav-btn"
                :disabled="!canGoToNextMonth"
                @click="goToNextMonth"
              >
                <i class="bi bi-chevron-right"></i>
              </button>
            </div>

            <div class="calendar-weekdays">
              <div>Sun</div>
              <div>Mon</div>
              <div>Tue</div>
              <div>Wed</div>
              <div>Thu</div>
              <div>Fri</div>
              <div>Sat</div>
            </div>

            <div class="calendar-grid">
              <button
                v-for="(day, index) in calendarDays"
                :key="day ? day.dateString : `empty-${index}`"
                type="button"
                class="calendar-day"
                :class="{
                  'empty-day': !day,
                  'disabled-day': day && (day.isWeekend || day.isPast || day.isOutsideCycle),
                  'selected-day': day && day.isSelected,
                  'today-day': day && day.isToday && !day.isSelected
                }"
                :disabled="!day || day.isWeekend || day.isPast || day.isOutsideCycle"
                @click="toggleDraftDate(day)"
              >
                {{ day ? day.dayNumber : '' }}
              </button>
            </div>

            <div class="calendar-help mt-2">
              <i class="bi bi-info-circle me-1"></i>
              Only weekdays inside the current scheduling cycle can be selected.
            </div>

            <div class="calendar-actions">
              <button
                type="button"
                class="btn btn-sm btn-outline-secondary"
                @click="clearDraftDates"
              >
                Clear
              </button>

              <div class="d-flex gap-2">
                <button
                  type="button"
                  class="btn btn-sm btn-light border"
                  @click="closeCalendar"
                >
                  Cancel
                </button>

                <button
                  type="button"
                  class="btn btn-sm btn-primary"
                  @click="confirmDates"
                >
                  Confirm Dates
                </button>
              </div>
            </div>
          </div>

          <div v-if="selectedDates.length" class="selected-date-list mt-2">
            <span
              v-for="date in selectedDates"
              :key="date"
              class="selected-date-chip"
            >
              <i class="bi bi-calendar-check me-1"></i>
              {{ formatShortSelectedDate(date) }}
              <button
                type="button"
                class="chip-remove-btn"
                aria-label="Remove"
                @click="removePreferredDate(date)"
              >
                ×
              </button>
            </span>
          </div>

          <small v-else class="text-muted d-block mt-1">
            Select one or more weekdays from the current cycle.
          </small>
        </div>

        <div class="col-md-6">
          <label class="form-label">Selected Dates</label>
          <input
            class="form-control"
            :value="selectedDatesFullDisplay"
            disabled
          />
        </div>

        <div class="col-md-6">
          <label class="form-label">Available Days</label>
          <input
            class="form-control"
            :value="availableDaysDisplay"
            disabled
          />
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

        <div class="col-md-3">
          <label class="form-label">Patient Readiness</label>
          <select v-model="form.patientReadiness" class="form-select">
            <option value="Ready">Ready</option>
            <option value="PendingClearance">Pending Clearance</option>
            <option value="NotReady">Not Ready</option>
          </select>
        </div>

        <div class="col-md-6">
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

<style scoped>
.date-picker-trigger {
  background: #ffffff;
  cursor: pointer;
  color: #212529;
}

.date-picker-trigger:hover {
  border-color: #86b7fe;
  box-shadow: 0 0 0 0.15rem rgba(13, 110, 253, 0.12);
}

.calendar-popup {
  position: absolute;
  z-index: 1050;
  width: 380px;
  max-width: 92vw;
  border-radius: 14px;
  padding: 14px;
  margin-top: 6px;
}

.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 12px;
  border-bottom: 1px solid #eef0f2;
}

.calendar-title {
  font-weight: 700;
  font-size: 0.98rem;
  color: #212529;
}

.calendar-subtitle {
  font-size: 0.75rem;
  color: #6c757d;
  margin-top: 2px;
}

.calendar-nav-btn {
  width: 32px;
  height: 32px;
  border: 1px solid #dee2e6;
  background: #ffffff;
  border-radius: 50%;
  color: #495057;
  display: flex;
  align-items: center;
  justify-content: center;
}

.calendar-nav-btn:hover:not(:disabled) {
  background: #f1f5ff;
  color: #0d6efd;
  border-color: #b6d4fe;
}

.calendar-nav-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.calendar-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 6px;
  margin-top: 12px;
  margin-bottom: 6px;
  text-align: center;
  font-size: 0.72rem;
  font-weight: 700;
  color: #6c757d;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 6px;
}

.calendar-day {
  height: 38px;
  border: 1px solid transparent;
  background: #f8f9fa;
  border-radius: 10px;
  color: #212529;
  font-size: 0.875rem;
  font-weight: 500;
  transition: all 0.15s ease-in-out;
}

.calendar-day:hover:not(:disabled) {
  background: #e7f1ff;
  border-color: #86b7fe;
  color: #0d6efd;
  transform: translateY(-1px);
}

.selected-day {
  background: #0d6efd !important;
  color: #ffffff !important;
  border-color: #0d6efd !important;
  box-shadow: 0 4px 10px rgba(13, 110, 253, 0.25);
}

.today-day {
  border-color: #0d6efd;
  background: #ffffff;
  color: #0d6efd;
  font-weight: 700;
}

.disabled-day {
  background: #f1f3f5 !important;
  color: #adb5bd !important;
  cursor: not-allowed;
  text-decoration: line-through;
}

.empty-day {
  visibility: hidden;
}

.calendar-help {
  font-size: 0.75rem;
  color: #6c757d;
  background: #f8f9fa;
  border-radius: 8px;
  padding: 7px 9px;
}

.calendar-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid #eef0f2;
}

.selected-date-list {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.selected-date-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: #e7f1ff;
  color: #084298;
  border: 1px solid #b6d4fe;
  border-radius: 999px;
  padding: 4px 8px;
  font-size: 0.75rem;
  font-weight: 600;
}

.chip-remove-btn {
  border: none;
  background: transparent;
  color: #084298;
  font-size: 1rem;
  line-height: 1;
  padding: 0 0 0 4px;
  cursor: pointer;
}

.chip-remove-btn:hover {
  color: #dc3545;
}

@media (max-width: 576px) {
  .calendar-popup {
    left: 0;
    right: 0;
    width: 100%;
  }

  .calendar-day {
    height: 34px;
    font-size: 0.8rem;
  }
}
</style>