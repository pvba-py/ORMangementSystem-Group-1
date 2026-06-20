<script setup>
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getRooms,
  createRoom,
  updateRoom,
  deleteRoom,
  getCalendar
} from '../../services/roomService'
import { formatDate, formatDateTime, formatTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)

const rooms = ref([])
const calendar = ref([])

const calendarFilters = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-26'
})

const selectedRoom = ref(null)

const roomForm = ref({
  roomName: '',
  roomType: 'Standard',
  location: '',
  isActive: true
})

const loadRooms = async () => {
  try {
    const response = await getRooms()
    rooms.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load rooms.'

    showToast(message, 'error')
  }
}

const loadCalendar = async () => {
  try {
    const response = await getCalendar({
      fromDate: calendarFilters.value.fromDate,
      toDate: calendarFilters.value.toDate
    })

    calendar.value = response.data || []
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load calendar.'

    showToast(message, 'error')
  }
}

const loadPage = async () => {
  loading.value = true

  try {
    await Promise.all([loadRooms(), loadCalendar()])
  } finally {
    loading.value = false
  }
}

const resetRoomForm = () => {
  selectedRoom.value = null

  roomForm.value = {
    roomName: '',
    roomType: 'Standard',
    location: '',
    isActive: true
  }
}

const openEditRoom = room => {
  selectedRoom.value = room

  roomForm.value = {
    roomName: room.roomName || '',
    roomType: room.roomType || 'Standard',
    location: room.location || '',
    isActive: room.isActive
  }
}

const submitRoom = async () => {
  if (!roomForm.value.roomName.trim()) {
    showToast('Room name is required.', 'warning')
    return
  }

  if (!roomForm.value.location.trim()) {
    showToast('Room location is required.', 'warning')
    return
  }

  saving.value = true

  try {
    const payload = {
      roomName: roomForm.value.roomName.trim(),
      roomType: roomForm.value.roomType,
      location: roomForm.value.location.trim(),
      isActive: roomForm.value.isActive
    }

    if (selectedRoom.value) {
      await updateRoom(selectedRoom.value.orRoomId, payload)
      showToast('Room updated successfully.', 'success')
    } else {
      await createRoom(payload)
      showToast('Room created successfully.', 'success')
    }

    resetRoomForm()
    await loadRooms()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to save room.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const handleDeleteRoom = async room => {
  if (!confirm(`Deactivate room ${room.roomName}?`)) {
    return
  }

  try {
    await deleteRoom(room.orRoomId)
    showToast('Room deactivated successfully.', 'success')
    await loadRooms()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to deactivate room.'

    showToast(message, 'error')
  }
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Rooms & Calendar"
      subtitle="Manage operating rooms and view the OR schedule calendar"
      icon="bi-calendar3"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <!-- Room form -->
      <div class="page-card mb-4">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="mb-0">
            <i class="bi bi-door-open me-2 text-primary"></i>
            {{ selectedRoom ? 'Edit Room' : 'Create Room' }}
          </h5>

          <button
            v-if="selectedRoom"
            class="btn btn-sm btn-outline-secondary"
            @click="resetRoomForm"
          >
            Cancel Edit
          </button>
        </div>

        <div class="row g-3">
          <div class="col-md-3">
            <label class="form-label">Room Name</label>
            <input
              v-model="roomForm.roomName"
              class="form-control"
              placeholder="e.g. OR-1"
            />
          </div>

          <div class="col-md-3">
            <label class="form-label">Room Type</label>
            <select v-model="roomForm.roomType" class="form-select">
              <option value="Standard">Standard</option>
              <option value="Cardiac">Cardiac</option>
              <option value="Neuro">Neuro</option>
              <option value="Emergency">Emergency</option>
              <option value="Hybrid">Hybrid</option>
            </select>
          </div>

          <div class="col-md-4">
            <label class="form-label">Location</label>
            <input
              v-model="roomForm.location"
              class="form-control"
              placeholder="e.g. Floor 2"
            />
          </div>

          <div class="col-md-2">
            <label class="form-label">Active</label>
            <select v-model="roomForm.isActive" class="form-select">
              <option :value="true">Yes</option>
              <option :value="false">No</option>
            </select>
          </div>
        </div>

        <div class="text-end mt-3">
          <button class="btn btn-primary" :disabled="saving" @click="submitRoom">
            <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
            {{ selectedRoom ? 'Update Room' : 'Create Room' }}
          </button>
        </div>
      </div>

      <!-- Rooms table -->
      <div class="page-card mb-4">
        <h5 class="mb-3">
          <i class="bi bi-hospital me-2 text-primary"></i>
          Operating Rooms
        </h5>

        <EmptyState
          v-if="rooms.length === 0"
          title="No rooms"
          message="No operating rooms were found."
          icon="bi-door-closed"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>ID</th>
                <th>Room</th>
                <th>Type</th>
                <th>Location</th>
                <th>Active</th>
                <th class="text-end">Actions</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="room in rooms" :key="room.orRoomId">
                <td>#{{ room.orRoomId }}</td>
                <td>{{ room.roomName }}</td>
                <td>{{ room.roomType }}</td>
                <td>{{ room.location }}</td>
                <td>
                  <span
                    class="badge"
                    :class="room.isActive ? 'bg-success' : 'bg-secondary'"
                  >
                    {{ room.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="text-end">
                  <button
                    class="btn btn-sm btn-outline-primary me-2"
                    @click="openEditRoom(room)"
                  >
                    Edit
                  </button>

                  <button
                    class="btn btn-sm btn-outline-danger"
                    @click="handleDeleteRoom(room)"
                  >
                    Deactivate
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Calendar -->
      <div class="page-card">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="mb-0">
            <i class="bi bi-calendar-week me-2 text-primary"></i>
            OR Calendar
          </h5>
        </div>

        <div class="row g-3 align-items-end mb-4">
          <div class="col-md-3">
            <label class="form-label">From Date</label>
            <input
              v-model="calendarFilters.fromDate"
              type="date"
              class="form-control"
            />
          </div>

          <div class="col-md-3">
            <label class="form-label">To Date</label>
            <input
              v-model="calendarFilters.toDate"
              type="date"
              class="form-control"
            />
          </div>

          <div class="col-md-3">
            <button class="btn btn-primary w-100" @click="loadCalendar">
              <i class="bi bi-search me-1"></i>
              Load Calendar
            </button>
          </div>
        </div>

        <EmptyState
          v-if="calendar.length === 0"
          title="No calendar records"
          message="No blocks or cases were found for the selected range."
          icon="bi-calendar-x"
        />

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle">
            <thead>
              <tr>
                <th>Room</th>
                <th>Block Date</th>
                <th>Block Time</th>
                <th>Block Status</th>
                <th>Surgeon</th>
                <th>Case</th>
                <th>Case Time</th>
                <th>Case Status</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="(item, index) in calendar" :key="index">
                <td>{{ item.roomName }}</td>
                <td>{{ formatDate(item.blockDate) }}</td>
                <td>{{ formatTime(item.startTime) }} - {{ formatTime(item.endTime) }}</td>
                <td>
                  <StatusBadge :status="item.blockStatus" />
                </td>
                <td>{{ item.surgeonName || '-' }}</td>
                <td>
                  <span v-if="item.surgeryId">#{{ item.surgeryId }}</span>
                  <span v-else>-</span>
                </td>
                <td>
                  <span v-if="item.scheduledStart">
                    {{ formatDateTime(item.scheduledStart) }}
                    <br />
                    <small class="text-muted">
                      to {{ formatDateTime(item.scheduledEnd) }}
                    </small>
                  </span>
                  <span v-else>-</span>
                </td>
                <td>
                  <StatusBadge
                    v-if="item.caseStatus"
                    :status="item.caseStatus"
                  />
                  <span v-else>-</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>