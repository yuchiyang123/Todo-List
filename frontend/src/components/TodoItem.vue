<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Todo, TodoInput } from '../api/todoApi'
import TodoForm from './TodoForm.vue'

const props = defineProps<{ todo: Todo }>()

const emit = defineEmits<{
  toggle: [number]
  update: [number, TodoInput & { isCompleted: boolean }]
  remove: [number]
}>()

const isEditing = ref(false)

const priorityLabel = computed(() => ['低', '中', '高'][props.todo.priority])
const priorityClass = computed(() => ['priority-low', 'priority-medium', 'priority-high'][props.todo.priority])

const isOverdue = computed(() => {
  if (!props.todo.dueDate || props.todo.isCompleted) return false
  return new Date(props.todo.dueDate) < new Date(new Date().toDateString())
})

function handleUpdate(input: TodoInput) {
  emit('update', props.todo.id, { ...input, isCompleted: props.todo.isCompleted })
  isEditing.value = false
}
</script>

<template>
  <li class="card todo-item" :class="{ done: todo.isCompleted }">
    <template v-if="isEditing">
      <TodoForm
        :initial="{ title: todo.title, description: todo.description, priority: todo.priority, dueDate: todo.dueDate }"
        submit-label="儲存"
        @submit="handleUpdate"
        @cancel="isEditing = false"
      />
    </template>
    <template v-else>
      <label class="checkbox">
        <input type="checkbox" :checked="todo.isCompleted" @change="emit('toggle', todo.id)" />
      </label>
      <div class="content">
        <p class="title">{{ todo.title }}</p>
        <p v-if="todo.description" class="description">{{ todo.description }}</p>
        <div class="meta">
          <span class="badge" :class="priorityClass">{{ priorityLabel }}</span>
          <span v-if="todo.dueDate" class="due" :class="{ overdue: isOverdue }">
            期限 {{ todo.dueDate.slice(0, 10) }}
          </span>
        </div>
      </div>
      <div class="row-actions">
        <button class="btn btn-ghost" @click="isEditing = true">編輯</button>
        <button class="btn btn-ghost btn-danger" @click="emit('remove', todo.id)">刪除</button>
      </div>
    </template>
  </li>
</template>

<style scoped>
.todo-item {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-3);
  padding: var(--spacing-4);
}

.checkbox input {
  width: 18px;
  height: 18px;
  margin-top: 2px;
  accent-color: var(--accent);
  cursor: pointer;
}

.content {
  flex: 1;
  min-width: 0;
}

.title {
  color: var(--text-h);
  font-weight: 500;
  word-break: break-word;
}

.done .title {
  text-decoration: line-through;
  color: var(--text);
}

.description {
  margin-top: var(--spacing-1);
  font-size: 13px;
  color: var(--text);
  word-break: break-word;
}

.meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  margin-top: var(--spacing-2);
}

.badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 999px;
  border: 1px solid var(--border);
}

.priority-low {
  color: var(--success);
  border-color: var(--success);
}

.priority-medium {
  color: var(--accent);
  border-color: var(--accent);
}

.priority-high {
  color: var(--danger);
  border-color: var(--danger);
}

.due {
  font-size: 12px;
  color: var(--text);
}

.due.overdue {
  color: var(--danger);
}

.row-actions {
  display: flex;
  gap: var(--spacing-1);
  flex-shrink: 0;
}
</style>
