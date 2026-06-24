<script setup>
import { computed, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import KpiCard from '../../components/common/KpiCard.vue'
import { generateORRoomWeeklyReport } from '../../services/utilizationService'
import { formatDate, formatDateTime, formatPercent } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const report = ref(null)

const roomSearch = ref('')
const statusFilter = ref('')

const toDateInputValue = (date) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const getCurrentMondayInput = () => {
  const today = new Date()
  const day = today.getDay()
  const diffToMonday = day === 0 ? -6 : 1 - day

  const monday = new Date(today)
  monday.setDate(today.getDate() + diffToMonday)

  return toDateInputValue(monday)
}

const reportForm = ref({
  weekStartDate: getCurrentMondayInput()
})

const parseDateInput = (value) => {
  if (!value) return null

  const parts = value.split('-').map(Number)

  if (parts.length !== 3) return null

  const [year, month, day] = parts

  if (!year || !month || !day) return null

  return new Date(year, month - 1, day)
}

const isMonday = (value) => {
  const date = parseDateInput(value)

  if (!date) return false

  return date.getDay() === 1
}

const weekStartValidationMessage = computed(() => {
  if (!reportForm.value.weekStartDate) {
    return 'Week start date is required.'
  }

  if (!isMonday(reportForm.value.weekStartDate)) {
    return 'Please select a Monday. Reports are generated from Monday to Friday.'
  }

  return ''
})

const canProduceReport = computed(() => {
  return !loading.value && !weekStartValidationMessage.value
})

const summary = computed(() => {
  return report.value?.summary || null
})

const rooms = computed(() => {
  return report.value?.rooms || []
})

const underutilizedRooms = computed(() => {
  return report.value?.underutilizedRooms || []
})

const filteredRooms = computed(() => {
  const search = roomSearch.value.trim().toLowerCase()
  const status = statusFilter.value

  return rooms.value.filter((room) => {
    const matchesSearch =
      !search ||
      room.roomName?.toLowerCase().includes(search) ||
      String(room.orRoomId).includes(search)

    const matchesStatus =
      !status || room.utilizationStatus === status

    return matchesSearch && matchesStatus
  })
})

const averageUtilization = computed(() => {
  return summary.value?.averageUtilizationPercent || 0
})

const utilizationProgressWidth = computed(() => {
  const value = Number(averageUtilization.value) || 0
  return `${Math.min(value, 100)}%`
})

const utilizationProgressClass = computed(() => {
  const value = Number(averageUtilization.value) || 0

  if (value >= 80) return 'bg-success'
  if (value >= 60) return 'bg-warning'
  if (value > 0) return 'bg-danger'

  return 'bg-secondary'
})

const formatMinutes = (minutes) => {
  const value = Number(minutes) || 0

  if (value < 60) {
    return `${value} min`
  }

  const hours = Math.floor(value / 60)
  const remainingMinutes = value % 60

  if (remainingMinutes === 0) {
    return `${value} min (${hours}h)`
  }

  return `${value} min (${hours}h ${remainingMinutes}m)`
}

const produceReport = async () => {
  if (weekStartValidationMessage.value) {
    showToast(weekStartValidationMessage.value, 'error')
    return
  }

  loading.value = true

  try {
    const response = await generateORRoomWeeklyReport({
      weekStartDate: reportForm.value.weekStartDate
    })

    report.value = response.data

    showToast('Weekly OR room utilization report generated successfully.', 'success')
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to generate OR room weekly utilization report.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const clearTableFilters = () => {
  roomSearch.value = ''
  statusFilter.value = ''
}
</script>

<template>
  <div>
    <PageHeader
      title="OR Room Weekly Utilization"
      subtitle="Generate and review Monday–Friday utilization reports for all operating rooms"
      icon="bi-bar-chart"
    >
      <template #actions>
        <button
          class="btn btn-outline-primary"
          :disabled="loading || !report"
          @click="produceReport"
        >
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh Report
        </button>
      </template>
    </PageHeader>

    <div class="page-card mb-4">
      <div class="row g-3 align-items-end">
        <div class="col-md-4">
          <label class="form-label">Week Start Date</label>
          <input
            v-model="reportForm.weekStartDate"
            type="date"
            class="form-control"
            :class="{ 'is-invalid': weekStartValidationMessage }"
          />
          <div v-if="weekStartValidationMessage" class="invalid-feedback">
            {{ weekStartValidationMessage }}
          </div>
          <div v-else class="form-text">
            Select Monday. Report covers Monday to Friday.
          </div>
        </div>

        <div class="col-md-3">
          <button
            class="btn btn-primary w-100"
            :disabled="!canProduceReport"
            @click="produceReport"
          >
            <span
              v-if="loading"
              class="spinner-border spinner-border-sm me-2"
            ></span>
            <i v-else class="bi bi-file-earmark-bar-graph me-1"></i>
            Produce Report
          </button>
        </div>

        <div class="col-md-5">
          <div class="alert alert-info mb-0 py-2">
            <i class="bi bi-info-circle me-1"></i>
            This calculates all active OR rooms for the selected week and displays the full report.
          </div>
        </div>
      </div>
    </div>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <EmptyState
        v-if="!report"
        title="No weekly report generated"
        message="Select a Monday and click Produce Report to view OR room utilization."
        icon="bi-file-earmark-bar-graph"
      />

      <div v-else>
        <div class="d-flex flex-wrap justify-content-between align-items-center mb-3">
          <div>
            <h5 class="mb-1">
              Weekly Report
            </h5>
            <div class="text-muted">
              {{ formatDate(report.weekStartDate) }}
              to
              {{ formatDate(report.weekEndDate) }}
              |
              Generated:
              {{ formatDateTime(report.generatedAt) }}
            </div>
          </div>

          <div class="text-muted mt-2 mt-md-0">
            Calculated Rooms:
            <strong>{{ report.calculatedRooms }}</strong>
          </div>
        </div>

        <div class="row g-3 mb-4" v-if="summary">
          <div class="col-md-3">
            <KpiCard
              label="Total Rooms"
              :value="summary.totalRooms"
              icon="bi-door-open"
              color="primary"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Allocated Time"
              :value="formatMinutes(summary.totalAllocatedMinutes)"
              icon="bi-clock"
              color="info"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Used Time"
              :value="formatMinutes(summary.totalUsedMinutes)"
              icon="bi-stopwatch"
              color="success"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Average Utilization"
              :value="formatPercent(summary.averageUtilizationPercent)"
              icon="bi-percent"
              color="warning"
            />
          </div>
        </div>

        <div class="row g-3 mb-4" v-if="summary">
          <div class="col-md-3">
            <KpiCard
              label="Good Rooms"
              :value="summary.goodRooms"
              icon="bi-check-circle"
              color="success"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Moderate Rooms"
              :value="summary.moderateRooms"
              icon="bi-dash-circle"
              color="warning"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Underutilized Rooms"
              :value="summary.underutilizedRooms"
              icon="bi-exclamation-triangle"
              color="danger"
            />
          </div>

          <div class="col-md-3">
            <KpiCard
              label="Unused Rooms"
              :value="summary.unusedRooms"
              icon="bi-x-circle"
              color="secondary"
            />
          </div>
        </div>

        <div class="page-card mb-4">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <h5 class="mb-0">
              <i class="bi bi-activity me-2 text-primary"></i>
              Overall Weekly Utilization
            </h5>
            <strong>{{ formatPercent(averageUtilization) }}</strong>
          </div>

          <div class="progress utilization-progress">
            <div
              class="progress-bar"
              :class="utilizationProgressClass"
              role="progressbar"
              :style="{ width: utilizationProgressWidth }"
              :aria-valuenow="averageUtilization"
              aria-valuemin="0"
              aria-valuemax="100"
            >
              {{ formatPercent(averageUtilization) }}
            </div>
          </div>
        </div>

        <div class="page-card mb-4">
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
            <h5 class="mb-0">
              <i class="bi bi-table me-2 text-primary"></i>
              Room-wise Utilization
            </h5>

            <div class="d-flex flex-wrap gap-2">
              <input
                v-model="roomSearch"
                type="text"
                class="form-control form-control-sm table-filter-input"
                placeholder="Search room"
              />

              <select
                v-model="statusFilter"
                class="form-select form-select-sm table-filter-select"
              >
                <option value="">All Statuses</option>
                <option value="Good">Good</option>
                <option value="Moderate">Moderate</option>
                <option value="Underutilized">Underutilized</option>
                <option value="Unused">Unused</option>
              </select>

              <button
                class="btn btn-sm btn-outline-secondary"
                @click="clearTableFilters"
              >
                Clear
              </button>
            </div>
          </div>

          <EmptyState
            v-if="filteredRooms.length === 0"
            title="No room utilization records"
            message="No OR room utilization records match the selected filters."
            icon="bi-search"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Room</th>
                  <th>Week</th>
                  <th>Allocated</th>
                  <th>Used</th>
                  <th>Utilization</th>
                  <th>Status</th>
                  <th>Calculated</th>
                </tr>
              </thead>

              <tbody>
                <tr
                  v-for="room in filteredRooms"
                  :key="room.orRoomUtilizationId || room.orRoomId"
                >
                  <td>
                    <div class="fw-semibold">{{ room.roomName }}</div>
                    <div class="text-muted small">Room ID: {{ room.orRoomId }}</div>
                  </td>

                  <td>
                    {{ formatDate(room.weekStartDate) }}
                    -
                    {{ formatDate(room.weekEndDate) }}
                  </td>

                  <td>{{ formatMinutes(room.allocatedMinutes) }}</td>

                  <td>{{ formatMinutes(room.usedMinutes) }}</td>

                  <td>
                    <div class="fw-semibold">
                      {{ formatPercent(room.utilizationPercent) }}
                    </div>
                    <div class="progress room-progress">
                      <div
                        class="progress-bar"
                        :class="{
                          'bg-success': room.utilizationPercent >= 80,
                          'bg-warning': room.utilizationPercent >= 60 && room.utilizationPercent < 80,
                          'bg-danger': room.utilizationPercent > 0 && room.utilizationPercent < 60,
                          'bg-secondary': room.utilizationPercent === 0
                        }"
                        :style="{ width: `${Math.min(room.utilizationPercent || 0, 100)}%` }"
                      ></div>
                    </div>
                  </td>

                  <td>
                    <StatusBadge :status="room.utilizationStatus" />
                  </td>

                  <td>{{ formatDateTime(room.calculatedAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="page-card">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="mb-0">
              <i class="bi bi-exclamation-triangle me-2 text-warning"></i>
              Underutilized / Unused Rooms
            </h5>

            <span class="badge bg-light text-dark">
              {{ underutilizedRooms.length }} room(s)
            </span>
          </div>

          <EmptyState
            v-if="underutilizedRooms.length === 0"
            title="No underutilized rooms"
            message="All OR rooms are performing at Moderate or Good utilization for this week."
            icon="bi-check-circle"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Room</th>
                  <th>Week</th>
                  <th>Allocated</th>
                  <th>Used</th>
                  <th>Utilization</th>
                  <th>Status</th>
                </tr>
              </thead>

              <tbody>
                <tr
                  v-for="item in underutilizedRooms"
                  :key="item.orRoomUtilizationId || item.orRoomId"
                >
                  <td>
                    <div class="fw-semibold">{{ item.roomName }}</div>
                    <div class="text-muted small">Room ID: {{ item.orRoomId }}</div>
                  </td>

                  <td>
                    {{ formatDate(item.weekStartDate) }}
                    -
                    {{ formatDate(item.weekEndDate) }}
                  </td>

                  <td>{{ formatMinutes(item.allocatedMinutes) }}</td>

                  <td>{{ formatMinutes(item.usedMinutes) }}</td>

                  <td>{{ formatPercent(item.utilizationPercent) }}</td>

                  <td>
                    <StatusBadge :status="item.utilizationStatus" />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.utilization-progress {
  height: 28px;
  border-radius: 999px;
}

.utilization-progress .progress-bar {
  font-weight: 600;
}

.room-progress {
  height: 6px;
  margin-top: 4px;
  border-radius: 999px;
}

.table-filter-input {
  width: 180px;
}

.table-filter-select {
  width: 170px;
}

@media (max-width: 768px) {
  .table-filter-input,
  .table-filter-select {
    width: 100%;
  }
}
</style>