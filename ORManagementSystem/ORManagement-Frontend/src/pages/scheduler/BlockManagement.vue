<script setup>

import PageHeader from '../../components/common/PageHeader.vue'
import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import StatusBadge from '../../components/common/StatusBadge.vue'
import {
  getBlockTemplates,
  createBlockTemplate,
  updateBlockTemplate,
  deleteBlockTemplate,
  addBlockException,
  generateBlocks,
  getBlocks,
  updateBlock,
  cancelBlock,
  releaseBlock
} from '../../services/blockService'
import { getSurgeons } from '../../services/masterDataService'
import { getRooms } from '../../services/roomService'
import { onMounted, ref } from 'vue'
import AppModal from '../../components/common/AppModal.vue'
import { formatDate, formatTime } from '../../utils/formatters'
import { showToast } from '../../utils/toast'

const loading = ref(false)
const saving = ref(false)

const activeTab = ref('blocks')

const templates = ref([])
const blocks = ref([])
const surgeons = ref([])
const rooms = ref([])

const showTemplateModal = ref(false)
const showEditBlockModal = ref(false)
const showReleaseBlockModal = ref(false)
const showExceptionModal = ref(false)

const selectedTemplate = ref(null)
const selectedBlock = ref(null)
const selectedReleaseBlock = ref(null)
const selectedExceptionTemplate = ref(null)

const blockFilters = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-26',
  surgeonId: '',
  roomId: ''
})

const templateForm = ref({
  surgeonId: '',
  orRoomId: '',
  specialty: '',
  dayOfWeek: 1,
  startTime: '08:00',
  endTime: '16:00',
  effectiveFrom: '2026-06-22',
  effectiveTo: '',
  isActive: true
})

const generateForm = ref({
  fromDate: '2026-06-22',
  toDate: '2026-06-26'
})

const blockForm = ref({
  surgeonId: '',
  orRoomId: '',
  blockDate: '',
  startTime: '',
  endTime: '',
  blockStatus: 'Allocated',
  remarks: ''
})

const releaseForm = ref({
  startTime: '',
  endTime: '',
  remarks: ''
})

const exceptionForm = ref({
  skipDate: '',
  reason: ''
})

const dayNames = {
  1: 'Monday',
  2: 'Tuesday',
  3: 'Wednesday',
  4: 'Thursday',
  5: 'Friday',
  6: 'Saturday',
  7: 'Sunday'
}

const normalizeTimeForApi = value => {
  if (!value) return ''
  return value.length === 5 ? `${value}:00` : value
}

const loadSupportingData = async () => {
  const [surgeonsResponse, roomsResponse] = await Promise.all([
    getSurgeons(),
    getRooms()
  ])

  surgeons.value = surgeonsResponse.data || []
  rooms.value = roomsResponse.data || []
}

const loadTemplates = async () => {
  const response = await getBlockTemplates()
  templates.value = response.data || []
}

const loadBlocks = async () => {
  const params = {}

  if (blockFilters.value.fromDate) params.fromDate = blockFilters.value.fromDate
  if (blockFilters.value.toDate) params.toDate = blockFilters.value.toDate
  if (blockFilters.value.surgeonId) params.surgeonId = blockFilters.value.surgeonId
  if (blockFilters.value.roomId) params.roomId = blockFilters.value.roomId

  const response = await getBlocks(params)
  blocks.value = response.data || []
}

const loadPage = async () => {
  loading.value = true

  try {
    await loadSupportingData()
    await Promise.all([loadTemplates(), loadBlocks()])
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load block management data.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const resetTemplateForm = () => {
  selectedTemplate.value = null

  templateForm.value = {
    surgeonId: '',
    orRoomId: '',
    specialty: '',
    dayOfWeek: 1,
    startTime: '08:00',
    endTime: '16:00',
    effectiveFrom: '2026-06-22',
    effectiveTo: '',
    isActive: true
  }

  showTemplateModal.value = false
}

const openCreateTemplate = () => {
  resetTemplateForm()
  showTemplateModal.value = true
}

const openEditTemplate = template => {
  selectedTemplate.value = template

  templateForm.value = {
    surgeonId: template.surgeonId,
    orRoomId: template.orRoomId,
    specialty: template.specialty,
    dayOfWeek: template.dayOfWeek,
    startTime: formatTime(template.startTime),
    endTime: formatTime(template.endTime),
    effectiveFrom: template.effectiveFrom?.substring(0, 10) || '',
    effectiveTo: template.effectiveTo?.substring(0, 10) || '',
    isActive: template.isActive
  }

  showTemplateModal.value = true
}

const submitTemplate = async () => {
  if (!templateForm.value.surgeonId || !templateForm.value.orRoomId) {
    showToast('Surgeon and room are required.', 'warning')
    return
  }

  if (!templateForm.value.specialty.trim()) {
    showToast('Specialty is required.', 'warning')
    return
  }

  saving.value = true

  try {
    const payload = {
      surgeonId: Number(templateForm.value.surgeonId),
      orRoomId: Number(templateForm.value.orRoomId),
      specialty: templateForm.value.specialty.trim(),
      dayOfWeek: Number(templateForm.value.dayOfWeek),
      startTime: normalizeTimeForApi(templateForm.value.startTime),
      endTime: normalizeTimeForApi(templateForm.value.endTime),
      effectiveFrom: templateForm.value.effectiveFrom,
      effectiveTo: templateForm.value.effectiveTo || null,
      isActive: templateForm.value.isActive
    }

    if (selectedTemplate.value) {
      await updateBlockTemplate(selectedTemplate.value.templateId, payload)
      showToast('Block template updated successfully.', 'success')
    } else {
      await createBlockTemplate(payload)
      showToast('Block template created successfully.', 'success')
    }

    resetTemplateForm()
    await loadTemplates()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to save block template.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const handleDeleteTemplate = async template => {
  if (!confirm(`Deactivate template #${template.templateId}?`)) return

  try {
    await deleteBlockTemplate(template.templateId)
    showToast('Template deactivated successfully.', 'success')
    await loadTemplates()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to deactivate template.'

    showToast(message, 'error')
  }
}

const openException = template => {
  selectedExceptionTemplate.value = template

  exceptionForm.value = {
    skipDate: '',
    reason: ''
  }

  showExceptionModal.value = true
}

const closeExceptionModal = () => {
  selectedExceptionTemplate.value = null
  showExceptionModal.value = false

  exceptionForm.value = {
    skipDate: '',
    reason: ''
  }
}

const submitException = async () => {
  if (!selectedExceptionTemplate.value) return

  if (!exceptionForm.value.skipDate) {
    showToast('Skip date is required.', 'warning')
    return
  }

  saving.value = true

  try {
    await addBlockException(selectedExceptionTemplate.value.templateId, {
      skipDate: exceptionForm.value.skipDate,
      reason: exceptionForm.value.reason
    })

    showToast('Block exception added successfully.', 'success')
    closeExceptionModal()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to add block exception.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const submitGenerateBlocks = async () => {
  if (!generateForm.value.fromDate || !generateForm.value.toDate) {
    showToast('From date and to date are required.', 'warning')
    return
  }

  saving.value = true

  try {
    const response = await generateBlocks({
      fromDate: generateForm.value.fromDate,
      toDate: generateForm.value.toDate
    })

    showToast(`Blocks generated: ${response.data.generatedCount}`, 'success')
    activeTab.value = 'blocks'
    await loadBlocks()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to generate blocks.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const openEditBlock = block => {
  selectedBlock.value = block

  blockForm.value = {
    surgeonId: block.surgeonId,
    orRoomId: block.orRoomId,
    blockDate: block.blockDate?.substring(0, 10) || '',
    startTime: formatTime(block.startTime),
    endTime: formatTime(block.endTime),
    blockStatus: block.blockStatus,
    remarks: block.remarks || ''
  }

  showEditBlockModal.value = true
}

const closeEditBlockModal = () => {
  selectedBlock.value = null
  showEditBlockModal.value = false
}

const submitBlockUpdate = async () => {
  if (!selectedBlock.value) return

  saving.value = true

  try {
    await updateBlock(selectedBlock.value.blockId, {
      surgeonId: Number(blockForm.value.surgeonId),
      orRoomId: Number(blockForm.value.orRoomId),
      blockDate: blockForm.value.blockDate,
      startTime: normalizeTimeForApi(blockForm.value.startTime),
      endTime: normalizeTimeForApi(blockForm.value.endTime),
      blockStatus: blockForm.value.blockStatus,
      remarks: blockForm.value.remarks
    })

    showToast('Block updated successfully.', 'success')
    closeEditBlockModal()
    await loadBlocks()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to update block.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

const handleCancelBlock = async block => {
  if (!confirm(`Cancel block #${block.blockId}?`)) return

  try {
    await cancelBlock(block.blockId)
    showToast('Block cancelled successfully.', 'success')
    await loadBlocks()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to cancel block.'

    showToast(message, 'error')
  }
}

const openReleaseBlock = block => {
  selectedReleaseBlock.value = block

  releaseForm.value = {
    startTime: formatTime(block.startTime),
    endTime: formatTime(block.endTime),
    remarks: ''
  }

  showReleaseBlockModal.value = true
}

const closeReleaseBlockModal = () => {
  selectedReleaseBlock.value = null
  showReleaseBlockModal.value = false
}

const submitReleaseBlock = async () => {
  if (!selectedReleaseBlock.value) return

  saving.value = true

  try {
    await releaseBlock(selectedReleaseBlock.value.blockId, {
      startTime: normalizeTimeForApi(releaseForm.value.startTime),
      endTime: normalizeTimeForApi(releaseForm.value.endTime),
      remarks: releaseForm.value.remarks
    })

    showToast('Block released successfully.', 'success')
    closeReleaseBlockModal()
    await loadBlocks()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to release block.'

    showToast(message, 'error')
  } finally {
    saving.value = false
  }
}

onMounted(loadPage)
</script>

<template>
  <div>
    <PageHeader
      title="Block Management"
      subtitle="Manage templates, generate blocks, and release unused OR time"
      icon="bi-grid-3x3-gap"
    />

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <ul class="nav nav-pills mb-4">
        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'blocks' }"
            @click="activeTab = 'blocks'"
          >
            Blocks
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'templates' }"
            @click="activeTab = 'templates'"
          >
            Templates
          </button>
        </li>

        <li class="nav-item">
          <button
            class="nav-link"
            :class="{ active: activeTab === 'generate' }"
            @click="activeTab = 'generate'"
          >
            Generate
          </button>
        </li>
      </ul>

      <!-- Blocks Tab -->
      <div v-if="activeTab === 'blocks'">
        <div class="page-card mb-4">
          <div class="row g-3 align-items-end">
            <div class="col-md-3">
              <label class="form-label">From Date</label>
              <input
                v-model="blockFilters.fromDate"
                type="date"
                class="form-control"
              />
            </div>

            <div class="col-md-3">
              <label class="form-label">To Date</label>
              <input
                v-model="blockFilters.toDate"
                type="date"
                class="form-control"
              />
            </div>

            <div class="col-md-2">
              <label class="form-label">Surgeon</label>
              <select v-model="blockFilters.surgeonId" class="form-select">
                <option value="">All</option>
                <option
                  v-for="surgeon in surgeons"
                  :key="surgeon.surgeonId"
                  :value="surgeon.surgeonId"
                >
                  {{ surgeon.fullName || surgeon.surgeonName }}
                </option>
              </select>
            </div>

            <div class="col-md-2">
              <label class="form-label">Room</label>
              <select v-model="blockFilters.roomId" class="form-select">
                <option value="">All</option>
                <option
                  v-for="room in rooms"
                  :key="room.orRoomId"
                  :value="room.orRoomId"
                >
                  {{ room.roomName }}
                </option>
              </select>
            </div>

            <div class="col-md-2">
              <button class="btn btn-primary w-100" @click="loadBlocks">
                Apply
              </button>
            </div>
          </div>
        </div>

        <div class="page-card">
          <EmptyState
            v-if="blocks.length === 0"
            title="No blocks"
            message="No blocks found for the selected filters."
            icon="bi-calendar-x"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Block</th>
                  <th>Surgeon</th>
                  <th>Room</th>
                  <th>Date</th>
                  <th>Time</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th class="text-end">Actions</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="block in blocks" :key="block.blockId">
                  <td>#{{ block.blockId }}</td>
                  <td>{{ block.surgeonName }}</td>
                  <td>{{ block.roomName }}</td>
                  <td>{{ formatDate(block.blockDate) }}</td>
                  <td>{{ formatTime(block.startTime) }} - {{ formatTime(block.endTime) }}</td>
                  <td>{{ block.blockType }}</td>
                  <td>
                    <StatusBadge :status="block.blockStatus" />
                  </td>
                  <td class="text-end">
                    <button
                      class="btn btn-sm btn-outline-primary me-2"
                      @click="openEditBlock(block)"
                    >
                      Edit
                    </button>

                    <button
                      class="btn btn-sm btn-outline-success me-2"
                      :disabled="block.blockStatus === 'Released' || block.blockStatus === 'Cancelled'"
                      @click="openReleaseBlock(block)"
                    >
                      Release
                    </button>

                    <button
                      class="btn btn-sm btn-outline-danger"
                      :disabled="block.blockStatus === 'Cancelled'"
                      @click="handleCancelBlock(block)"
                    >
                      Cancel
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Templates Tab -->
      <div v-if="activeTab === 'templates'">
        <div class="d-flex justify-content-end mb-3">
          <button class="btn btn-primary" @click="openCreateTemplate">
            <i class="bi bi-plus-circle me-1"></i>
            Create Template
          </button>
        </div>

        <div class="page-card">
          <EmptyState
            v-if="templates.length === 0"
            title="No templates"
            message="No recurring block templates were found."
            icon="bi-calendar-x"
          />

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Template</th>
                  <th>Surgeon</th>
                  <th>Room</th>
                  <th>Specialty</th>
                  <th>Day</th>
                  <th>Time</th>
                  <th>Effective</th>
                  <th>Active</th>
                  <th class="text-end">Actions</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="template in templates" :key="template.templateId">
                  <td>#{{ template.templateId }}</td>
                  <td>{{ template.surgeonName }}</td>
                  <td>{{ template.roomName }}</td>
                  <td>{{ template.specialty }}</td>
                  <td>{{ dayNames[template.dayOfWeek] }}</td>
                  <td>{{ formatTime(template.startTime) }} - {{ formatTime(template.endTime) }}</td>
                  <td>
                    {{ formatDate(template.effectiveFrom) }}
                    <span v-if="template.effectiveTo">
                      to {{ formatDate(template.effectiveTo) }}
                    </span>
                  </td>
                  <td>
                    <span
                      class="badge"
                      :class="template.isActive ? 'bg-success' : 'bg-secondary'"
                    >
                      {{ template.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="text-end">
                    <button
                      class="btn btn-sm btn-outline-primary me-2"
                      @click="openEditTemplate(template)"
                    >
                      Edit
                    </button>

                    <button
                      class="btn btn-sm btn-outline-warning me-2"
                      @click="openException(template)"
                    >
                      Exception
                    </button>

                    <button
                      class="btn btn-sm btn-outline-danger"
                      @click="handleDeleteTemplate(template)"
                    >
                      Deactivate
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Generate Tab -->
      <div v-if="activeTab === 'generate'" class="page-card">
        <h5 class="mb-3">Generate Blocks</h5>

        <div class="row g-3 align-items-end">
          <div class="col-md-4">
            <label class="form-label">From Date</label>
            <input
              v-model="generateForm.fromDate"
              type="date"
              class="form-control"
            />
          </div>

          <div class="col-md-4">
            <label class="form-label">To Date</label>
            <input
              v-model="generateForm.toDate"
              type="date"
              class="form-control"
            />
          </div>

          <div class="col-md-4">
            <button
              class="btn btn-primary w-100"
              :disabled="saving"
              @click="submitGenerateBlocks"
            >
              <span
                v-if="saving"
                class="spinner-border spinner-border-sm me-2"
              ></span>
              Generate Blocks
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Block Modal -->
    <AppModal
      :show="showEditBlockModal"
      :title="selectedBlock ? `Edit Block #${selectedBlock.blockId}` : 'Edit Block'"
      size="xl"
      @close="closeEditBlockModal"
    >
      <div class="row g-3">
        <div class="col-md-3">
          <label class="form-label">Surgeon</label>
          <select v-model="blockForm.surgeonId" class="form-select">
            <option
              v-for="surgeon in surgeons"
              :key="surgeon.surgeonId"
              :value="surgeon.surgeonId"
            >
              {{ surgeon.fullName || surgeon.surgeonName }}
            </option>
          </select>
        </div>

        <div class="col-md-3">
          <label class="form-label">Room</label>
          <select v-model="blockForm.orRoomId" class="form-select">
            <option
              v-for="room in rooms"
              :key="room.orRoomId"
              :value="room.orRoomId"
            >
              {{ room.roomName }}
            </option>
          </select>
        </div>

        <div class="col-md-2">
          <label class="form-label">Date</label>
          <input v-model="blockForm.blockDate" type="date" class="form-control" />
        </div>

        <div class="col-md-2">
          <label class="form-label">Start</label>
          <input v-model="blockForm.startTime" type="time" class="form-control" />
        </div>

        <div class="col-md-2">
          <label class="form-label">End</label>
          <input v-model="blockForm.endTime" type="time" class="form-control" />
        </div>

        <div class="col-md-3">
          <label class="form-label">Status</label>
          <select v-model="blockForm.blockStatus" class="form-select">
            <option value="Allocated">Allocated</option>
            <option value="PartiallyBooked">Partially Booked</option>
            <option value="FullyBooked">Fully Booked</option>
            <option value="Released">Released</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <div class="col-md-9">
          <label class="form-label">Remarks</label>
          <input v-model="blockForm.remarks" class="form-control" />
        </div>
      </div>

      <template #footer>
        <button class="btn btn-outline-secondary" @click="closeEditBlockModal">
          Cancel
        </button>

        <button
          class="btn btn-primary"
          :disabled="saving"
          @click="submitBlockUpdate"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          Save Block
        </button>
      </template>
    </AppModal>

    <!-- Release Block Modal -->
    <AppModal
      :show="showReleaseBlockModal"
      :title="selectedReleaseBlock ? `Release Block #${selectedReleaseBlock.blockId}` : 'Release Block'"
      size="lg"
      @close="closeReleaseBlockModal"
    >
      <div class="row g-3">
        <div class="col-md-4">
          <label class="form-label">Release Start</label>
          <input v-model="releaseForm.startTime" type="time" class="form-control" />
        </div>

        <div class="col-md-4">
          <label class="form-label">Release End</label>
          <input v-model="releaseForm.endTime" type="time" class="form-control" />
        </div>

        <div class="col-md-12">
          <label class="form-label">Remarks</label>
          <input v-model="releaseForm.remarks" class="form-control" />
        </div>
      </div>

      <template #footer>
        <button class="btn btn-outline-secondary" @click="closeReleaseBlockModal">
          Cancel
        </button>

        <button
          class="btn btn-success"
          :disabled="saving"
          @click="submitReleaseBlock"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          Release Time
        </button>
      </template>
    </AppModal>

    <!-- Template Modal -->
    <AppModal
      :show="showTemplateModal"
      :title="selectedTemplate ? 'Edit Template' : 'Create Template'"
      size="xl"
      @close="resetTemplateForm"
    >
      <div class="row g-3">
        <div class="col-md-3">
          <label class="form-label">Surgeon</label>
          <select v-model="templateForm.surgeonId" class="form-select">
            <option value="">Select surgeon</option>
            <option
              v-for="surgeon in surgeons"
              :key="surgeon.surgeonId"
              :value="surgeon.surgeonId"
            >
              {{ surgeon.fullName || surgeon.surgeonName }}
            </option>
          </select>
        </div>

        <div class="col-md-3">
          <label class="form-label">Room</label>
          <select v-model="templateForm.orRoomId" class="form-select">
            <option value="">Select room</option>
            <option
              v-for="room in rooms"
              :key="room.orRoomId"
              :value="room.orRoomId"
            >
              {{ room.roomName }}
            </option>
          </select>
        </div>

        <div class="col-md-3">
          <label class="form-label">Specialty</label>
          <input v-model="templateForm.specialty" class="form-control" />
        </div>

        <div class="col-md-3">
          <label class="form-label">Day</label>
          <select v-model.number="templateForm.dayOfWeek" class="form-select">
            <option v-for="day in 7" :key="day" :value="day">
              {{ dayNames[day] }}
            </option>
          </select>
        </div>

        <div class="col-md-2">
          <label class="form-label">Start</label>
          <input v-model="templateForm.startTime" type="time" class="form-control" />
        </div>

        <div class="col-md-2">
          <label class="form-label">End</label>
          <input v-model="templateForm.endTime" type="time" class="form-control" />
        </div>

        <div class="col-md-3">
          <label class="form-label">Effective From</label>
          <input v-model="templateForm.effectiveFrom" type="date" class="form-control" />
        </div>

        <div class="col-md-3">
          <label class="form-label">Effective To</label>
          <input v-model="templateForm.effectiveTo" type="date" class="form-control" />
        </div>

        <div class="col-md-2">
          <label class="form-label">Active</label>
          <select v-model="templateForm.isActive" class="form-select">
            <option :value="true">Yes</option>
            <option :value="false">No</option>
          </select>
        </div>
      </div>

      <template #footer>
        <button class="btn btn-outline-secondary" @click="resetTemplateForm">
          Cancel
        </button>

        <button
          class="btn btn-primary"
          :disabled="saving"
          @click="submitTemplate"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          {{ selectedTemplate ? 'Update Template' : 'Create Template' }}
        </button>
      </template>
    </AppModal>

    <!-- Exception Modal -->
    <AppModal
      :show="showExceptionModal"
      :title="selectedExceptionTemplate ? `Add Exception — Template #${selectedExceptionTemplate.templateId}` : 'Add Exception'"
      size="lg"
      @close="closeExceptionModal"
    >
      <div class="row g-3">
        <div class="col-md-4">
          <label class="form-label">Skip Date</label>
          <input v-model="exceptionForm.skipDate" type="date" class="form-control" />
        </div>

        <div class="col-md-8">
          <label class="form-label">Reason</label>
          <input v-model="exceptionForm.reason" class="form-control" />
        </div>
      </div>

      <template #footer>
        <button class="btn btn-outline-secondary" @click="closeExceptionModal">
          Cancel
        </button>

        <button
          class="btn btn-warning"
          :disabled="saving"
          @click="submitException"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-2"></span>
          Add Exception
        </button>
      </template>
    </AppModal>
  </div>
</template>