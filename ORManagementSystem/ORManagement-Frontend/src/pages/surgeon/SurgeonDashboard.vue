<script setup>
import { computed, onMounted, ref } from 'vue'

import PageHeader from '../../components/common/PageHeader.vue'
import KpiCard from '../../components/common/KpiCard.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'

import { getSurgeonDashboard } from '../../services/dashboardService'
import { getCurrentCycle } from '../../services/cycleService'
import { getMyCalendar } from '../../services/roomService'

import {
  formatDate,
  formatDateTime,
  formatPercent,
  formatTime
} from '../../utils/formatters'

import { showToast } from '../../utils/toast'

const loading = ref(false)

const dashboard = ref(null)
const currentCycle = ref(null)
const myCalendar = ref([])

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

  // 08:00–10:00 appears in 08, 09, and 10 rows.
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

  myCalendar.value.forEach(item => {
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
        blockType: item.blockType || 'Block',
        surgeonName: item.surgeonName || `${item.blockType || 'Block'} Capacity`,
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
        patientMrn: item.patientMrn,
        surgeryType: item.surgeryType
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

const loadMyCalendar = async () => {
  if (!currentCycle.value?.weekStartDate || !currentCycle.value?.weekEndDate) {
    myCalendar.value = []
    return
  }

  try {
    const response = await getMyCalendar({
      fromDate: currentCycle.value.weekStartDate.substring(0, 10),
      toDate: currentCycle.value.weekEndDate.substring(0, 10)
    })

    myCalendar.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load your calendar.'

    showToast(message, 'error')
  }
}

const loadDashboard = async () => {
  loading.value = true

  try {
    const [dashboardResponse, cycleResponse] = await Promise.all([
      getSurgeonDashboard(),
      getCurrentCycle()
    ])

    dashboard.value = dashboardResponse.data
    currentCycle.value = cycleResponse.data || null

    await loadMyCalendar()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load surgeon dashboard.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)
</script>

<template>
  <div>
    <PageHeader
      title="Surgeon Dashboard"
      subtitle="View your OR requests, personal schedule, assigned blocks, and utilization"
      icon="bi-speedometer2"
    >
      <template #actions>
        <router-link
          to="/app/surgeon/request-or-time"
          class="btn btn-primary"
        >
          <i class="bi bi-plus-circle me-1"></i>
          Request OR Time
        </router-link>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else-if="dashboard">
      <!-- KPI cards -->
      <div class="row g-3 mb-4">
        <div class="col-md-3">
          <KpiCard
            label="Assigned Blocks"
            :value="dashboard.assignedBlocks"
            icon="bi-calendar-week"
            color="primary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Upcoming Blocks"
            :value="dashboard.upcomingBlocks"
            icon="bi-calendar-event"
            color="info"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Upcoming Surgeries"
            :value="dashboard.upcomingSurgeries"
            icon="bi-hospital"
            color="success"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Avg Utilization"
            :value="formatPercent(dashboard.averageUtilizationPercent)"
            icon="bi-bar-chart"
            color="warning"
          />
        </div>
      </div>

      <!-- Request status cards -->
      <div class="row g-3 mb-4">
        <div class="col-md-3">
          <KpiCard
            label="Pending Requests"
            :value="dashboard.pendingRequests"
            icon="bi-hourglass-split"
            color="primary"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Approved Requests"
            :value="dashboard.approvedRequests"
            icon="bi-check-circle"
            color="success"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Waitlisted Requests"
            :value="dashboard.waitlistedRequests"
            icon="bi-list-ol"
            color="warning"
          />
        </div>

        <div class="col-md-3">
          <KpiCard
            label="Scheduled Requests"
            :value="dashboard.scheduledRequests"
            icon="bi-calendar-check"
            color="secondary"
          />
        </div>
      </div>

      <!-- Personal Weekly Calendar -->
      <div class="page-card mb-4">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h5 class="mb-0">
              <i class="bi bi-calendar-week me-2 text-primary"></i>
              My Weekly OR Calendar
            </h5>

            <small v-if="currentCycle" class="text-muted">
              Current cycle:
              #{{ currentCycle.cycleId }}
              · {{ formatDate(currentCycle.weekStartDate) }}
              -
              {{ formatDate(currentCycle.weekEndDate) }}
            </small>

            <small v-else class="text-muted">
              No current cycle found.
            </small>
          </div>

          <button
            class="btn btn-sm btn-outline-primary"
            :disabled="!currentCycle"
            @click="loadMyCalendar"
          >
            <i class="bi bi-arrow-clockwise me-1"></i>
            Refresh Calendar
          </button>
        </div>

        <EmptyState
          v-if="calendarBlocks.length === 0"
          title="No blocks or cases"
          message="You do not have assigned blocks or scheduled cases in the current cycle."
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
                            v-if="caseItem.patientMrn"
                            class="calendar-case-detail"
                          >
                            MRN: {{ caseItem.patientMrn }}
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

      <div class="row g-4">
        <!-- Upcoming cases -->
        <div class="col-lg-7">
          <div class="page-card h-100">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <h5 class="mb-0">
                <i class="bi bi-hospital me-2 text-primary"></i>
                Upcoming Cases
              </h5>
            </div>

            <EmptyState
              v-if="!dashboard.upcomingCases || dashboard.upcomingCases.length === 0"
              title="No upcoming cases"
              message="You do not have any upcoming scheduled cases."
              icon="bi-calendar-x"
            />

            <div v-else class="table-responsive">
              <table class="table table-hover align-middle">
                <thead>
                  <tr>
                    <th>Case</th>
                    <th>Room</th>
                    <th>Surgery</th>
                    <th>Scheduled</th>
                    <th>Status</th>
                  </tr>
                </thead>

                <tbody>
                  <tr
                    v-for="item in dashboard.upcomingCases"
                    :key="item.surgeryId"
                  >
                    <td>#{{ item.surgeryId }}</td>
                    <td>{{ item.roomName }}</td>
                    <td>{{ item.surgeryType }}</td>
                    <td>
                      <div>{{ formatDateTime(item.scheduledStart) }}</div>
                      <small class="text-muted">
                        to {{ formatDateTime(item.scheduledEnd) }}
                      </small>
                    </td>
                    <td>
                      <StatusBadge :status="item.caseStatus" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Recent requests -->
        <div class="col-lg-5">
          <div class="page-card h-100">
            <div class="d-flex justify-content-between align-items-center mb-3">
              <h5 class="mb-0">
                <i class="bi bi-list-check me-2 text-primary"></i>
                Recent Requests
              </h5>

              <router-link
                to="/app/surgeon/requests"
                class="btn btn-sm btn-outline-primary"
              >
                View All
              </router-link>
            </div>

            <EmptyState
              v-if="!dashboard.recentRequests || dashboard.recentRequests.length === 0"
              title="No requests"
              message="You have not submitted any OR requests yet."
              icon="bi-inbox"
            />

            <div v-else class="request-list">
              <div
                v-for="item in dashboard.recentRequests"
                :key="item.requestId"
                class="request-item"
              >
                <div class="d-flex justify-content-between gap-2">
                  <div>
                    <strong>#{{ item.requestId }} - {{ item.surgeryType }}</strong>
                    <div class="text-muted small">
                      {{ item.priority }} · {{ item.patientReadiness }}
                    </div>
                    <div class="text-muted small">
                      Created: {{ formatDateTime(item.createdAt) }}
                    </div>
                  </div>

                  <StatusBadge :status="item.requestStatus" />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <EmptyState
      v-else
      title="Dashboard unavailable"
      message="No dashboard data was returned."
      icon="bi-exclamation-circle"
    />
  </div>
</template>

<style scoped>
.request-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.request-item {
  padding: 14px;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  background: #fff;
}

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