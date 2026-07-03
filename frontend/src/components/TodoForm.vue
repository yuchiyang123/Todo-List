<script setup lang="ts">
import { ref } from 'vue'
import type { Priority, TodoInput } from '../api/todoApi'

const props = withDefaults(
  defineProps<{
    initial?: TodoInput
    submitLabel?: string
  }>(),
  { submitLabel: '新增' },
)

const emit = defineEmits<{
  submit: [TodoInput]
  cancel: []
}>()

const title = ref(props.initial?.title ?? '')
const description = ref(props.initial?.description ?? '')
const priority = ref<Priority>(props.initial?.priority ?? 1)
const dueDate = ref(props.initial?.dueDate?.slice(0, 10) ?? '')

function handleSubmit() {
  if (!title.value.trim()) return
  emit('submit', {
    title: title.value.trim(),
    description: description.value.trim() || null,
    priority: priority.value,
    dueDate: dueDate.value || null,
  })
}
</script>

<template>
  <form class="todo-form" @submit.prevent="handleSubmit">
    <input v-model="title" class="title-input" placeholder="想完成什麼事？" required />
    <textarea v-model="description" class="description-input" placeholder="備註（選填）" rows="2" />
    <div class="row">
      <select v-model.number="priority">
        <option :value="0">低優先</option>
        <option :value="1">中優先</option>
        <option :value="2">高優先</option>
      </select>
      <input v-model="dueDate" type="date" />
    </div>
    <div class="actions">
      <button type="button" class="btn btn-ghost" @click="emit('cancel')">取消</button>
      <button type="submit" class="btn btn-primary">{{ submitLabel }}</button>
    </div>
  </form>
</template>

<style scoped>
.todo-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.title-input {
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
  color: var(--text-h);
  font-size: 15px;
}

.description-input {
  padding: 8px 12px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
  color: var(--text-h);
  resize: vertical;
}

.row {
  display: flex;
  gap: var(--spacing-3);
}

.row select,
.row input {
  padding: 6px 10px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--surface);
  color: var(--text-h);
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}
</style>
