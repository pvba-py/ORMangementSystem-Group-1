<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getReleasedSlots,
  getSlotMatches,
  updateReleasedSlotStatus,
  getWaitlist,
  assignWaitlist,
  removeWaitlist,
  getStarvationList
} from '../../services/waitlistService'
import { formatDate, formatTime, formatDateTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)

const activeTab = ref('slots')

const releasedSlots = ref([])
const waitlist = ref([])
const starvationList = ref([])
const matches = ref([])

const showMatchesModal = ref(false)
const showSlotStatusModal = ref(false)

const selectedSlot = ref(null)
const selectedWaitlist = ref(null)

const slotFilters = ref({
  state: 'Available',
  fromDate: '2026-06-22',
  toDate: '2026-06-26'
})

const slotStatusForm = ref({
  slotState: 'Matched'
})

const loadReleasedSlots = async () => {
  const params = {}

  if (slotFilters.value.state) params.state = slotFilters.value.state
  if (slotFilters.value.fromDate) params.fromDate = slotFilters.value.fromDate
  if (slotFilters.value.toDate) params.toDate = slotFilters.value.toDate

  const response = await getReleasedSlots(params)
  releasedSlots.value = response.data || []
}

const loadWaitlist = async () => {
  const response = await getWaitlist()
  waitlist.value = response.data || []
}

const loadStarvation = async () => {
  const response = await getStarvationList()
  starvationList.value = response.data || []
}

const loadPage = async () => {
  loading.value = true

  try {
    await Promise.all([
      loadReleasedSlots(),
      loadWaitlist(),
      loadStarvation()
    ])
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load waitlist/backfill data.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const openMatches = async slot => {
  selectedSlot.value = slot
  selectedWaitlist.value = null
  matches.value = []
  showMatchesModal.value = true

  try {
    const response = await getSlotMatches(slot.slotId)
    matches.value = response.data || []

    if (matches.value.length === 0) {
      showToast('No matching waitlist requests found for this slot.', 'info')
    }
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load slot matches.'

    showToast(message, 'error')
  }
}

const openSlotStatus = slot => {
  selectedSlot.value = slot
  selectedWaitlist.value = null
  matches.value = []
  showSlotStatusModal.value = true

  slotStatusForm.value = {
    slotState: slot.slotState || 'Matched'
  }
}

const submitSlotStatus = async () => {
  if (!selectedSlot.value) return

  saving.value = true

  try {
    await updateReleasedSlotStatus(selectedSlot.value.slotId, {
      slotState: slotStatusForm.value.slotState
    })

    showToast('Released slot status updated successfully.', 'success')
    selectedSlot.value = null
    showSlotStatusModal.value = false
  selectedSlot.value = null
    await loadReleasedSlots()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update slot status.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const assignMatch = async match => {
  if (!selectedSlot.value) return

  if (!confirm(`Assign waitlist #${match.waitlistId} to slot #${selectedSlot.value.slotId}?`)) {
    return
  }

  saving.value = true

  try {
    await assignWaitlist(match.waitlistId, {
      slotId: selectedSlot.value.slotId,
      matchScore: match.matchScore
    })

    showToast('Waitlist request assigned successfully.', 'success')
    matches.value = []
    showMatchesModal.value = false
    selectedSlot.value = null
    await loadPage()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to assign waitlist request.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const handleRemoveWaitlist = async item => {
  if (!confirm(`Remove waitlist #${item.waitlistId}?`)) return

  try {
    await removeWaitlist(item.waitlistId)
    showToast('Waitlist request removed successfully.', 'success')
    await loadPage()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to remove waitlist request.'

    showToast(message, 'error')
  }
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Waitlist & Backfill"
      subtitle="Match released OR slots with waitlisted requests"
      icon="bi-hourglass-split"
    >
      <template #actions>
        <button class="btn btn-outline-primary" @click="loadPage">
          <i class="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'slots' }"
            @click="activeTab = 'slots'"
          >
            Released Slots
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'waitlist' }"
            @click="activeTab = 'waitlist'"
          >
            Waitlist
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'starvation' }"
            @click="activeTab = 'starvation'"
          >
            Starvation
          </button>
        </li>
      </ul>

      <!-- Released Slots Tab -->
      <div v-if="activeTab === 'slots'">
        <div class="page-card mb-4">
          <div class="row g-3 align-items-end">
            <div class="col-md-3">
              <label class="form-label">State</label>
              <select v-model="slotFilters.state" class="form-select">
                <option value="">All</option>
                <option value="Available">Available</option>
                <option value="Matched">Matched</option>
                <option value="Assigned">Assigned</option>
                <option value="Expired">Expired</option>
              </select>
            </div>

            <div class="col-md-3">
              <label class="form-label">From Date</label>
              <input v-model="slotFilters.fromDate" type="date" class="form-control" />
            </div>

            <div class="col-md-3">
              <label class="form-label">To Date</label>
              <input v-model="slotFilters.toDate" type="date" class="form-control" />
            </div>

            <div class="col-md-3">
              <button class="btn btn-primary w-100" @click="loadReleasedSlots">
                Apply Filter
              </button>
            </div>
          </div>
        </div>

        <div class="page-card">
          <EmptyState
            v-if="releasedSlots.length === 0"
            title="No released slots"
            message="No released slots were found for the selected filter."
            icon="bi-box"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Slot</th>
                  <th>Room</th>
                  <th>Date</th>
                  <th>Time</th>
                  <th>Source</th>
                  <th>State</th>
                  <th>Created</th>
                  <th class="text-end">Actions</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="slot in releasedSlots" :key="slot.slotId">
                  <td>#{{ slot.slotId }}</td>
                  <td>{{ slot.roomName }}</td>
                  <td>{{ formatDate(slot.slotDate) }}</td>
                  <td>{{ formatTime(slot.startTime) }} - {{ formatTime(slot.endTime) }}</td>
                  <td>{{ slot.source }}</td>
                  <td>
                    <StatusBadge :status="slot.slotState" />
                  </td>
                  <td>{{ formatDateTime(slot.createdAt) }}</td>
                  <td class="text-end">
                    <button
                      class="btn btn-sm btn-outline-primary me-2"
                      :disabled="slot.slotState !== 'Available'"
                      @click="openMatches(slot)"
                    >
                      Matches
                    </button>

                    <button
                      class="btn btn-sm btn-outline-secondary"
                      @click="openSlotStatus(slot)"
                    >
                      Status
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Matches panel -->
        <AppModal
  :show="showMatchesModal"
  :title="selectedSlot ? `Matches for Slot #${selectedSlot.slotId}` : 'Slot Matches'"
  size="xl"
  @close="showMatchesModal = false; selectedSlot = null; matches = []"
>
  <EmptyState
    v-if="matches.length === 0"
    title="No matches"
    message="No matching waitlist requests were found for this slot."
    icon="bi-search"
  />

  <div v-else class="table-responsive">
    <table class="table table-hover align-middle">
      <thead>
        <tr>
          <th>Waitlist</th>
          <th>Request</th>
          <th>Surgery</th>
          <th>Priority</th>
          <th>Readiness</th>
          <th>Duration</th>
          <th>Waited</th>
          <th>Slot Min</th>
          <th>Score</th>
          <th class="text-end">Action</th>
        </tr>
      </thead>

      <tbody>
        <tr v-for="match in matches" :key="match.waitlistId">
          <td>#{{ match.waitlistId }}</td>
          <td>#{{ match.requestId }}</td>
          <td>{{ match.surgeryType }}</td>
          <td>{{ match.priority }}</td>
          <td>{{ match.patientReadiness }}</td>
          <td>{{ match.estimatedDurationMin }} min</td>
          <td>
            {{ match.cyclesWaited }} cycles
            <br />
            <small>{{ match.waitingDays }} days</small>
          </td>
          <td>{{ match.slotMin }} min</td>
          <td>
            <strong>{{ Number(match.matchScore).toFixed(2) }}</strong>
          </td>
          <td class="text-end">
            <button
              class="btn btn-sm btn-success"
              :disabled="saving"
              @click="assignMatch(match)"
            >
              Assign
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <template #footer>
    <button
      class="btn btn-outline-secondary"
      @click="showMatchesModal = false; selectedSlot = null; matches = []"
    >
      Close
    </button>
  </template>
</AppModal>

        <!-- Slot status panel -->
        <AppModal
  :show="showSlotStatusModal"
  :title="selectedSlot ? `Update Slot #${selectedSlot.slotId}` : 'Update Slot'"
  size="md"
  @close="showSlotStatusModal = false; selectedSlot = null"
>
  <div>
    <label class="form-label">Slot State</label>
    <select v-model="slotStatusForm.slotState" class="form-select">
      <option value="Available">Available</option>
      <option value="Matched">Matched</option>
      <option value="Assigned">Assigned</option>
      <option value="Expired">Expired</option>
    </select>
  </div>

  <template #footer>
    <button
      class="btn btn-outline-secondary"
      @click="showSlotStatusModal = false; selectedSlot = null"
    >
      Cancel
    </button>

    <button class="btn btn-primary" :disabled="saving" @click="submitSlotStatus">
      <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
      Save Status
    </button>
  </template>
</AppModal>
      </div>

      <!-- Waitlist Tab -->
      <div v-if="activeTab === 'waitlist'" class="page-card">
        <EmptyState
          v-if="waitlist.length === 0"
          title="No waitlist requests"
          message="There are currently no waitlisted requests."
          icon="bi-hourglass"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Waitlist</th>
                <th>Request</th>
                <th>Surgeon</th>
                <th>Patient</th>
                <th>Surgery</th>
                <th>Priority</th>
                <th>Readiness</th>
                <th>Duration</th>
                <th>Availability</th>
                <th>Cycles</th>
                <th>Waiting Since</th>
                <th class="text-end">Actions</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="item in waitlist" :key="item.waitlistId">
                <td>#{{ item.waitlistId }}</td>
                <td>#{{ item.requestId }}</td>
                <td>{{ item.surgeonName }}</td>
                <td>
                  <div>{{ item.patientName }}</div>
                </td>
                <td>{{ item.surgeryType }}</td>
                <td>{{ item.priority }}</td>
                <td>{{ item.patientReadiness }}</td>
                <td>{{ item.estimatedDurationMin }} min</td>
                <td>{{ item.availableDaysDisplay }}</td>
                <td>{{ item.cyclesWaited }}</td>
                <td>{{ formatDateTime(item.waitingSince) }}</td>
                <td class="text-end">
                  <button class="btn btn-sm btn-outline-danger" @click="handleRemoveWaitlist(item)">
                    Remove
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Starvation Tab -->
      <div v-if="activeTab === 'starvation'" class="page-card">
        <EmptyState
          v-if="starvationList.length === 0"
          title="No starved requests"
          message="No waitlisted requests have crossed the starvation threshold."
          icon="bi-check-circle"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Waitlist</th>
                <th>Request</th>
                <th>Surgeon</th>
                <th>Patient</th>
                <th>Surgery</th>
                <th>Priority</th>
                <th>Duration</th>
                <th>Cycles Waited</th>
                <th>Availability</th>
                <th>Waiting Since</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="item in starvationList" :key="item.waitlistId">
                <td>#{{ item.waitlistId }}</td>
                <td>#{{ item.requestId }}</td>
                <td>{{ item.surgeonName }}</td>
                <td>
                  <div>{{ item.patientName }}</div>
                  <small>{{ item.patientMrn }}</small>
                </td>
                <td>{{ item.surgeryType }}</td>
                <td>{{ item.priority }}</td>
                <td>{{ item.estimatedDurationMin }} min</td>
                <td>
                  <span class="badge bg-danger">
                    {{ item.cyclesWaited }}
                  </span>
                </td>
                <td>{{ item.availableDaysDisplay }}</td>
                <td>{{ formatDateTime(item.waitingSince) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>