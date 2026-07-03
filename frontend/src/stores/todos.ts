import { defineStore } from 'pinia'
import {
  fetchTodos,
  createTodo,
  updateTodo,
  toggleTodoComplete,
  deleteTodo,
  type Todo,
  type TodoInput,
} from '../api/todoApi'

export type StatusFilter = 'all' | 'active' | 'completed'

export const useTodoStore = defineStore('todos', {
  state: () => ({
    todos: [] as Todo[],
    statusFilter: 'all' as StatusFilter,
    keyword: '',
    isLoading: false,
  }),
  getters: {
    remainingCount: (state) => state.todos.filter((t) => !t.isCompleted).length,
  },
  actions: {
    async load() {
      this.isLoading = true
      try {
        const completed =
          this.statusFilter === 'all' ? undefined : this.statusFilter === 'completed'
        this.todos = await fetchTodos({ completed, keyword: this.keyword || undefined })
      } finally {
        this.isLoading = false
      }
    },

    async add(input: TodoInput) {
      const todo = await createTodo(input)
      this.todos.unshift(todo)
    },

    async edit(id: number, input: TodoInput & { isCompleted: boolean }) {
      const updated = await updateTodo(id, input)
      const index = this.todos.findIndex((t) => t.id === id)
      if (index !== -1) this.todos[index] = updated
    },

    async toggleComplete(id: number) {
      const updated = await toggleTodoComplete(id)
      const index = this.todos.findIndex((t) => t.id === id)
      if (index !== -1) this.todos[index] = updated
    },

    async remove(id: number) {
      await deleteTodo(id)
      this.todos = this.todos.filter((t) => t.id !== id)
    },
  },
})
