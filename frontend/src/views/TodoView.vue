<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useTodoStore } from '../stores/todos'
import type { TodoInput } from '../api/todoApi'
import TodoForm from '../components/TodoForm.vue'
import TodoItem from '../components/TodoItem.vue'
import FilterBar from '../components/FilterBar.vue'

const store = useTodoStore()
const isAdding = ref(false)

onMounted(() => store.load())

watch(() => store.statusFilter, () => store.load())

let searchTimer: ReturnType<typeof setTimeout> | undefined
watch(
  () => store.keyword,
  () => {
    clearTimeout(searchTimer)
    searchTimer = setTimeout(() => store.load(), 300)
  },
)

async function handleAdd(input: TodoInput) {
  await store.add(input)
  isAdding.value = false
}
</script>

<template>
  <div class="todo-view">
    <div class="header">
      <h1>待辦事項</h1>
      <span class="remaining">{{ store.remainingCount }} 項待完成</span>
    </div>

    <FilterBar
      :status="store.statusFilter"
      :keyword="store.keyword"
      @update:status="store.statusFilter = $event"
      @update:keyword="store.keyword = $event"
    />

    <div class="card add-card">
      <TodoForm v-if="isAdding" submit-label="新增" @submit="handleAdd" @cancel="isAdding = false" />
      <button v-else class="btn add-trigger" @click="isAdding = true">+ 新增待辦事項</button>
    </div>

    <p v-if="store.isLoading" class="status-text">載入中...</p>
    <p v-else-if="store.todos.length === 0" class="status-text">目前沒有待辦事項</p>

    <ul v-else class="todo-list">
      <TodoItem
        v-for="todo in store.todos"
        :key="todo.id"
        :todo="todo"
        @toggle="store.toggleComplete($event)"
        @update="(id, input) => store.edit(id, input)"
        @remove="store.remove($event)"
      />
    </ul>
  </div>
</template>

<style scoped>
.header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin-bottom: var(--spacing-5);
}

h1 {
  font-size: 22px;
}

.remaining {
  font-size: 13px;
  color: var(--text);
}

.add-card {
  padding: var(--spacing-4);
  margin-bottom: var(--spacing-5);
}

.add-trigger {
  width: 100%;
  justify-content: flex-start;
  color: var(--text);
  border-style: dashed;
}

.status-text {
  text-align: center;
  color: var(--text);
  padding: var(--spacing-6) 0;
}

.todo-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}
</style>
