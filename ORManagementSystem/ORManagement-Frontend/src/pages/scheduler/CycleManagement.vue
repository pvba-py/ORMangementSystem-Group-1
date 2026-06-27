<script setup>
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getCycles,
  cutoffCycle,
  publishCycle,
  startCycle,
  closeCycle,
  autoBuildBlocks,
  autoAssignCases
} from '../../services/cycleService'
import { computed, onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import { getCalendar } from '../../services/roomService'
import { formatDate, formatDateTime, formatTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const actionLoading = ref(false)
const activeAction = ref('')
const isActionLoading = actionName => {
  return actionLoading.value && activeAction.value === actionName
}


const cycles = ref([])
const selectedCycle = ref(null)
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

  return startMinutes < slotEnd && endMinutes > slotStart
}

const selectedCycleStatus = computed(() => {
  return String(selectedCycle.value?.cycleStatus || '').trim()
})

const hasOpenCycle = computed(() => {
  return cycles.value.some(cycle => cycle.cycleStatus === 'Open')
})

const canStartSelectedCycle = computed(() => {
  return selectedCycleStatus.value === 'Closed' && !hasOpenCycle.value
})

const canCutoffSelectedCycle = computed(() => {
  return selectedCycleStatus.value === 'Open'
})

const canPublishSelectedCycle = computed(() => {
  return selectedCycleStatus.value === 'Cutoff'
})

const canCloseSelectedCycle = computed(() => {
  return selectedCycleStatus.value === 'Published'
})

const selectedCycleActionHint = computed(() => {
  if (!selectedCycle.value) {
    return 'Select a cycle to view available actions.'
  }

  switch (selectedCycleStatus.value) {
    case 'Open':
      return 'This cycle is accepting surgeon requests. Use cutoff when request intake should close.'
    case 'Cutoff':
      return 'Request intake is closed. Continue scheduling, then publish when finalized.'
    case 'Published':
      return 'The schedule is finalized. Close the cycle when it should become historical.'
    case 'Closed':
      return hasOpenCycle.value
        ? 'This cycle is closed. Another cycle is already open, so this cycle cannot be started now.'
        : 'This cycle is closed. Start it to accept new surgeon requests.'
    default:
      return 'Unknown cycle status.'
  }
})
const handleAutoBuildBlocks = async () => {
  if (!selectedCycle.value?.cycleId) return

  if (!confirm(`Auto build blocks for cycle #${selectedCycle.value.cycleId}? This will create missing templates and generate blocks.`)) {
    return
  }

  actionLoading.value = true
  activeAction.value = 'autoBuild'

  try {
    const cycleId = selectedCycle.value.cycleId

    const response = await autoBuildBlocks(cycleId)
    const data = response.data?.data

    const templatesCreated = data?.templatesCreated ?? 0
    const blocksGenerated = data?.blocksGenerated ?? 0
    const skippedCount = data?.skippedCount ?? 0

    showToast(
      `Auto build completed. Templates created: ${templatesCreated}, Blocks generated: ${blocksGenerated}, Skipped: ${skippedCount}.`,
      'success'
    )

    await loadCycles(cycleId)
    await loadCalendar()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to auto build blocks.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
    activeAction.value = ''
  }
}

const handleAutoAssignCases = async () => {
  if (!selectedCycle.value?.cycleId) return

  if (!confirm(`Auto assign approved ready requests for cycle #${selectedCycle.value.cycleId}?`)) {
    return
  }

  actionLoading.value = true
  activeAction.value = 'autoAssign'

  try {
    const cycleId = selectedCycle.value.cycleId

    const response = await autoAssignCases(cycleId)
    const data = response.data?.data

    const casesScheduled = data?.casesScheduled ?? 0
    const requestsSkipped = data?.requestsSkipped ?? 0
    const skippedRequests = data?.skippedRequests || []

    showToast(
      `Auto assignment completed. Cases scheduled: ${casesScheduled}, Requests skipped: ${requestsSkipped}.`,
      casesScheduled > 0 ? 'success' : 'warning'
    )

    if (skippedRequests.length > 0) {
      const firstSkipped = skippedRequests[0]

      showToast(
        `Skipped request #${firstSkipped.requestId}: ${firstSkipped.reason}`,
        'warning'
      )
    }

    await loadCycles(cycleId)
    await loadCalendar()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      err?.response?.data?.error ||
      'Failed to auto assign cases.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
    activeAction.value = ''
  }
}
const cycleWeekDays = computed(() => {
  if (!selectedCycle.value?.weekStartDate) {
    return []
  }

  const startDate = parseDateOnly(selectedCycle.value.weekStartDate)

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

const isSelectedCycle = cycle => {
  return Number(selectedCycle.value?.cycleId) === Number(cycle.cycleId)
}

const selectCycle = async cycle => {
  selectedCycle.value = cycle
  await loadCalendar()
}

const loadCalendar = async () => {
  if (!selectedCycle.value?.weekStartDate || !selectedCycle.value?.weekEndDate) {
    calendar.value = []
    return
  }

  try {
    const response = await getCalendar({
      fromDate: selectedCycle.value.weekStartDate.substring(0, 10),
      toDate: selectedCycle.value.weekEndDate.substring(0, 10)
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

const chooseDefaultCycle = preferredCycleId => {
  if (!cycles.value.length) {
    selectedCycle.value = null
    return
  }

  if (preferredCycleId) {
    const sameCycle = cycles.value.find(
      cycle => Number(cycle.cycleId) === Number(preferredCycleId)
    )

    if (sameCycle) {
      selectedCycle.value = sameCycle
      return
    }
  }

  selectedCycle.value =
    cycles.value.find(cycle => cycle.cycleStatus === 'Open') ||
    cycles.value.find(cycle => cycle.cycleStatus === 'Cutoff') ||
    cycles.value.find(cycle => cycle.cycleStatus === 'Published') ||
    cycles.value.find(cycle => cycle.cycleStatus === 'Closed') ||
    cycles.value[0]
}

const loadCycles = async (preferredCycleId = null) => {
  loading.value = true

  try {
    const response = await getCycles()
    cycles.value = response.data || []

    chooseDefaultCycle(preferredCycleId)

    if (selectedCycle.value?.cycleId) {
      await loadCalendar()
    } else {
      calendar.value = []
    }
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load cycles.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const handleStart = async () => {
  if (!selectedCycle.value?.cycleId) return

  if (hasOpenCycle.value) {
    showToast('Another cycle is already open. Only one cycle can be open at a time.', 'warning')
    return
  }

  if (!confirm(`Start cycle #${selectedCycle.value.cycleId}? This cycle will begin accepting surgeon requests.`)) {
    return
  }

  actionLoading.value = true
  activeAction.value = 'start'

  try {
    const cycleId = selectedCycle.value.cycleId

    await startCycle(cycleId)
    showToast('Cycle started successfully.', 'success')

    await loadCycles(cycleId)
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to start cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
    activeAction.value = ''
  }
}

const handleCutoff = async () => {
  if (!selectedCycle.value?.cycleId) return

  if (!confirm(`Cutoff cycle #${selectedCycle.value.cycleId}? New normal requests will be blocked.`)) {
    return
  }

  actionLoading.value = true
  activeAction.value = 'cutoff'
  try {
    const cycleId = selectedCycle.value.cycleId

    await cutoffCycle(cycleId)
    showToast('Cycle cutoff completed successfully.', 'success')

    await loadCycles(cycleId)
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to cutoff cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
    activeAction.value = ''
  }
}

const handlePublish = async () => {
  if (!selectedCycle.value?.cycleId) return

  if (!confirm(`Publish cycle #${selectedCycle.value.cycleId}? The schedule will be finalized.`)) {
    return
  }

  actionLoading.value = true

  try {
    const cycleId = selectedCycle.value.cycleId

    await publishCycle(cycleId)
    showToast('Cycle published successfully.', 'success')

    await loadCycles(cycleId)
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to publish cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
    activeAction.value = ''
  }
}

const handleClose = async () => {
  if (!selectedCycle.value?.cycleId) return

  if (!confirm(`Close cycle #${selectedCycle.value.cycleId}? This will make the cycle historical/read-only.`)) {
    return
  }

  actionLoading.value = true
  activeAction.value = 'close'

  try {
    const cycleId = selectedCycle.value.cycleId

    await closeCycle(cycleId)
    showToast('Cycle closed successfully.', 'success')

    await loadCycles(cycleId)
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to close cycle.'

    showToast(message, 'error')
  } finally {
    actionLoading.value = false
    activeAction.value = ''
  }
}

onMounted(() => loadCycles())
</script>

<template>
  <div>
    <PageHeader
      title="Cycle Management"
      subtitle="Manage scheduling cycle lifecycle and inspect each cycle's OR calendar"
      icon="bi-arrow-repeat"
    >
      <template #actions>
        <button
          class="btn btn-outline-primary me-2"
          :disabled="loading || actionLoading"
          @click="loadCycles(selectedCycle?.cycleId)"
        >
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <!-- Cycle Lifecycle Guide -->
      <div class="cycle-guide page-card mb-4">
        <div class="d-flex flex-wrap gap-3 align-items-center">
          <div class="cycle-guide-item">
            <span class="cycle-dot cycle-dot-open"></span>
            <strong>Open</strong>
            <small>Accepting surgeon requests</small>
          </div>

          <div class="cycle-guide-arrow">→</div>

          <div class="cycle-guide-item">
            <span class="cycle-dot cycle-dot-cutoff"></span>
            <strong>Cutoff</strong>
            <small>Request intake closed</small>
          </div>

          <div class="cycle-guide-arrow">→</div>

          <div class="cycle-guide-item">
            <span class="cycle-dot cycle-dot-published"></span>
            <strong>Published</strong>
            <small>Schedule finalized</small>
          </div>

          <div class="cycle-guide-arrow">→</div>

          <div class="cycle-guide-item">
            <span class="cycle-dot cycle-dot-closed"></span>
            <strong>Closed</strong>
            <small>Historical/read-only</small>
          </div>
        </div>
      </div>

      <!-- Cycle List -->
      <div class="page-card mb-4">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h5 class="mb-0">
              <i class="bi bi-calendar-range me-2 text-primary"></i>
              Scheduling Cycles
            </h5>
            <small class="text-muted">
              Select a cycle to view its status, actions, blocks, and scheduled cases.
            </small>
          </div>

          <span class="text-muted small">
            Total: {{ cycles.length }}
          </span>
        </div>

        <EmptyState
          v-if="cycles.length === 0"
          title="No cycles"
          message="No scheduling cycles were found."
          icon="bi-calendar-x"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Cycle</th>
                <th>Week</th>
                <th>Cutoff At</th>
                <th>Status</th>
                <th class="text-end">Action</th>
              </tr>
            </thead>

            <tbody>
              <tr
                v-for="cycle in cycles"
                :key="cycle.cycleId"
                :class="{ 'selected-cycle-row': isSelectedCycle(cycle) }"
                class="cycle-row"
                @click="selectCycle(cycle)"
              >
                <td>
                  <strong>#{{ cycle.cycleId }}</strong>
                </td>

                <td>
                  {{ formatDate(cycle.weekStartDate) }}
                  -
                  {{ formatDate(cycle.weekEndDate) }}
                </td>

                <td>
                  {{ formatDateTime(cycle.cutoffAt) }}
                </td>

                <td>
                  <StatusBadge :status="cycle.cycleStatus" />
                </td>

                <td class="text-end">
                  <button
                    class="btn btn-sm"
                    :class="isSelectedCycle(cycle) ? 'btn-primary' : 'btn-outline-primary'"
                    @click.stop="selectCycle(cycle)"
                  >
                    {{ isSelectedCycle(cycle) ? 'Selected' : 'View Calendar' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Selected Cycle Summary -->
      <div v-if="selectedCycle" class="page-card mb-4">
        <div class="row g-3 align-items-center">
          <div class="col-md-3">
            <small class="text-muted">Selected Cycle</small>
            <h5 class="mb-0">#{{ selectedCycle.cycleId }}</h5>
          </div>

          <div class="col-md-3">
            <small class="text-muted">Week</small>
            <h6 class="mb-0">
              {{ formatDate(selectedCycle.weekStartDate) }}
              -
              {{ formatDate(selectedCycle.weekEndDate) }}
            </h6>
          </div>

          <div class="col-md-3">
            <small class="text-muted">Cutoff At</small>
            <h6 class="mb-0">
              {{ formatDateTime(selectedCycle.cutoffAt) }}
            </h6>
          </div>

          <div class="col-md-3">
            <small class="text-muted d-block">Status</small>
            <StatusBadge :status="selectedCycle.cycleStatus" />
          </div>
        </div>

        <hr />

        <div class="d-flex flex-wrap justify-content-between align-items-center gap-3">
          <small class="text-muted">
            {{ selectedCycleActionHint }}
          </small>

          <div class="d-flex flex-wrap justify-content-end gap-2">
            <button
              v-if="selectedCycleStatus === 'Closed'"
              class="btn btn-primary"
              :disabled="actionLoading || !canStartSelectedCycle"
              @click="handleStart"
            >
              <span
  v-if="isActionLoading('start')"
  class="spinner-border spinner-border-sm me-2"
></span>
              Start Cycle
            </button>

            <button
              v-if="selectedCycleStatus === 'Open'"
              class="btn btn-warning"
              :disabled="actionLoading || !canCutoffSelectedCycle"
              @click="handleCutoff"
            >
              <span
                v-if="isActionLoading('cutoff')"
                class="spinner-border spinner-border-sm me-2"
              ></span>
              Cutoff Cycle
            </button>

            <button
  v-if="selectedCycleStatus === 'Cutoff'"
  class="btn btn-outline-primary"
  :disabled="actionLoading"
  @click="handleAutoBuildBlocks"
>
  <span
    v-if="isActionLoading('autoBuild')"
    class="spinner-border spinner-border-sm me-2"
  ></span>
  Auto Build Blocks
</button>

<button
  v-if="selectedCycleStatus === 'Cutoff'"
  class="btn btn-outline-success"
  :disabled="actionLoading"
  @click="handleAutoAssignCases"
>
  <span
    v-if="isActionLoading('autoAssign')"
    class="spinner-border spinner-border-sm me-2"
  ></span>
  Auto Assign Cases
</button>

<button
  v-if="selectedCycleStatus === 'Cutoff'"
  class="btn btn-success"
  :disabled="actionLoading || !canPublishSelectedCycle"
  @click="handlePublish"
>
  <span
    v-if="isActionLoading('publish')"
    class="spinner-border spinner-border-sm me-2"
  ></span>
  Publish Schedule
</button>

            <button
              v-if="selectedCycleStatus === 'Published'"
              class="btn btn-outline-dark"
              :disabled="actionLoading || !canCloseSelectedCycle"
              @click="handleClose"
            >
              <span
                v-if="isActionLoading('close')"
                class="spinner-border spinner-border-sm me-2"
              ></span>
              Close Cycle
            </button>
          </div>
        </div>

        <div
          v-if="selectedCycle.cycleStatus === 'Closed' && hasOpenCycle"
          class="alert alert-warning mt-3 mb-0"
        >
          Another cycle is currently open. Only one cycle can be open at a time.
        </div>
      </div>

      <EmptyState
        v-else
        title="No selected cycle"
        message="Select a scheduling cycle to view its calendar."
        icon="bi-calendar-x"
      />

      <!-- Weekly OR Calendar -->
      <div v-if="selectedCycle" class="page-card">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h5 class="mb-0">
              <i class="bi bi-calendar-week me-2 text-primary"></i>
              Weekly OR Calendar
            </h5>
            <small class="text-muted">
              Blocks and scheduled cases for selected cycle #{{ selectedCycle.cycleId }}.
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
.selected-cycle-row {
  background: #eef5ff;
}

.cycle-row {
  cursor: pointer;
}

.cycle-guide {
  background: #ffffff;
}

.cycle-guide-item {
  display: flex;
  align-items: center;
  gap: 7px;
  color: #334155;
}

.cycle-guide-item small {
  color: #64748b;
}

.cycle-guide-arrow {
  color: #94a3b8;
  font-weight: 700;
}

.cycle-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  display: inline-block;
}

.cycle-dot-open {
  background: #198754;
}

.cycle-dot-cutoff {
  background: #ffc107;
}

.cycle-dot-published {
  background: #0d6efd;
}

.cycle-dot-closed {
  background: #475569;
}

/* Compact weekly calendar */
.weekly-calendar-wrapper {
  overflow-x: auto;
  border: 2px solid #cbd5e1;
  border-radius: 14px;
  background: #ffffff;
}

.weekly-calendar {
  display: grid;
  grid-template-columns: 75px repeat(5, minmax(220px, 1fr));
  min-width: 1175px;
  background: #ffffff;
}

.calendar-header-cell {
  position: sticky;
  top: 0;
  z-index: 2;
  background: #f1f5f9;
  border-bottom: 2px solid #cbd5e1;
  border-right: 1px solid #cbd5e1;
  padding: 9px;
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
  padding: 8px;
  font-weight: 700;
  color: #475569;
  min-height: 95px;
  font-size: 0.85rem;
}

.calendar-day-cell {
  border-right: 1px solid #dbe3ef;
  border-bottom: 1px solid #dbe3ef;
  padding: 6px;
  min-height: 95px;
  background: #ffffff;
}

.calendar-day-cell:hover {
  background: #f8fafc;
}

.calendar-block-card {
  border-radius: 10px;
  padding: 8px;
  border: 2px solid #334155;
  background: #ffffff;
  margin-bottom: 6px;
  box-shadow: 0 2px 5px rgba(15, 23, 42, 0.12);
}

.calendar-block-type {
  display: inline-block;
  font-size: 0.62rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 3px;
  color: #1e293b;
}

.calendar-block-title {
  font-size: 0.9rem;
  font-weight: 800;
  color: #0f172a;
}

.calendar-block-owner {
  font-size: 0.74rem;
  font-weight: 600;
  color: #475569;
}

.calendar-block-time {
  font-size: 0.74rem;
  font-weight: 700;
  color: #334155;
}

.calendar-block-recurring {
  border-left: 6px solid #0d6efd;
  background: #eef5ff;
}

.calendar-block-open {
  border-left: 6px solid #198754;
  background: #eefaf3;
}

.calendar-block-emergency {
  border-left: 6px solid #dc3545;
  background: #fff1f2;
}

.calendar-block-adhoc {
  border-left: 6px solid #ffc107;
  background: #fff8e1;
}

.calendar-case-list {
  border-top: 2px dashed #94a3b8;
  padding-top: 6px;
}

.calendar-case-item {
  background: #ffffff;
  border: 2px solid #64748b;
  border-radius: 9px;
  padding: 6px;
  margin-top: 5px;
}

.calendar-case-title {
  font-size: 0.82rem;
  font-weight: 800;
  color: #0f172a;
}

.calendar-case-detail {
  font-size: 0.72rem;
  color: #334155;
  margin-top: 2px;
}

.calendar-case-time {
  font-size: 0.7rem;
  color: #475569;
  margin-top: 4px;
  font-weight: 600;
}
</style>


