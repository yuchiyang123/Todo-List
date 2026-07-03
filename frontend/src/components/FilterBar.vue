<script setup lang="ts">
import type { StatusFilter } from '../stores/todos'

const props = defineProps<{
  status: StatusFilter
  keyword: string
}>()

const emit = defineEmits<{
  'update:status': [StatusFilter]
  'update:keyword': [string]
}>()

const tabs: { value: StatusFilter; label: string }[] = [
  { value: 'all', label: '全部' },
  { value: 'active', label: '待完成' },
  { value: 'completed', label: '已完成' },
]
</script>

<template>
  <div class="filter-bar">
    <div class="tabs">
      <button
        v-for="tab in tabs"
        :key="tab.value"
        class="btn tab"
        :class="{ active: props.status === tab.value }"
        @click="emit('update:status', tab.value)"
      >
        {{ tab.label }}
      </button>
    </div>
    <input
      class="search"
      type="search"
      placeholder="搜尋待辦事項..."
      :value="props.keyword"
      @input="emit('update:keyword', ($event.target as HTMLInputElement).value)"
    />
  </div>
</template>

<style scoped>
.filter-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-3);
  margin-bottom: var(--spacing-4);
}

.tabs {
  display: flex;
  gap: var(--spacing-1);
}

.tab {
  padding: 6px 14px;
  font-size: 14px;
}

.tab.active {
  border-color: var(--accent);
  color: var(--accent);
}

.search {
  padding: 7px 12px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
  color: var(--text-h);
  min-width: 200px;
}
</style>
