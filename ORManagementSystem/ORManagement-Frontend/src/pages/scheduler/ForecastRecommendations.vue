<script setup>
import { computed, onMounted, ref } from 'vue'

import LoadingSpinner from '../../components/common/LoadingSpinner.vue'
import EmptyState from '../../components/common/EmptyState.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import { getForecastSummary } from '../../services/forecastService'
import { showToast } from '../../utils/toast'
import { formatDate } from '../../utils/formatters'

const loading = ref(false)
const forecast = ref(null)

const loadForecast = async () => {
  loading.value = true

  try {
    const response = await getForecastSummary()
    forecast.value = response.data || null
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.title ||
      'Failed to load forecast summary.'

    showToast(message, 'error')
  } finally {
    loading.value = false
  }
}

const formatNumber = value => {
  if (value === null || value === undefined) return '0'
  return Number(value).toFixed(0)
}

const formatDecimal = value => {
  if (value === null || value === undefined) return '0.00'
  return Number(value).toFixed(2)
}

const maxForecastHours = computed(() => {
  const values =
    forecast.value?.surgeryTypeForecasts?.map(item =>
      Number(item.forecastedHoursNextWeek || 0)
    ) || []

  return Math.max(...values, 1)
})

const maxAverageDuration = computed(() => {
  const values =
    forecast.value?.surgeryTypeForecasts?.map(item =>
      Number(item.averageDurationMinutes || 0)
    ) || []

  return Math.max(...values, 1)
})

const maxWeeklyDemand = computed(() => {
  const values =
    forecast.value?.weeklyDemandTrend?.map(item =>
      Math.max(
        Number(item.actualCases || 0),
        Number(item.forecastedCases || 0)
      )
    ) || []

  return Math.max(...values, 1)
})

const maxSpecialtyHours = computed(() => {
  const values =
    forecast.value?.specialtyCapacityGaps?.flatMap(item => [
      Number(item.forecastedHours || 0),
      Number(item.availableBlockHours || 0)
    ]) || []

  return Math.max(...values, 1)
})

const getForecastBarWidth = hours => {
  return `${Math.min((Number(hours || 0) / maxForecastHours.value) * 100, 100)}%`
}

const getDurationBarWidth = duration => {
  return `${Math.min((Number(duration || 0) / maxAverageDuration.value) * 100, 100)}%`
}

const getWeeklyBarHeight = value => {
  return `${Math.max((Number(value || 0) / maxWeeklyDemand.value) * 100, 4)}%`
}

const getSpecialtyBarWidth = value => {
  return `${Math.min((Number(value || 0) / maxSpecialtyHours.value) * 100, 100)}%`
}

const capacityGapClass = computed(() => {
  const gap = Number(forecast.value?.capacityGapHours || 0)

  if (gap > 0) return 'text-danger'
  if (gap < 0) return 'text-success'

  return 'text-primary'
})

const capacityGapLabel = computed(() => {
  const gap = Number(forecast.value?.capacityGapHours || 0)

  if (gap > 0) {
    return `Short by ${formatDecimal(gap)} hrs`
  }

  if (gap < 0) {
    return `Surplus ${formatDecimal(Math.abs(gap))} hrs`
  }

  return 'Balanced'
})

onMounted(loadForecast)
</script>

<template>
  <div>
    <PageHeader
      title="Forecast Intelligence"
      subtitle="Predict future surgical demand, OR hours, average case duration, and specialty capacity gaps"
      icon="bi-graph-up-arrow"
    >
      <template #actions>
        <button
          class="btn btn-outline-primary"
          :disabled="loading"
          @click="loadForecast"
        >
          <span
            v-if="loading"


          ></span>
          <i
            v-else
            class="bi bi-arrow-clockwise me-1"
          ></i>
          Refresh
        </button>
      </template>
    </PageHeader>

    <LoadingSpinner v-if="loading" />

    <div v-else>
      <EmptyState
        v-if="!forecast"
        title="No forecast data"
        message="Forecast summary could not be loaded."
        icon="bi-graph-up"
      />

      <div v-else>
        <!-- Summary Cards -->
        <div class="row g-3 mb-4">
          <div class="col-md-3">
            <div class="forecast-card">
              <div class="forecast-label">Predicted Cases</div>
              <div class="forecast-value text-primary">
                {{ formatNumber(forecast.predictedCasesNextWeek) }}
              </div>
              <div class="forecast-help">Next week surgical case demand</div>
            </div>
          </div>

          <div class="col-md-3">
            <div class="forecast-card">
              <div class="forecast-label">Predicted OR Hours</div>
              <div class="forecast-value text-info">
                {{ formatDecimal(forecast.predictedOrHoursNextWeek) }}
              </div>
              <div class="forecast-help">Estimated OR hours required</div>
            </div>
          </div>

          <div class="col-md-3">
            <div class="forecast-card">
              <div class="forecast-label">Available Block Hours</div>
              <div class="forecast-value text-success">
                {{ formatDecimal(forecast.availableBlockHoursNextWeek) }}
              </div>
              <div class="forecast-help">Planned capacity for next week</div>
            </div>
          </div>

          <div class="col-md-3">
            <div class="forecast-card">
              <div class="forecast-label">Capacity Gap</div>
              <div
                class="forecast-value"
                :class="capacityGapClass"
              >
                {{ capacityGapLabel }}
              </div>
              <div class="forecast-help">Demand vs available capacity</div>
            </div>
          </div>
        </div>

        <!-- Explanation -->
        <div class="alert alert-primary mb-4">
          <strong>Forecast method:</strong>
          This module uses a lightweight moving-average and trend-adjustment forecast model.
          It estimates future surgical demand by surgery type, converts demand into OR hours
          using average duration, then compares forecasted demand against available block capacity.
        </div>

        <div class="row g-4">
          <!-- Weekly Demand Trend -->
          <div class="col-lg-6">
            <div class="page-card h-100">
              <h5 class="mb-1">
                <i class="bi bi-bar-chart-line me-2 text-primary"></i>
                Weekly Demand Trend
              </h5>
              <small class="text-muted">
                Historical case demand with next-week forecast.
              </small>

              <div class="weekly-chart mt-4">
                <div
                  v-for="item in forecast.weeklyDemandTrend"
                  :key="item.weekStartDate"
                  class="weekly-chart-item"
                >
                  <div class="weekly-bar-wrapper">
                    <div
                      v-if="item.forecastedCases !== null && item.forecastedCases !== undefined"
                      class="weekly-bar weekly-bar-forecast"
                      :style="{ height: getWeeklyBarHeight(item.forecastedCases) }"
                    ></div>

                    <div
                      v-else
                      class="weekly-bar weekly-bar-actual"
                      :style="{ height: getWeeklyBarHeight(item.actualCases) }"
                    ></div>
                  </div>

                  <div class="weekly-count">
                    {{
                      item.forecastedCases !== null && item.forecastedCases !== undefined
                        ? formatNumber(item.forecastedCases)
                        : item.actualCases
                    }}
                  </div>

                  <div class="weekly-label">
                    {{ formatDate(item.weekStartDate) }}
                  </div>

                  <div
                    v-if="item.forecastedCases !== null && item.forecastedCases !== undefined"
                    class="weekly-badge"
                  >
                    Forecast
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Specialty Capacity Gap -->
          <div class="col-lg-6">
            <div class="page-card h-100">
              <h5 class="mb-1">
                <i class="bi bi-hospital me-2 text-primary"></i>
                Specialty Capacity Gap
              </h5>
              <small class="text-muted">
                Forecasted demand hours compared with available block hours.
              </small>

              <div class="mt-4">
                <div
                  v-for="item in forecast.specialtyCapacityGaps"
                  :key="item.specialty"
                  class="capacity-gap-row"
                >
                  <div class="d-flex justify-content-between gap-2 mb-1">
                    <strong>{{ item.specialty }}</strong>
                    <span
                      class="fw-semibold"
                      :class="Number(item.gapHours) > 0 ? 'text-danger' : 'text-success'"
                    >
                      Gap: {{ formatDecimal(item.gapHours) }} hrs
                    </span>
                  </div>

                  <div class="capacity-bar-line">
                    <div
                      class="capacity-bar forecast-hours"
                      :style="{ width: getSpecialtyBarWidth(item.forecastedHours) }"
                    ></div>
                  </div>

                  <div class="capacity-bar-line mt-1">
                    <div
                      class="capacity-bar available-hours"
                      :style="{ width: getSpecialtyBarWidth(item.availableBlockHours) }"
                    ></div>
                  </div>

                  <div class="d-flex justify-content-between small text-muted mt-1">
                    <span>Forecast: {{ formatDecimal(item.forecastedHours) }} hrs</span>
                    <span>Available: {{ formatDecimal(item.availableBlockHours) }} hrs</span>
                  </div>
                </div>
              </div>

              <div class="chart-legend mt-3">
                <span>
                  <i class="legend-box forecast-box"></i>
                  Forecasted Hours
                </span>
                <span>
                  <i class="legend-box available-box"></i>
                  Available Hours
                </span>
              </div>
            </div>
          </div>

          <!-- Forecasted OR Hours by Surgery Type -->
          <div class="col-lg-6">
            <div class="page-card h-100">
              <h5 class="mb-1">
                <i class="bi bi-activity me-2 text-primary"></i>
                Forecasted OR Hours by Surgery Type
              </h5>
              <small class="text-muted">
                Predicted next-week OR hours by procedure category.
              </small>

              <div class="mt-4">
                <div
                  v-for="item in forecast.surgeryTypeForecasts"
                  :key="item.surgeryType"
                  class="metric-row"
                >
                  <div class="d-flex justify-content-between gap-2 mb-1">
                    <span class="metric-title">{{ item.surgeryType }}</span>
                    <strong>{{ formatDecimal(item.forecastedHoursNextWeek) }} hrs</strong>
                  </div>

                  <div class="metric-bar-track">
                    <div
                      class="metric-bar metric-bar-primary"
                      :style="{ width: getForecastBarWidth(item.forecastedHoursNextWeek) }"
                    ></div>
                  </div>

                  <small class="text-muted">
                    Forecast cases: {{ formatDecimal(item.forecastedCasesNextWeek) }}
                  </small>
                </div>
              </div>
            </div>
          </div>

          <!-- Average Duration -->
          <div class="col-lg-6">
            <div class="page-card h-100">
              <h5 class="mb-1">
                <i class="bi bi-clock-history me-2 text-primary"></i>
                Average Duration by Surgery Type
              </h5>
              <small class="text-muted">
                Average planned/scheduled case duration used in forecast conversion.
              </small>

              <div class="mt-4">
                <div
                  v-for="item in forecast.surgeryTypeForecasts"
                  :key="`${item.surgeryType}-duration`"
                  class="metric-row"
                >
                  <div class="d-flex justify-content-between gap-2 mb-1">
                    <span class="metric-title">{{ item.surgeryType }}</span>
                    <strong>{{ formatDecimal(item.averageDurationMinutes) }} min</strong>
                  </div>

                  <div class="metric-bar-track">
                    <div
                      class="metric-bar metric-bar-info"
                      :style="{ width: getDurationBarWidth(item.averageDurationMinutes) }"
                    ></div>
                  </div>

                  <small class="text-muted">
                    Historical cases: {{ item.historicalCaseCount }}
                  </small>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Forecast Table -->
        <div class="page-card mt-4">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <div>
              <h5 class="mb-0">
                <i class="bi bi-table me-2 text-primary"></i>
                Surgery Type Forecast Details
              </h5>
              <small class="text-muted">
                Detailed forecast values used by the dashboard.
              </small>
            </div>
          </div>

          <div class="table-responsive">
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Surgery Type</th>
                  <th class="text-end">Historical Cases</th>
                  <th class="text-end">Avg Duration</th>
                  <th class="text-end">Forecast Cases</th>
                  <th class="text-end">Forecast Hours</th>
                </tr>
              </thead>

              <tbody>
                <tr
                  v-for="item in forecast.surgeryTypeForecasts"
                  :key="`${item.surgeryType}-row`"
                >
                  <td>
                    <strong>{{ item.surgeryType }}</strong>
                  </td>

                  <td class="text-end">
                    {{ item.historicalCaseCount }}
                  </td>

                  <td class="text-end">
                    {{ formatDecimal(item.averageDurationMinutes) }} min
                  </td>

                  <td class="text-end">
                    {{ formatDecimal(item.forecastedCasesNextWeek) }}
                  </td>

                  <td class="text-end">
                    <strong>{{ formatDecimal(item.forecastedHoursNextWeek) }} hrs</strong>
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
.forecast-card {
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  padding: 18px;
  background: #ffffff;
  min-height: 125px;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}

.forecast-label {
  font-size: 12px;
  color: #64748b;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin-bottom: 8px;
}

.forecast-value {
  font-size: 28px;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.2;
}

.forecast-help {
  margin-top: 8px;
  color: #64748b;
  font-size: 13px;
}

.weekly-chart {
  height: 260px;
  display: flex;
  align-items: stretch;
  gap: 14px;
  padding-top: 10px;
}

.weekly-chart-item {
  flex: 1;
  min-width: 70px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.weekly-bar-wrapper {
  flex: 1;
  width: 38px;
  display: flex;
  align-items: flex-end;
  border-radius: 10px;
  background: #f1f5f9;
  overflow: hidden;
  border: 1px solid #e2e8f0;
}

.weekly-bar {
  width: 100%;
  border-radius: 10px 10px 0 0;
}

.weekly-bar-actual {
  background: linear-gradient(180deg, #0d6efd, #78aefc);
}

.weekly-bar-forecast {
  background: linear-gradient(180deg, #dc3545, #f59aaa);
}

.weekly-count {
  margin-top: 8px;
  font-weight: 800;
  color: #0f172a;
}

.weekly-label {
  font-size: 11px;
  color: #64748b;
  text-align: center;
  margin-top: 2px;
}

.weekly-badge {
  margin-top: 4px;
  font-size: 10px;
  font-weight: 700;
  color: #dc3545;
}

.metric-row {
  margin-bottom: 16px;
}

.metric-title {
  font-weight: 700;
  color: #334155;
}

.metric-bar-track {
  height: 12px;
  border-radius: 999px;
  background: #e2e8f0;
  overflow: hidden;
}

.metric-bar {
  height: 100%;
  border-radius: 999px;
}

.metric-bar-primary {
  background: linear-gradient(90deg, #0d6efd, #78aefc);
}

.metric-bar-info {
  background: linear-gradient(90deg, #0dcaf0, #8be7f8);
}

.capacity-gap-row {
  margin-bottom: 18px;
}

.capacity-bar-line {
  height: 11px;
  border-radius: 999px;
  background: #e2e8f0;
  overflow: hidden;
}

.capacity-bar {
  height: 100%;
  border-radius: 999px;
}

.forecast-hours {
  background: linear-gradient(90deg, #dc3545, #f59aaa);
}

.available-hours {
  background: linear-gradient(90deg, #198754, #7bdba7);
}

.chart-legend {
  display: flex;
  gap: 18px;
  flex-wrap: wrap;
  color: #64748b;
  font-size: 13px;
}

.legend-box {
  display: inline-block;
  width: 12px;
  height: 12px;
  border-radius: 3px;
  margin-right: 6px;
  vertical-align: middle;
}

.forecast-box {
  background: #dc3545;
}

.available-box {
  background: #198754;
}
</style>