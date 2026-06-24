<script setup>
import { computed, onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getCurrentCycle,
  cutoffCycle,
  publishCycle
} from '../../services/cycleService'
import { getCalendar } from '../../services/roomService'
import { formatDate, formatDateTime, formatTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const actionLoading = ref(false)

const currentCycle = ref(null)
const calendar = ref([])

const calendarHours = Array.from({ length: 24 }, (_, index) => index)

const parseDateOnly = dateValue => {
  if (!dateValue) return null

  const datePart = String(dateValue).substring(0, 10)
  const [year, month, day] = datePart.split('-').map(Number)

  return new Date(year, month - 1, day)
}

const formatDateOnly = date => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const addDays = (date, days) => {
  const copy = new Date(date)
  copy.setDate(copy.getDate() + days)
  return copy
}

const formatHourLabel = hour => {
  return `${String(hour).padStart(2, '0')}:00`
}

const getMinutesFromTimeValue = value => {
  if (!value) return null

  const time = formatTime(value).substring(0, 5)
  const [hour, minute] = time.split(':').map(Number)

  if (Number.isNaN(hour) || Number.isNaN(minute)) {
    return null
  }

  return hour * 60 + minute
}

const getMinutesFromDateTimeValue = value => {
  if (!value) return null

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return null
  }

  return date.getHours() * 60 + date.getMinutes()
}

const overlapsHourInclusive = (startMinutes, endMinutes, hour) => {
  if (startMinutes === null || endMinutes === null) {
    return false
  }

  const slotStart = hour * 60
  const slotEnd = (hour + 1) * 60

  /*
    Inclusive behavior:
    08:00–10:00 appears in 08, 09, and 10 rows.
  */
  return startMinutes <= slotEnd && endMinutes >= slotStart
}

const cycleWeekDays = computed(() => {
  if (!currentCycle.value?.weekStartDate) {
    return []
  }

  const startDate = parseDateOnly(currentCycle.value.weekStartDate)

  if (!startDate) {
    return []
  }

  return [0, 1, 2, 3, 4].map(offset => {
    const date = addDays(startDate, offset)

    return {
      label: date.toLocaleDateString('en-US', { weekday: 'short' }),
      fullLabel: date.toLocaleDateString('en-US', { weekday: 'long' }),
      date,
      dateString: formatDateOnly(date)
    }
  })
})

const calendarBlocks = computed(() => {
  const map = new Map()

  calendar.value.forEach(item => {
    const blockDate = String(item.blockDate || '').substring(0, 10)

    const blockKey = item.blockId
      ? `block-${item.blockId}`
      : `${item.roomName}-${blockDate}-${item.startTime}-${item.endTime}-${item.blockType || ''}`

    if (!map.has(blockKey)) {
      map.set(blockKey, {
        blockId: item.blockId,
        roomName: item.roomName,
        blockDate,
        startTime: item.startTime,
        endTime: item.endTime,
        blockStatus: item.blockStatus,
        blockType: item.blockType || item.type || 'Block',
        surgeonName: item.surgeonName || `${item.blockType || 'Open'} Capacity`,
        cases: []
      })
    }

    if (item.surgeryId) {
      map.get(blockKey).cases.push({
        surgeryId: item.surgeryId,
        scheduledStart: item.scheduledStart,
        scheduledEnd: item.scheduledEnd,
        caseStatus: item.caseStatus,
        patientName: item.patientName,
        surgeryType: item.surgeryType,
        surgeonName: item.surgeonName
      })
    }
  })

  return Array.from(map.values())
})

const getBlocksForCell = (dayString, hour) => {
  return calendarBlocks.value.filter(block => {
    if (block.blockDate !== dayString) {
      return false
    }

    const blockStartMinutes = getMinutesFromTimeValue(block.startTime)
    const blockEndMinutes = getMinutesFromTimeValue(block.endTime)

    return overlapsHourInclusive(blockStartMinutes, blockEndMinutes, hour)
  })
}

const getCasesForBlockCell = (block, hour) => {
  return block.cases.filter(caseItem => {
    const caseStartMinutes = getMinutesFromDateTimeValue(caseItem.scheduledStart)
    const caseEndMinutes = getMinutesFromDateTimeValue(caseItem.scheduledEnd)

    return overlapsHourInclusive(caseStartMinutes, caseEndMinutes, hour)
  })
}

const getBlockTypeClass = blockType => {
  return {
    'calendar-block-recurring': blockType === 'Recurring',
    'calendar-block-open': blockType === 'Open',
    'calendar-block-emergency': blockType === 'Emergency',
    'calendar-block-adhoc': blockType === 'AdHoc'
  }
}

const loadCalendar = async () => {
  if (!currentCycle.value?.weekStartDate || !currentCycle.value?.weekEndDate) {
    calendar.value = []
    return
  }

  try {
    const response = await getCalendar({
      fromDate: currentCycle.value.weekStartDate.substring(0, 10),
      toDate: currentCycle.value.weekEndDate.substring(0, 10)
    })

    calendar.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load cycle calendar.'

    showToast(message, 'error')
  }
}

const loadCycle = async () => {
  loading.value = true

  try {
    const response = await getCurrentCycle()
    currentCycle.value = response.data || null

    if (currentCycle.value?.cycleId) {
      await loadCalendar()
    } else {
      calendar.value = []
    }
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load current cycle.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const handleCutoff = async () => {
  if (!currentCycle.value?.cycleId) return

  if (!confirm(`Cutoff cycle #${currentCycle.value.cycleId}?`)) {
    return
  }

  actionLoading.value = true

  try {
    await cutoffCycle(currentCycle.value.cycleId)
    showToast('Cycle cutoff completed successfully.', 'success')
    await loadCycle()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to cutoff cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
  }
}

const handlePublish = async () => {
  if (!currentCycle.value?.cycleId) return

  if (!confirm(`Publish cycle #${currentCycle.value.cycleId}?`)) {
    return
  }

  actionLoading.value = true

  try {
    await publishCycle(currentCycle.value.cycleId)
    showToast('Cycle published successfully.', 'success')
    await loadCycle()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to publish cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
  }
}

onMounted(loadCycle)
</script>

<template>
  <div>
    <PageHeader
      title="Cycle Management"
      subtitle="Manage the current weekly scheduling cycle and view the OR schedule calendar"
      icon="bi-arrow-repeat"
    >
      <template #actions>
        <button
          class="btn btn-outline-primary me-2"
          :disabled="loading || actionLoading"
          @click="loadCycle"
        >
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <div v-if="currentCycle" class="page-card mb-4">
        <div class="row g-3 align-items-center">
          <div class="col-md-3">
            <small class="text-muted">Cycle ID</small>
            <h5 class="mb-0">#{{ currentCycle.cycleId }}</h5>
          </div>

          <div class="col-md-3">
            <small class="text-muted">Week</small>
            <h6 class="mb-0">
              {{ formatDate(currentCycle.weekStartDate) }}
              -
              {{ formatDate(currentCycle.weekEndDate) }}
            </h6>
          </div>

          <div class="col-md-3">
            <small class="text-muted">Cutoff At</small>
            <h6 class="mb-0">
              {{ formatDateTime(currentCycle.cutoffAt) }}
            </h6>
          </div>

          <div class="col-md-3">
            <small class="text-muted d-block">Status</small>
            <StatusBadge :status="currentCycle.cycleStatus" />
          </div>
        </div>

        <hr />

        <div class="d-flex justify-content-end gap-2">
          <button
            class="btn btn-warning"
            :disabled="actionLoading || currentCycle.cycleStatus !== 'Open'"
            @click="handleCutoff"
          >
            <span
              v-if="actionLoading"
              class="spinner-border spinner-border-sm me-2"
            ></span>
            Cutoff Cycle
          </button>

          <button
            class="btn btn-success"
            :disabled="
              actionLoading ||
              !['Cutoff', 'Scheduling'].includes(currentCycle.cycleStatus)
            "
            @click="handlePublish"
          >
            <span
              v-if="actionLoading"
              class="spinner-border spinner-border-sm me-2"
            ></span>
            Publish Schedule
          </button>
        </div>
      </div>

      <EmptyState
        v-else
        title="No current cycle"
        message="No open scheduling cycle was found."
        icon="bi-calendar-x"
      />

      <!-- Weekly OR Calendar -->
      <div v-if="currentCycle" class="page-card">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h5 class="mb-0">
              <i class="bi bi-calendar-week me-2 text-primary"></i>
              Weekly OR Calendar
            </h5>
            <small class="text-muted">
              Blocks and scheduled cases for the selected cycle week.
            </small>
          </div>

          <button
            class="btn btn-sm btn-outline-primary"
            @click="loadCalendar"
          >
            <i class="bi bi-arrow-clockwise me-1"></i>
            Refresh Calendar
          </button>
        </div>

        <EmptyState
          v-if="calendarBlocks.length === 0"
          title="No blocks or cases"
          message="No generated blocks or scheduled cases were found for this cycle."
          icon="bi-calendar-x"
        />

        <div v-else class="weekly-calendar-wrapper">
          <div class="weekly-calendar">
            <div class="calendar-header-cell time-header">
              Time
            </div>

            <div
              v-for="day in cycleWeekDays"
              :key="day.dateString"
              class="calendar-header-cell day-header"
            >
              <div class="fw-semibold">{{ day.fullLabel }}</div>
              <small class="text-muted">{{ formatDate(day.dateString) }}</small>
            </div>

            <template
              v-for="hour in calendarHours"
              :key="hour"
            >
              <div class="calendar-time-cell">
                {{ formatHourLabel(hour) }}
              </div>

              <div
                v-for="day in cycleWeekDays"
                :key="`${day.dateString}-${hour}`"
                class="calendar-day-cell"
              >
                <div
                  v-for="block in getBlocksForCell(day.dateString, hour)"
                  :key="`${block.blockId || block.roomName}-${block.startTime}-${block.endTime}-${hour}`"
                  class="calendar-block-card"
                  :class="getBlockTypeClass(block.blockType)"
                >
                  <div class="d-flex justify-content-between align-items-start gap-2">
                    <div>
                      <span class="calendar-block-type">
                        {{ block.blockType }}
                      </span>

                      <div class="calendar-block-title">
                        {{ block.roomName }}
                      </div>

                      <div class="calendar-block-owner">
                        {{ block.surgeonName || `${block.blockType} Capacity` }}
                      </div>
                    </div>

                    <StatusBadge :status="block.blockStatus" />
                  </div>

                  <div class="calendar-block-time mt-1">
                    {{ formatTime(block.startTime) }} -
                    {{ formatTime(block.endTime) }}
                  </div>

                  <div
                    v-if="getCasesForBlockCell(block, hour).length > 0"
                    class="calendar-case-list mt-2"
                  >
                    <div
                      v-for="caseItem in getCasesForBlockCell(block, hour)"
                      :key="`${caseItem.surgeryId}-${hour}`"
                      class="calendar-case-item"
                    >
                      <div class="d-flex justify-content-between align-items-start gap-2">
                        <div>
                          <div class="calendar-case-title">
                            Case #{{ caseItem.surgeryId }}
                          </div>

                          <div
                            v-if="caseItem.patientName"
                            class="calendar-case-detail"
                          >
                            Patient: {{ caseItem.patientName }}
                          </div>

                          <div
                            v-if="caseItem.surgeryType"
                            class="calendar-case-detail"
                          >
                            Surgery: {{ caseItem.surgeryType }}
                          </div>

                          <div class="calendar-case-time">
                            {{ formatDateTime(caseItem.scheduledStart) }}
                            to
                            {{ formatDateTime(caseItem.scheduledEnd) }}
                          </div>
                        </div>

                        <StatusBadge
                          v-if="caseItem.caseStatus"
                          :status="caseItem.caseStatus"
                        />
                      </div>
                    </div>
                  </div>

                  <div v-else class="small text-muted mt-2">
                    No case scheduled in this hour
                  </div>
                </div>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.weekly-calendar-wrapper {
  overflow-x: auto;
  border: 2px solid #cbd5e1;
  border-radius: 14px;
  background: #ffffff;
}

.weekly-calendar {
  display: grid;
  grid-template-columns: 90px repeat(5, minmax(250px, 1fr));
  min-width: 1320px;
  background: #ffffff;
}

.calendar-header-cell {
  position: sticky;
  top: 0;
  z-index: 2;
  background: #f1f5f9;
  border-bottom: 2px solid #cbd5e1;
  border-right: 1px solid #cbd5e1;
  padding: 12px;
}

.time-header {
  left: 0;
  z-index: 3;
}

.day-header {
  text-align: center;
}

.calendar-time-cell {
  background: #f8fafc;
  border-right: 2px solid #cbd5e1;
  border-bottom: 1px solid #dbe3ef;
  padding: 10px;
  font-weight: 700;
  color: #475569;
  min-height: 120px;
}

.calendar-day-cell {
  border-right: 1px solid #dbe3ef;
  border-bottom: 1px solid #dbe3ef;
  padding: 8px;
  min-height: 120px;
  background: #ffffff;
}

.calendar-day-cell:hover {
  background: #f8fafc;
}

.calendar-block-card {
  border-radius: 12px;
  padding: 10px;
  border: 2px solid #334155;
  background: #ffffff;
  margin-bottom: 8px;
  box-shadow: 0 2px 5px rgba(15, 23, 42, 0.12);
}

.calendar-block-type {
  display: inline-block;
  font-size: 0.68rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 4px;
  color: #1e293b;
}

.calendar-block-title {
  font-size: 0.95rem;
  font-weight: 800;
  color: #0f172a;
}

.calendar-block-owner {
  font-size: 0.78rem;
  font-weight: 600;
  color: #475569;
}

.calendar-block-time {
  font-size: 0.78rem;
  font-weight: 700;
  color: #334155;
}

.calendar-block-recurring {
  border-left: 7px solid #0d6efd;
  background: #eef5ff;
}

.calendar-block-open {
  border-left: 7px solid #198754;
  background: #eefaf3;
}

.calendar-block-emergency {
  border-left: 7px solid #dc3545;
  background: #fff1f2;
}

.calendar-block-adhoc {
  border-left: 7px solid #ffc107;
  background: #fff8e1;
}

.calendar-case-list {
  border-top: 2px dashed #94a3b8;
  padding-top: 8px;
}

.calendar-case-item {
  background: #ffffff;
  border: 2px solid #64748b;
  border-radius: 10px;
  padding: 8px;
  margin-top: 6px;
}

.calendar-case-title {
  font-size: 0.86rem;
  font-weight: 800;
  color: #0f172a;
}

.calendar-case-detail {
  font-size: 0.76rem;
  color: #334155;
  margin-top: 2px;
}

.calendar-case-time {
  font-size: 0.74rem;
  color: #475569;
  margin-top: 4px;
  font-weight: 600;
}
</style>