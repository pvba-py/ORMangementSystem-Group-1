<script setup>
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getRoomsPaged,
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

const showRoomModal = ref(false)
const selectedRoom = ref(null)

const roomFilters = ref({
  isActive: '',
  pageNumber: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false
})

const calendarFilters = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-26'
})

const roomForm = ref({
  roomName: '',
  roomType: 'Standard',
  location: '',
  isActive: true
})

const loadRooms = async () => {
  try {
    const params = {
      pageNumber: roomFilters.value.pageNumber,
      pageSize: roomFilters.value.pageSize
    }

    if (roomFilters.value.isActive !== '') {
      params.isActive = roomFilters.value.isActive
    }

    const response = await getRoomsPaged(params)

    rooms.value = response.data.items || []

    roomFilters.value.pageNumber = response.data.pageNumber
    roomFilters.value.pageSize = response.data.pageSize
    roomFilters.value.totalCount = response.data.totalCount
    roomFilters.value.totalPages = response.data.totalPages
    roomFilters.value.hasPreviousPage = response.data.hasPreviousPage
    roomFilters.value.hasNextPage = response.data.hasNextPage
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

  showRoomModal.value = false
}

const openCreateRoom = () => {
  resetRoomForm()
  showRoomModal.value = true
}

const openEditRoom = room => {
  selectedRoom.value = room

  roomForm.value = {
    roomName: room.roomName || '',
    roomType: room.roomType || 'Standard',
    location: room.location || '',
    isActive: room.isActive
  }

  showRoomModal.value = true
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

const handleDeactivateRoom = async room => {
  if (!room.isActive) {
    showToast('Room is already inactive.', 'info')
    return
  }

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

const applyRoomFilter = async () => {
  roomFilters.value.pageNumber = 1
  await loadRooms()
}

const clearRoomFilter = async () => {
  roomFilters.value.isActive = ''
  roomFilters.value.pageNumber = 1
  await loadRooms()
}

const changeRoomPageSize = async () => {
  roomFilters.value.pageNumber = 1
  await loadRooms()
}

const goPreviousRooms = async () => {
  if (!roomFilters.value.hasPreviousPage) return

  roomFilters.value.pageNumber -= 1
  await loadRooms()
}

const goNextRooms = async () => {
  if (!roomFilters.value.hasNextPage) return

  roomFilters.value.pageNumber += 1
  await loadRooms()
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Rooms & Calendar"
      subtitle="Manage operating rooms and view the OR schedule calendar"
      icon="bi-calendar3"
    >
      <template #actions>
        <button class="btn btn-primary" @click="openCreateRoom">
          <i class="bi bi-plus-circle me-1"></i>
          Create Room
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <!-- Rooms table -->
      <div class="page-card mb-4">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h5 class="mb-0">
            <i class="bi bi-hospital me-2 text-primary"></i>
            Operating Rooms
          </h5>

          <div class="text-muted small">
            Total: {{ roomFilters.totalCount }}
            · Page {{ roomFilters.pageNumber }} of {{ roomFilters.totalPages || 1 }}
          </div>
        </div>

        <div class="row g-3 align-items-end mb-4">
          <div class="col-md-3">
            <label class="form-label">Status</label>
            <select v-model="roomFilters.isActive" class="form-select">
              <option value="">All</option>
              <option :value="true">Active</option>
              <option :value="false">Inactive</option>
            </select>
          </div>

          <div class="col-md-2">
            <label class="form-label">Page Size</label>
            <select
              v-model.number="roomFilters.pageSize"
              class="form-select"
              @change="changeRoomPageSize"
            >
              <option :value="5">5</option>
              <option :value="10">10</option>
              <option :value="20">20</option>
              <option :value="50">50</option>
            </select>
          </div>

          <div class="col-md-2">
            <button class="btn btn-primary w-100" @click="applyRoomFilter">
              Apply
            </button>
          </div>

          <div class="col-md-2">
            <button class="btn btn-outline-secondary w-100" @click="clearRoomFilter">
              Clear
            </button>
          </div>
        </div>

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
                    :disabled="!room.isActive"
                    @click="handleDeactivateRoom(room)"
                  >
                    Deactivate
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div
          v-if="rooms.length > 0"
          class="d-flex justify-content-end mt-3"
        >
          <div class="btn-group">
            <button
              class="btn btn-outline-primary btn-sm"
              :disabled="!roomFilters.hasPreviousPage"
              @click="goPreviousRooms"
            >
              Previous
            </button>

            <button
              class="btn btn-outline-primary btn-sm"
              :disabled="!roomFilters.hasNextPage"
              @click="goNextRooms"
            >
              Next
            </button>
          </div>
        </div>
      </div>

      
    </div>

    <!-- Create/Edit Room Modal -->
    <AppModal
      :show="showRoomModal"
      :title="selectedRoom ? `Edit Room #${selectedRoom.orRoomId}` : 'Create Room'"
      size="lg"
      @close="resetRoomForm"
    >
      <div class="row g-3">
        <div class="col-md-4">
          <label class="form-label">Room Name</label>
          <input
            v-model="roomForm.roomName"
            class="form-control"
            placeholder="e.g. OR-1"
          />
        </div>

        <div class="col-md-4">
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

        <div class="col-md-4">
          <label class="form-label">Active</label>
          <select v-model="roomForm.isActive" class="form-select">
            <option :value="true">Yes</option>
            <option :value="false">No</option>
          </select>
        </div>
      </div>

      <template #footer>
        <button class="btn btn-outline-secondary" @click="resetRoomForm">
          Cancel
        </button>

        <button
          class="btn btn-primary"
          :disabled="saving"
          @click="submitRoom"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          {{ selectedRoom ? 'Update Room' : 'Create Room' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>